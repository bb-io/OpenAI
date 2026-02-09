using Apps.OpenAI.Actions.Base;
using Apps.OpenAI.Api;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Entities;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.PostEdit;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Requests.Xliff;
using Apps.OpenAI.Models.Responses.Chat;
using Apps.OpenAI.Services;
using Apps.OpenAI.Utils;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using MoreLinq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;

namespace Apps.OpenAI.Actions;

[ActionList("Deprecated XLIFF")]
public class DeprecatedXliffActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : BaseActions(invocationContext, fileManagementClient)
{
    [Action("Apply prompt to XLIFF file (experimental)",
        Description = "Runs prompt for each translation unit in the XLIFF file according to the provided instructions and updates the target text for each unit. For now it supports only 1.2 version and 2.1 of XLIFF. Supports batching where multiple units will be put into a single prompt.")]
    public async Task<PostEditXliffResponse> PromptXLIFF(
        [ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] PromptXliffRequest input,
        [ActionParameter, Display("User prompt")] string prompt,
        [ActionParameter, Display("System prompt")] string? systemPrompt,
        [ActionParameter] GlossaryRequest glossary,
        [ActionParameter] BaseChatRequest promptRequest,
        [ActionParameter, Display("Bucket size", Description = "Specify the number of translation units to be processed at once. Default value: 1. (See our documentation for an explanation)")]
            int? bucketSize = 1)
    {
        var postEditService = new PostEditService(
            new XliffService(FileManagementClient),
            new JsonGlossaryService(FileManagementClient),
            new OpenAICompletionService(UniversalClient),
            new ResponseDeserializationService(),
            new PromptBuilderService(),
            FileManagementClient);

        var fileExtension = Path.GetExtension(input.File.Name)?.ToLowerInvariant() ?? string.Empty;
        var result = await postEditService.PostEditXliffAsync(new OpenAiXliffInnerRequest
        {
            ModelId = UniversalClient.GetModel(modelIdentifier.ModelId),
            Prompt = prompt,
            SystemPrompt = systemPrompt,
            OverwritePrompts = true,
            XliffFile = input.File,
            Glossary = glossary.Glossary,
            FilterGlossary = input.FilterGlossary ?? true,
            BucketSize = bucketSize ?? 1,
            ProcessOnlyTargetState = input.ProcessOnlyTargetState,
            AddMissingTrailingTags = input.AddMissingTrailingTags ?? false,
            NeverFail = input.NeverFail ?? true,
            BatchRetryAttempts = input.BatchRetryAttempts ?? 2,
            MaxTokens = promptRequest.MaximumTokens,
            DisableTagChecks = input.DisableTagChecks ?? false,
            PostEditLockedSegments = input.UpdateLockedSegments ?? false,
            ModifiedBy = input.ModifiedBy ?? "Blackbird",
            FileExtension = fileExtension
        });

        return new PostEditXliffResponse(result);
    }
}