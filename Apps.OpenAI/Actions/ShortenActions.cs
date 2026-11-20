using System;
using System.Threading.Tasks;
using Apps.OpenAI.Actions.Base;
using Apps.OpenAI.Ai;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Content;
using Apps.OpenAI.Models.Responses.Content;
using Blackbird.Ai.Content.Models;
using Blackbird.Ai.Files.Models;
using Blackbird.Ai.Text.Models;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;

namespace Apps.OpenAI.Actions;

[ActionList("Shortening")]
public class ShortenActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : BaseActions(invocationContext, fileManagementClient)
{
    [Action("Shorten content", Description = "Shorten translated file content to fit the size restrictions of its units or the maximum length.")]
    public async Task<ShortenContentResponse> ShortenContent(
        [ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] ShortenContentRequest input)
    {
        ThrowIfInvalid(input.MaximumLength, input.Retries);

        var model = modelIdentifier.ModelId;
        var client = CreateLlmClient(model, new());
        var processor = CreateContentProcessor(client, model, input.BucketSize);

        await using var file = await FileManagementClient.DownloadAsync(input.File);
        await using var glossaryFile = await DownloadOptionalAsync(input.Glossary);

        var result = await AiAdapters.Mapped(() => processor.ShortenAsync(new ShortenStreamOptions
        {
            File = file,
            FileName = input.File.Name,
            MediaType = input.File.ContentType,
            OutputFileHandling = input.OutputFileHandling,
            GlossaryTbx = glossaryFile,
            AdditionalInstructions = input.AdditionalInstructions,
            MaxLength = input.MaximumLength,
            MaxRetries = input.Retries ?? ShortenOptions.DefaultMaxRetries
        }));

        var shorten = result.Content.Shorten!;

        return new ShortenContentResponse
        {
            File = await UploadAsync(result.File),
            UnitsWithLimit = shorten.UnitsWithLimit,
            UnitsOverLimit = shorten.UnitsOverLimit,
            UnitsShortened = shorten.UnitsShortened,
            UnitsStillOverLimit = shorten.UnitsStillOverLimit,
            Usage = ToUsageDto(result.Content.Usage, client, model)
        };
    }

    [Action("Shorten text", Description = "Shorten text to fit the maximum length.")]
    public async Task<ShortenTextResponse> ShortenText(
        [ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] ShortenTextRequest input)
    {
        ThrowIfInvalid(input.MaximumLength, input.Retries);

        var model = modelIdentifier.ModelId;
        var client = CreateLlmClient(model, new());
        var processor = CreateTextProcessor(client, model);

        await using var glossaryFile = await DownloadOptionalAsync(input.Glossary);

        var result = await AiAdapters.Mapped(() => processor.ShortenAsync(new ShortenTextOptions
        {
            Text = input.Text,
            MaxLength = input.MaximumLength,
            MaxRetries = input.Retries ?? ShortenOptions.DefaultMaxRetries,
            AdditionalInstructions = input.AdditionalInstructions,
            GlossaryTbx = glossaryFile
        }));

        if (result.Content.UnresolvedSegments > 0)
            throw new PluginApplicationException(string.Join(Environment.NewLine, result.Content.Warnings));

        return new ShortenTextResponse
        {
            ShortenedText = result.Text,
            SystemPrompt = result.Content.SystemPrompt ?? string.Empty,
            Usage = ToUsageDto(result.Content.Usage, client, model)
        };
    }

    private static void ThrowIfInvalid(int? maximumLength, int? retries)
    {
        if (maximumLength is <= 0)
            throw new PluginMisconfigurationException("Maximum length must be greater than zero.");
        if (retries is < 0)
            throw new PluginMisconfigurationException("Retries must be zero or greater.");
    }
}
