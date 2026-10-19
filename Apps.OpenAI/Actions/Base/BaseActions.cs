using Apps.OpenAI.Ai;
using Apps.OpenAI.Api;
using Apps.OpenAI.Api.Requests;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Invocables;
using Apps.OpenAI.Models;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Responses.Batch;
using Apps.OpenAI.Utils;
using Apps.OpenAI.Models.Responses.Background;
using Blackbird.Ai.Background;
using Blackbird.Ai.Background.Models;
using Blackbird.Ai.Batching.Models;
using Blackbird.Ai.Content.Models;
using Blackbird.Ai.Files;
using Blackbird.Ai.Files.Models;
using Blackbird.Ai.Llm.Abstract;
using Blackbird.Ai.Llm.Models;
using Blackbird.Ai.Prompts.Models;
using Blackbird.Ai.Text;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Glossaries.Utils.Converters;
using Blackbird.Applications.Sdk.Glossaries.Utils.Dtos;
using Blackbird.Applications.Sdk.Utils.Extensions.Http;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Xliff.Utils;
using Blackbird.Xliff.Utils.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace Apps.OpenAI.Actions.Base;

public abstract class BaseActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : OpenAIInvocable(invocationContext)
{
    protected readonly OpenAiUniversalClient UniversalClient = new OpenAiUniversalClient(invocationContext.AuthenticationCredentialsProviders);

    protected readonly IFileManagementClient FileManagementClient = fileManagementClient;

    protected string? GetGlossaryPromptPart(Glossary blackbirdGlossary, string sourceContent, bool filter)
    {
        var glossaryPromptPart = new StringBuilder();
        glossaryPromptPart.AppendLine();
        glossaryPromptPart.AppendLine();
        glossaryPromptPart.AppendLine("Glossary entries (each entry includes terms in different language. Each " +
                                      "language may have a few synonymous variations which are separated by ;;):");

        var entriesIncluded = false;
        foreach (var entry in blackbirdGlossary.ConceptEntries)
        {
            var allTerms = entry.LanguageSections.SelectMany(x => x.Terms.Select(y => y.Term));
            if (filter && !allTerms.Any(x => Regex.IsMatch(sourceContent, $@"\b{x}\b", RegexOptions.IgnoreCase))) continue;
            entriesIncluded = true;

            glossaryPromptPart.AppendLine();
            glossaryPromptPart.AppendLine("\tEntry:");

            foreach (var section in entry.LanguageSections)
            {
                glossaryPromptPart.AppendLine(
                    $"\t\t{section.LanguageCode}: {string.Join(";; ", section.Terms.Select(term => term.Term))}");
            }
        }

        return entriesIncluded ? glossaryPromptPart.ToString() : null;
    }

    protected async Task<string> GetGlossaryPromptPart(FileReference glossary, string sourceContent, bool filter)
    {
        if(!glossary.Name.EndsWith(".tbx", StringComparison.OrdinalIgnoreCase))
        {
            var extension = Path.GetExtension(glossary.Name);
            throw new PluginMisconfigurationException($"Glossary file must be in TBX format. But the provided file has {extension} extension.");
        }

        var glossaryStream = await FileManagementClient.DownloadAsync(glossary);
        using var sanitizedStream = await ToSanitizedMemoryStreamAsync(glossaryStream);

        var blackbirdGlossary = await sanitizedStream.ConvertFromTbx();
        
        return GetGlossaryPromptPart(blackbirdGlossary, sourceContent, filter);
    }

    protected async Task<XliffDocument> DownloadXliffDocumentAsync(FileReference file)
    {
        var fileStream = await FileManagementClient.DownloadAsync(file);
        var xliffMemoryStream = new MemoryStream();
        await fileStream.CopyToAsync(xliffMemoryStream);
        xliffMemoryStream.Position = 0;

        XliffDocument xliffDocument;
        try
        {
            xliffDocument = xliffMemoryStream.ToXliffDocument();
        }
        catch(InvalidOperationException ex) when (ex.Message.Contains("Unsupported XLIFF version"))
        {
            throw new PluginMisconfigurationException("Unsupported XLIFF version. This action supports XLIFF 1.2, 2.1 and 2.2 versions only");
        }
        catch (XmlException ex)
        {
            throw new PluginMisconfigurationException("Incorrect XLIFF file structure. Check if the file complies with the XLIFF structure");
        }
        if (xliffDocument.TranslationUnits.Count == 0)
        {
            throw new PluginMisconfigurationException("The XLIFF file does not contain any translation units. Please check your input file");
        }

        return xliffDocument;
    }

    protected Task<string> ResolveTextChatModelAsync(string? model)
        => UniversalClient.ResolveTextChatModelAsync(model);

    protected virtual ILlmClient CreateLlmClient(string? model, OpenAiLlmSettings settings)
        => new OpenAiLlmClient(UniversalClient, model, settings);

    protected virtual IBatchLlmClient CreateBatchClient(string? model, OpenAiLlmSettings settings)
        => new OpenAiBatchLlmClient(UniversalClient, model, settings);

    protected ContentStreamProcessor CreateContentProcessor(ILlmClient client, string? model, int? bucketSize = null)
        => new(client, ContentEngineOptions(model, bucketSize));

    // Starting a job needs the synchronous client for language detection and the summary; downloading does not.
    protected BackgroundContentProcessor CreateBackgroundProcessor(
        IBatchLlmClient batchClient,
        ILlmClient? client = null,
        string? model = null,
        int? bucketSize = null)
        => new(batchClient, client, ContentEngineOptions(model, bucketSize));

    // Full traces keep the user prompt, which the text action responses report.
    protected TextProcessor CreateTextProcessor(ILlmClient client, string? model)
        => new(client, new EngineOptions
        {
            Logger = AiAdapters.Logger(InvocationContext),
            LogTag = "[OpenAI]",
            ProvenanceTool = model ?? "OpenAI",
            TraceLevel = PromptTraceLevel.Full
        });

    protected static string LastUserPrompt(ContentResult content)
        => content.Traces.LastOrDefault()?.UserPrompt ?? string.Empty;

    protected async Task<Stream?> DownloadOptionalAsync(FileReference? file)
        => file is null ? null : await FileManagementClient.DownloadAsync(file);

    protected async Task<FileReference> UploadAsync(ContentStream file)
    {
        await using var content = file.Content;
        return await FileManagementClient.UploadAsync(content, file.MediaType, file.FileName);
    }

    protected static UsageDto ToUsageDto(LlmUsage usage, ILlmClient client, string? requestedModel)
        => ToUsageDto(usage, (client as OpenAiLlmClient)?.ModelUsed ?? requestedModel);

    protected static UsageDto ToUsageDto(LlmUsage usage, string? modelUsed) => new()
    {
        ModelUsed = modelUsed,
        PromptTokens = usage.InputTokens,
        CompletionTokens = usage.OutputTokens,
        TotalTokens = usage.TotalTokens
    };

    protected async Task<ChatCompletionDto> ExecuteApiRequestAsync(IEnumerable<object> messages, string? model, BaseChatRequest input = null, object responseFormat = null)
    {
        var body = await GenerateResponseBodyAsync(messages, model, input, responseFormat);
        return await UniversalClient.ExecuteApiRequestAsync(body);
    }

    protected async Task<Dictionary<string, object>> GenerateResponseBodyAsync(
        IEnumerable<object> messages,
        string? model,
        BaseChatRequest input = null,
        object responseFormat = null)
    {
        var resolvedModel = await ResolveTextChatModelAsync(model);
        var openAiModel = new OpenAiModel(resolvedModel);
        var body = new Dictionary<string, object>
        {
            { "model", resolvedModel },
            { "store", false },
            { "input", MapMessagesToResponsesInput(messages) }
        };

        if (input?.TopP != null && openAiModel.SupportsTopP(input.ReasoningEffort))
        {
            body["top_p"] = input.TopP.Value;
        }

        if (input?.Temperature != null)
        {
            body.AppendIfNotNull("temperature", input.Temperature);
        }

        body.AppendIfNotNull("max_output_tokens", input?.MaximumTokens);

        if (openAiModel.SupportsReasoningEffort() && !string.IsNullOrWhiteSpace(input?.ReasoningEffort))
        {
            body["reasoning"] = new Dictionary<string, object>
            {
                ["effort"] = input.ReasoningEffort!
            };
        }

        if (responseFormat != null)
        {
            body["text"] = new Dictionary<string, object>
            {
                ["format"] = responseFormat
            };
        }

        if (input is IWebSearchRequest webSearchRequest && webSearchRequest.EnableWebSearch == true)
        {
            ValidateWebSearchConfiguration(resolvedModel, input, webSearchRequest);

            var webSearchTool = BuildWebSearchTool(webSearchRequest);
            body["tools"] = new[] { webSearchTool };
            body["tool_choice"] = "auto";
            body["include"] = new[] { "web_search_call.action.sources" };
        }

        return body;
    }

    protected OpenAIRequest GenerateBaseAudioRequest(string? model, string endpoint, Method method)
    {
        if (UniversalClient.ConnectionType != ConnectionTypes.AzureOpenAi)
        {
            var request = new OpenAIRequest(endpoint, method);
            request.AddParameter("model", model);
            return request;
        }

        // Azure uses a different endpoint specifically for audio/transcriptions
        // So we need to construct a new request manually
        string rawAzureUrl = Creds.FirstOrDefault(x => x.KeyName == CredNames.Url)?.Value;
        string azureDeploymentName = UniversalClient.GetModel();
        return new OpenAIRequest(
            $"{rawAzureUrl}/openai/deployments/{azureDeploymentName}{endpoint}?api-version=2024-02-01",
            method);
    }

    protected async Task<string> IdentifySourceLanguage(string? modelId, string content)
    {
        var systemPrompt = "You are a linguist. Identify the language of the following text. Your response should be in the BCP 47 (language) or (language-country). You respond with the language only, not other text is required.";

        var snippet = content.Length > 200 ? content[..Math.Min(content.Length, 300)] : content;
        var userPrompt = snippet + ". The BCP 47 language code: ";

        var messages = new List<ChatMessageDto> { new(MessageRoles.System, systemPrompt), new(MessageRoles.User, userPrompt) };
        var response = await ExecuteApiRequestAsync(messages, modelId);

        return response.Choices.First().Message.Content;
    }

    protected Task<BatchResponse> CreateBatchAsync(List<object> requests)
        => UniversalClient.CreateBatchAsync(requests);

    protected async Task<BackgroundProcessingResponse> ToBackgroundResponseAsync(BackgroundStart started, ILlmClient client, string? model)
    {
        var content = started.Content;

        return new BackgroundProcessingResponse
        {
            // With nothing to process no batch is created; the download then returns the file unchanged.
            BatchId = started.Job?.Id ?? string.Empty,
            Status = (started.Job?.State ?? BatchJobState.Completed).ToString().ToLowerInvariant(),
            CreatedAt = started.Job?.CreatedAt?.ToUnixTimeSeconds().ToString(),
            TransformationFile = await UploadAsync(started.WorkFile),
            TotalSegmentsCount = content.TotalSegments,
            SegmentsToProcessCount = content.ProcessableSegments,
            RequestsCount = started.Requests,
            SystemPrompt = content.SystemPrompt ?? string.Empty,
            Usage = ToUsageDto(content.Usage, client, model)
        };
    }

    protected void ThrowForAzure(string actionType)
    {
        if (UniversalClient.ConnectionType == ConnectionTypes.AzureOpenAi)
            throw new PluginMisconfigurationException($"Azure OpenAI does not support {actionType} actions. Please use OpenAI for such tasks");
    }

    private static async Task<MemoryStream> ToSanitizedMemoryStreamAsync(Stream input)
    {
        var memoryStream = new MemoryStream();
        await input.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        if (memoryStream.Length >= 3)
        {
            var bom = new byte[3];
            var read = await memoryStream.ReadAsync(bom, 0, 3);
            if (read == 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
            {
                var cleaned = new MemoryStream();
                await memoryStream.CopyToAsync(cleaned);
                cleaned.Position = 0;
                return cleaned;
            }
            memoryStream.Position = 0;
        }

        return memoryStream;
    }

    private EngineOptions ContentEngineOptions(string? model, int? bucketSize) => new()
    {
        Logger = AiAdapters.Logger(InvocationContext),
        LogTag = "[OpenAI]",
        ProvenanceTool = model ?? "OpenAI",
        Bucketing = BucketingOptions.FromInputs(bucketSize, null)
    };

    private static IEnumerable<object> MapMessagesToResponsesInput(IEnumerable<object> messages)
    {
        return messages.Select(message =>
        {
            if (message is ChatMessageDto textMessage)
            {
                return new Dictionary<string, object>
                {
                    ["role"] = textMessage.Role,
                    ["content"] = textMessage.Content
                };
            }

            if (message is ChatImageMessageDto imageMessage)
            {
                var content = imageMessage.Content.Select(item => item switch
                {
                    ChatImageMessageTextContentDto text => new Dictionary<string, object>
                    {
                        ["type"] = "input_text",
                        ["text"] = text.Text
                    },
                    ChatImageMessageImageContentDto image => new Dictionary<string, object>
                    {
                        ["type"] = "input_image",
                        ["image_url"] = image.Image_url.Url
                    },
                    _ => throw new PluginMisconfigurationException("Unsupported image message content type for Responses API")
                }).Cast<object>().ToList();

                return new Dictionary<string, object>
                {
                    ["role"] = imageMessage.Role,
                    ["content"] = content
                };
            }

            if (message is ChatAudioMessageDto audioMessage)
            {
                var content = audioMessage.Content.Select(item => item switch
                {
                    ChatAudioMessageTextContentDto text => new Dictionary<string, object>
                    {
                        ["type"] = "input_text",
                        ["text"] = text.Text
                    },
                    ChatAudioMessageAudioContentDto audio => new Dictionary<string, object>
                    {
                        ["type"] = "input_audio",
                        ["input_audio"] = new Dictionary<string, object>
                        {
                            ["format"] = audio.input_audio.Format,
                            ["data"] = audio.input_audio.Data
                        }
                    },
                    _ => throw new PluginMisconfigurationException("Unsupported audio message content type for Responses API")
                }).Cast<object>().ToList();

                return new Dictionary<string, object>
                {
                    ["role"] = audioMessage.Role,
                    ["content"] = content
                };
            }

            var anonymousMessage = JObject.FromObject(message);
            return anonymousMessage.ToObject<Dictionary<string, object>>() ?? new Dictionary<string, object>();
        }).ToList();
    }

    private static Dictionary<string, object> BuildWebSearchTool(IWebSearchRequest request)
    {
        var tool = new Dictionary<string, object>
        {
            ["type"] = "web_search"
        };

        if (!string.IsNullOrWhiteSpace(request.WebSearchContextSize))
        {
            tool["search_context_size"] = request.WebSearchContextSize!;
        }

        if (request.ExternalWebAccess.HasValue)
        {
            tool["external_web_access"] = request.ExternalWebAccess.Value;
        }

        var allowedDomains = request.AllowedDomains?
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Replace("https://", string.Empty).Replace("http://", string.Empty).Trim('/'))
            .Distinct()
            .ToList();

        if (allowedDomains != null && allowedDomains.Count != 0)
        {
            tool["filters"] = new Dictionary<string, object>
            {
                ["allowed_domains"] = allowedDomains
            };
        }

        if (!string.IsNullOrWhiteSpace(request.UserLocationCity) ||
            !string.IsNullOrWhiteSpace(request.UserLocationCountry) ||
            !string.IsNullOrWhiteSpace(request.UserLocationRegion) ||
            !string.IsNullOrWhiteSpace(request.UserLocationTimezone))
        {
            var userLocation = new Dictionary<string, object>
            {
                ["type"] = "approximate"
            };

            if (!string.IsNullOrWhiteSpace(request.UserLocationCity))
                userLocation["city"] = request.UserLocationCity!;
            if (!string.IsNullOrWhiteSpace(request.UserLocationCountry))
                userLocation["country"] = request.UserLocationCountry!;
            if (!string.IsNullOrWhiteSpace(request.UserLocationRegion))
                userLocation["region"] = request.UserLocationRegion!;
            if (!string.IsNullOrWhiteSpace(request.UserLocationTimezone))
                userLocation["timezone"] = request.UserLocationTimezone!;

            tool["user_location"] = userLocation;
        }

        return tool;
    }

    private static void ValidateWebSearchConfiguration(string model, BaseChatRequest input, IWebSearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(model))
            throw new PluginMisconfigurationException("Model is required when web search is enabled");

        if (model.Contains("gpt-4.1-nano", StringComparison.OrdinalIgnoreCase))
        {
            throw new PluginMisconfigurationException("Web search is not supported for gpt-4.1-nano");
        }

        if (model.Contains("gpt-5", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(input?.ReasoningEffort, "minimal", StringComparison.OrdinalIgnoreCase))
        {
            throw new PluginMisconfigurationException("Web search is not supported for GPT-5 with minimal reasoning effort. Use low, medium, or high reasoning effort.");
        }

        var allowedDomainsCount = request.AllowedDomains?.Count(x => !string.IsNullOrWhiteSpace(x)) ?? 0;
        if (allowedDomainsCount > 100)
        {
            throw new PluginMisconfigurationException("Web search domain filtering supports up to 100 allowed domains.");
        }
    }
}
