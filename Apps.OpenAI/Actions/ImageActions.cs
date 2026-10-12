using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Apps.OpenAI.Actions.Base;
using Apps.OpenAI.Api.Requests;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Requests.Image;
using Apps.OpenAI.Models.Responses.Chat;
using Apps.OpenAI.Models.Responses.Image;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.Files;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
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

        var sourceLanguageInstruction = string.IsNullOrWhiteSpace(input.SourceLanguage)
            ? "Detect the language of the extracted text and return its BCP-47 language code."
            : $"The source language is '{input.SourceLanguage}'. Return this exact value in source_language.";

        var prompt = "Perform OCR on the supplied image and extract all visible text that should be localized. " +
                     "Return each complete text item as a separate segment in natural reading order. " +
                     "Preserve the exact source spelling, punctuation, capitalization, numbers, and meaningful line breaks. " +
                     "Do not translate, summarize, correct, merge unrelated text, or include descriptions of visual elements. " +
                     "If the image contains no localizable text, return an empty segments array. " +
                     sourceLanguageInstruction;

        Transformation transformation;
        await using (var imageStream = await FileManagementClient.DownloadAsync(input.Image))
        {
            var imageLoadResult = Transformation.Load(
                imageStream,
                input.Image.Name,
                input.Image.ContentType);

            if (!imageLoadResult.Success)
            {
                throw new PluginMisconfigurationException(
                    $"The original image could not be embedded in the XLIFF file: {imageLoadResult.Error}");
            }

            transformation = imageLoadResult.Value!;
        }

        if (string.IsNullOrWhiteSpace(transformation.Original) ||
            string.IsNullOrWhiteSpace(transformation.OriginalMediaType))
        {
            throw new PluginMisconfigurationException(
                "The original image could not be embedded in the XLIFF file.");
        }

        var (extraction, usage) = await ExtractImageLocalizationContent(
            prompt,
            modelIdentifier.ModelId,
            input,
            transformation.OriginalMediaType,
            transformation.Original);
        var segments = extraction.Segments
            .Select(x => x.Text?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .ToList();

        var sourceLanguage = !string.IsNullOrWhiteSpace(input.SourceLanguage)
            ? input.SourceLanguage!
            : extraction.SourceLanguage?.Trim();

        if (segments.Count > 0 && string.IsNullOrWhiteSpace(sourceLanguage))
        {
            throw new PluginApplicationException("The model extracted text but did not return its source language.");
        }

        var outputName = BuildOutputName(input.OutputFileName, input.Image.Name, ".xlf");
        transformation.SourceLanguage = string.IsNullOrWhiteSpace(sourceLanguage) ? null : sourceLanguage;
        transformation.BilingualFileName = outputName;

        foreach (var text in segments)
        {
            transformation.AddTranslationUnit(text);
        }

        using var outputStream = transformation.ToStream();
        var file = await FileManagementClient.UploadAsync(outputStream, MediaTypes.Xliff2, outputName);

        return new()
        {
            File = file,
            SourceLanguage = sourceLanguage,
            SegmentsCount = segments.Count,
            Usage = usage
        };
    }

    [Action("Generate localized image variant", Description = "Generates a localized image using the original image and translations embedded in an XLIFF file.")]
    public async Task<ImageResponse> GenerateLocalizedImageVariant(
        [ActionParameter] ImageEditingModelIdentifier modelIdentifier,
        [ActionParameter] GenerateLocalizedImageVariantRequest input)
    {
        ThrowForAzure("image");

        var localizedExtension = Path.GetExtension(input.LocalizedFile.Name);
        if (!string.Equals(localizedExtension, ".xlf", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(localizedExtension, ".xliff", StringComparison.OrdinalIgnoreCase))
        {
            throw new PluginMisconfigurationException(
                "The localized file must be an XLIFF file with an .xlf or .xliff extension.");
        }

        Transformation transformation;
        await using (var localizedFileStream = await FileManagementClient.DownloadAsync(input.LocalizedFile))
        {
            var loadResult = Transformation.Load(
                localizedFileStream,
                input.LocalizedFile.Name,
                input.LocalizedFile.ContentType);

            if (!loadResult.Success)
            {
                throw new PluginMisconfigurationException(
                    $"The localized file could not be read: {loadResult.Error}");
            }

            transformation = loadResult.Value!;
        }

        var replacements = transformation.GetUnits()
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

        var sourceResult = transformation.Source();
        if (!sourceResult.Success)
        {
            throw new PluginMisconfigurationException(
                $"The original image could not be restored from the localized XLIFF file: {sourceResult.Error}");
        }

        var sourceContent = sourceResult.Value!;

        Stream RestoreEmbeddedImage()
        {
            try
            {
                return sourceContent.ToStream();
            }
            catch (Exception ex)
            {
                throw new PluginMisconfigurationException(
                    $"The original image embedded in the localized XLIFF file could not be restored: {ex.Message}");
            }
        }

        var replacementsJson = JsonConvert.SerializeObject(replacements, Formatting.Indented);
        var languageContext = BuildLanguageContext(
            transformation.SourceLanguage,
            transformation.TargetLanguage);
        var prompt = "Edit the supplied original image by replacing its visible source-language text according to " +
                     "the exact source-to-target mappings below. Use every target string exactly as supplied; do not " +
                     "translate, paraphrase, correct, omit, duplicate, or invent text. Match replacements by source " +
                     "text and reading order. Preserve all non-text content, layout, geometry, colors, icons, imagery, " +
                     "lighting, and visual style. Remove the original source-language text. Adjust font size or line " +
                     "breaks only when necessary to fit the original text region. Do not add explanations, labels, " +
                     $"watermarks, or any text not present in the mappings.{languageContext}\n\n" +
                     $"Text mappings:\n{replacementsJson}";

        if (!string.IsNullOrWhiteSpace(input.AdditionalInstructions))
        {
            prompt += $"\n\nAdditional instructions:\n{input.AdditionalInstructions.Trim()}";
        }

        var request = new OpenAIRequest("/images/edits", Method.Post);
        request.AddFile(
            "image[]",
            RestoreEmbeddedImage,
            sourceContent.OriginalName,
            sourceContent.OriginalMediaType);
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
        var outputName = BuildOutputName(input.OutputImageName, sourceContent.OriginalName, ".png");
        var outputFile = await FileManagementClient.UploadAsync(outputStream, "image/png", outputName);

        return new() { Image = outputFile };
    }

    private async Task<(ImageLocalizationExtractionDto Extraction, UsageDto Usage)> ExtractImageLocalizationContent(
        string prompt,
        string modelId,
        ExtractLocalizableContentFromImageRequest input,
        string imageMediaType,
        string imageBase64)
    {
        var messages = new List<ChatImageMessageDto>
        {
            new(MessageRoles.User,
            [
                new ChatImageMessageTextContentDto("text", prompt),
                new ChatImageMessageImageContentDto("image_url", new ImageUrlDto(
                    $"data:{imageMediaType};base64,{imageBase64}"))
            ])
        };

        var response = await ExecuteApiRequestAsync(
            messages,
            modelId,
            input,
            ResponseFormats.GetImageLocalizationExtractionResponseFormat());

        return (DeserializeExtraction(response.Choices.First().Message.Content), response.Usage);
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



    private static string BuildLanguageContext(string? sourceLanguage, string? targetLanguage)
    {
        if (string.IsNullOrWhiteSpace(sourceLanguage) && string.IsNullOrWhiteSpace(targetLanguage))
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(sourceLanguage) && !string.IsNullOrWhiteSpace(targetLanguage))
        {
            return $" The source language is '{sourceLanguage}' and the target language is '{targetLanguage}'.";
        }

        return !string.IsNullOrWhiteSpace(sourceLanguage)
            ? $" The source language is '{sourceLanguage}'."
            : $" The target language is '{targetLanguage}'.";
    }

    private static string BuildOutputName(string? requestedName, string inputName, string extension)
    {
        var baseName = string.IsNullOrWhiteSpace(requestedName)
            ? Path.GetFileNameWithoutExtension(inputName)
            : Path.GetFileNameWithoutExtension(requestedName.Trim());

        return $"{baseName}{extension}";
    }
}
