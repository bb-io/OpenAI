using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Apps.OpenAI.Actions.Base;
using Apps.OpenAI.Api.Requests;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Extensions;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Requests.Image;
using Apps.OpenAI.Models.Responses.Chat;
using Apps.OpenAI.Models.Responses.Image;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.Files;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Bilingual.Xliff1;
using Blackbird.Filters.Coders;
using Blackbird.Filters.Constants;
using Blackbird.Filters.Transformations;
using Newtonsoft.Json;
using RestSharp;

namespace Apps.OpenAI.Actions;

[ActionList("Images")]
public class ImageActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient) : BaseActions(invocationContext, fileManagementClient)
{
    [Action("Generate image", Description = "Generates an image from a prompt.")]
    public async Task<ImageResponse> GenerateImage([ActionParameter] ImageGenerationModelIdentifier modelIdentifier,
        [ActionParameter] ImageRequest input)
    {
        ThrowForAzure("image");

        var model = modelIdentifier.ModelId ?? "dall-e-3";
        var request = new OpenAIRequest("/images/generations", Method.Post);

        if (model == "dall-e-3")
        {
            request.AddJsonBody(new
            {
                model,
                prompt = input.Prompt,
                response_format = "b64_json",
                size = input.Size ?? "1024x1024",
                quality = input.Quality ?? "standard",
                style = input.Style ?? "vivid"
            });
        }
        else if (model == "gpt-image-1" || model == "chatgpt-image-latest")
        {
            request.AddJsonBody(new
            {
                model,
                prompt = input.Prompt,
                size = input.Size ?? "1024x1024"
            });
        }
        else
        {
            request.AddJsonBody(new
            {
                model,
                prompt = input.Prompt,
                size = input.Size ?? "1024x1024"
            });
        }

        var response = await UniversalClient.ExecuteWithErrorHandling<DataDto<ImageDataDto>>(request);
        var bytes = Convert.FromBase64String(response.Data.First().Base64);

        using var stream = new MemoryStream(bytes);
        var filename = (input.OutputImageName ?? "image") + ".png";
        var file = await FileManagementClient.UploadAsync(stream, "image/png", filename);
        return new() { Image = file };
    }

    [Action("Get localizable content from image", Description = "Extracts localizable text content from an image.")]
    public async Task<ChatResponse> GetLocalizableContentFromImage(
        [ActionParameter] ImageChatModelIdentifier modelIdentifier,
        [ActionParameter] GetLocalizableContentFromImageRequest input)
    {
        ThrowForAzure("image");

        var prompt = "Your objective is to conduct optical character recognition (OCR) to identify and extract any " +
                     "localizable content present in the image. Respond with the text found in the image, if any. " +
                     "If no localizable content is detected, provide an empty response.";

        var fileStream = await FileManagementClient.DownloadAsync(input.Image);
        var fileBytes = await fileStream.GetByteData();
        var messages = new List<ChatImageMessageDto>
            {
                new(MessageRoles.User, new List<ChatImageMessageContentDto>
                {
                    new ChatImageMessageTextContentDto("text", prompt),
                    new ChatImageMessageImageContentDto("image_url", new ImageUrlDto(
                        $"data:{input.Image.ContentType};base64,{Convert.ToBase64String(fileBytes)}"))
                })
            };
        
        var response = await ExecuteApiRequestAsync(messages, modelIdentifier.ModelId, input);
        return new()
        {
            SystemPrompt = prompt,
            UserPrompt = "",
            Message = response.Choices.First().Message.Content,
            Usage = response.Usage,
        };
    }

    [Action("Extract localizable content from image", Description = "Extracts localizable text from an image into an XLIFF file.")]
    public async Task<ExtractLocalizableContentFromImageResponse> ExtractLocalizableContentFromImage(
        [ActionParameter] ImageChatModelIdentifier modelIdentifier,
        [ActionParameter] ExtractLocalizableContentFromImageRequest input)
    {
        ThrowForAzure("image");
        ValidateImage(input.Image, allowGif: true);

        var sourceLanguageInstruction = string.IsNullOrWhiteSpace(input.SourceLanguage)
            ? "Detect the language of the extracted text and return its BCP-47 language code."
            : $"The source language is '{input.SourceLanguage}'. Return this exact value in source_language.";

        var prompt = "Perform OCR on the supplied image and extract all visible text that should be localized. " +
                     "Return each complete text item as a separate segment in natural reading order. " +
                     "Preserve the exact source spelling, punctuation, capitalization, numbers, and meaningful line breaks. " +
                     "Do not translate, summarize, correct, merge unrelated text, or include descriptions of visual elements. " +
                     "If the image contains no localizable text, return an empty segments array. " +
                     sourceLanguageInstruction;

        var fileStream = await FileManagementClient.DownloadAsync(input.Image);
        var fileBytes = await fileStream.GetByteData();
        var contentType = GetImageContentType(input.Image);
        var messages = new List<ChatImageMessageDto>
        {
            new(MessageRoles.User,
            [
                new ChatImageMessageTextContentDto("text", prompt),
                new ChatImageMessageImageContentDto("image_url", new ImageUrlDto(
                    $"data:{contentType};base64,{Convert.ToBase64String(fileBytes)}"))
            ])
        };

        var response = await ExecuteApiRequestAsync(
            messages,
            modelIdentifier.ModelId,
            input,
            ResponseFormats.GetImageLocalizationExtractionResponseFormat());

        var extraction = DeserializeExtraction(response.Choices.First().Message.Content);
        var segments = extraction.Segments
            .Select(x => x.Text?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .ToList();

        if (segments.Count == 0)
        {
            throw new PluginMisconfigurationException("No localizable text was detected in the supplied image.");
        }

        var sourceLanguage = !string.IsNullOrWhiteSpace(input.SourceLanguage)
            ? input.SourceLanguage!
            : extraction.SourceLanguage?.Trim();

        if (string.IsNullOrWhiteSpace(sourceLanguage))
        {
            throw new PluginApplicationException("The model extracted text but did not return its source language.");
        }

        var outputName = BuildOutputName(input.OutputFileName, input.Image.Name, ".xlf");
        var transformation = CreateImageTextTransformation(
            segments,
            sourceLanguage,
            input.Image,
            outputName);

        var isXliff1 = string.Equals(input.OutputFileFormat, "xliff1", StringComparison.OrdinalIgnoreCase);
        using var outputStream = isXliff1
            ? new MemoryStream(System.Text.Encoding.UTF8.GetBytes(Xliff1Serializer.Serialize(transformation)))
            : transformation.ToStream();
        var mediaType = isXliff1 ? MediaTypes.Xliff1 : MediaTypes.Xliff2;
        var file = await FileManagementClient.UploadAsync(outputStream, mediaType, outputName);

        return new()
        {
            File = file,
            SourceLanguage = sourceLanguage,
            SegmentsCount = segments.Count,
            Usage = response.Usage
        };
    }

    [Action("Generate localized image variant", Description = "Replaces text in an original image using translations from an XLIFF file.")]
    public async Task<ImageResponse> GenerateLocalizedImageVariant(
        [ActionParameter] ImageEditingModelIdentifier modelIdentifier,
        [ActionParameter] GenerateLocalizedImageVariantRequest input)
    {
        ThrowForAzure("image");
        ValidateImage(input.Image, allowGif: false);

        var localizedExtension = Path.GetExtension(input.LocalizedFile.Name);
        if (!string.Equals(localizedExtension, ".xlf", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(localizedExtension, ".xliff", StringComparison.OrdinalIgnoreCase))
        {
            throw new PluginMisconfigurationException(
                "The localized file must be an XLIFF file with an .xlf or .xliff extension.");
        }

        var localizedFileStream = await FileManagementClient.DownloadAsync(input.LocalizedFile);
        var loadResult = Transformation.Load(
            localizedFileStream,
            input.LocalizedFile.Name,
            input.LocalizedFile.ContentType);

        if (!loadResult.Success)
        {
            throw new PluginMisconfigurationException(
                $"The localized file could not be read: {loadResult.Error}");
        }

        var replacements = loadResult.Value.GetUnits()
            .SelectMany(unit => unit.Segments)
            .Select((segment, index) => new
            {
                order = index + 1,
                source = segment.GetSource(),
                target = segment.GetTarget()
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.source) && !string.IsNullOrWhiteSpace(x.target))
            .ToList();

        if (replacements.Count == 0)
        {
            throw new PluginMisconfigurationException(
                "The localized XLIFF file does not contain any segments with both source and target text.");
        }

        var replacementsJson = JsonConvert.SerializeObject(replacements, Formatting.Indented);
        var prompt = "Edit the supplied original image by replacing its visible source-language text according to " +
                     "the exact source-to-target mappings below. Use every target string exactly as supplied; do not " +
                     "translate, paraphrase, correct, omit, duplicate, or invent text. Match replacements by source " +
                     "text and reading order. Preserve all non-text content, layout, geometry, colors, icons, imagery, " +
                     "lighting, and visual style. Remove the original source-language text. Adjust font size or line " +
                     "breaks only when necessary to fit the original text region. Do not add explanations, labels, " +
                     "watermarks, or any text not present in the mappings.\n\n" +
                     $"Text mappings:\n{replacementsJson}";

        if (!string.IsNullOrWhiteSpace(input.AdditionalInstructions))
        {
            prompt += $"\n\nAdditional instructions:\n{input.AdditionalInstructions.Trim()}";
        }

        var imageStream = await FileManagementClient.DownloadAsync(input.Image);
        var imageBytes = await imageStream.GetByteData();
        var contentType = GetImageContentType(input.Image);

        var request = new OpenAIRequest("/images/edits", Method.Post);
        request.AddFile("image[]", imageBytes, input.Image.Name, contentType);
        request.AddParameter("model", modelIdentifier.ModelId);
        request.AddParameter("prompt", prompt);
        request.AddParameter("output_format", "png");

        var response = await UniversalClient.ExecuteWithErrorHandling<DataDto<ImageDataDto>>(request);
        var imageData = response.Data.FirstOrDefault();
        if (imageData == null || string.IsNullOrWhiteSpace(imageData.Base64))
        {
            throw new PluginApplicationException("OpenAI did not return a generated image.");
        }

        var outputBytes = Convert.FromBase64String(imageData.Base64);
        using var outputStream = new MemoryStream(outputBytes);
        var outputName = BuildOutputName(input.OutputImageName, input.Image.Name, ".png");
        var outputFile = await FileManagementClient.UploadAsync(outputStream, "image/png", outputName);

        return new() { Image = outputFile };
    }

    private static ImageLocalizationExtractionDto DeserializeExtraction(string content)
    {
        try
        {
            return JsonConvert.DeserializeObject<ImageLocalizationExtractionDto>(content)
                   ?? throw new JsonSerializationException("The response was empty.");
        }
        catch (JsonException ex)
        {
            throw new PluginApplicationException(
                $"Could not parse localizable image content returned by OpenAI: {ex.Message}");
        }
    }

    private static Transformation CreateImageTextTransformation(
        IEnumerable<string> texts,
        string sourceLanguage,
        FileReference image,
        string outputName)
    {
        var coder = new PlaintextCoder();
        var transformation = new Transformation(sourceLanguage, null)
        {
            OriginalName = image.Name,
            OriginalMediaType = GetImageContentType(image),
            BilingualFileName = outputName
        };

        foreach (var (text, index) in texts.Select((text, index) => (text, index)))
        {
            transformation.Children.Add(new Unit(coder)
            {
                Segments =
                [
                    new Segment(coder)
                    {
                        Source = coder.DeserializeSegment(text),
                        Order = index + 1
                    }
                ]
            });
        }

        return transformation;
    }

    private static void ValidateImage(FileReference image, bool allowGif)
    {
        if (!image.IsImage())
        {
            throw new PluginMisconfigurationException(
                $"The supplied file '{image.Name}' is not a supported image. Use PNG, JPEG, WEBP, or GIF.");
        }

        if (!allowGif && (string.Equals(image.ContentType, "image/gif", StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(Path.GetExtension(image.Name), ".gif", StringComparison.OrdinalIgnoreCase)))
        {
            throw new PluginMisconfigurationException(
                "GIF files are not supported for localized image generation. Use PNG, JPEG, or WEBP.");
        }
    }

    private static string GetImageContentType(FileReference image)
    {
        if (!string.IsNullOrWhiteSpace(image.ContentType))
        {
            return image.ContentType;
        }

        return Path.GetExtension(image.Name).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => throw new PluginMisconfigurationException(
                $"Content type is required for image input '{image.Name}'.")
        };
    }

    private static string BuildOutputName(string? requestedName, string inputName, string extension)
    {
        var baseName = string.IsNullOrWhiteSpace(requestedName)
            ? Path.GetFileNameWithoutExtension(inputName)
            : Path.GetFileNameWithoutExtension(requestedName.Trim());

        return $"{baseName}{extension}";
    }
}
