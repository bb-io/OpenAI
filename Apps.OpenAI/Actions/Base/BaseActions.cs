using Apps.OpenAI.Api;
using Apps.OpenAI.Api.Requests;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Invocables;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Responses.Batch;
using Apps.OpenAI.Utils;
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
    
    /// <summary>
    /// Creates an optimized glossary lookup structure for efficient term filtering
    /// </summary>
    protected Dictionary<string, List<GlossaryEntry>> CreateGlossaryLookup(Glossary glossary)
    {
        var glossaryLookup = new Dictionary<string, List<GlossaryEntry>>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var conceptEntry in glossary.ConceptEntries)
        {
            foreach (var languageSection in conceptEntry.LanguageSections)
            {
                foreach (var term in languageSection.Terms)
                {
                    var normalizedTerm = term.Term.ToLowerInvariant();
                    if (!glossaryLookup.TryGetValue(normalizedTerm, out var entries))
                    {
                        entries = new List<GlossaryEntry>();
                        glossaryLookup[normalizedTerm] = entries;
                    }
                    
                    entries.Add(new GlossaryEntry
                    {
                        Term = term.Term,
                        LanguageCode = languageSection.LanguageCode,
                        ConceptEntry = conceptEntry
                    });
                }
            }
        }
        
        return glossaryLookup;
    }
    
    /// <summary>
    /// Gets relevant glossary entries for the given source content using an optimized lookup
    /// </summary>
    protected HashSet<GlossaryConceptEntry> GetRelevantGlossaryEntries(
        Dictionary<string, List<GlossaryEntry>> glossaryLookup, 
        string sourceContent)
    {
        var relevantEntries = new HashSet<GlossaryConceptEntry>();
        
        // Extract all words from the source content for efficient matching
        var words = Regex.Matches(sourceContent, @"\b\w+\b")
            .Cast<Match>()
            .Select(m => m.Value.ToLowerInvariant())
            .ToHashSet();
            
        foreach (var word in words)
        {
            if (glossaryLookup.TryGetValue(word, out var entries))
            {
                foreach (var entry in entries)
                {
                    relevantEntries.Add(entry.ConceptEntry);
                }
            }
        }
        
        return relevantEntries;
    }
    
    /// <summary>
    /// Generates glossary prompt part using optimized lookup structure
    /// </summary>
    protected string? GetOptimizedGlossaryPromptPart(Dictionary<string, List<GlossaryEntry>> glossaryLookup, string sourceContent)
    {
        var relevantEntries = GetRelevantGlossaryEntries(glossaryLookup, sourceContent);
        
        if (!relevantEntries.Any())
            return null;
            
        var glossaryPromptPart = new StringBuilder();
        glossaryPromptPart.AppendLine();
        glossaryPromptPart.AppendLine("Glossary entries (each entry includes terms in different language. " +
                                      "Each language may have a few synonymous variations which are separated by ;;). " +
                                      "Use these terms in translation:");

        foreach (var entry in relevantEntries)
        {
            glossaryPromptPart.AppendLine($"\tEntry:");
            foreach (var section in entry.LanguageSections)
            {
                glossaryPromptPart.AppendLine(
                    $"\t\t{section.LanguageCode}: {string.Join(";; ", section.Terms.Select(term => term.Term))}");
            }
        }

        return glossaryPromptPart.ToString();
    }

    protected async Task<Glossary?> ProcessGlossaryFromFile(FileReference? glossaryFile)
    {
        if (glossaryFile == null)
            return null;
            
        if (!glossaryFile.Name.EndsWith(".tbx", StringComparison.OrdinalIgnoreCase))
        {
            var extension = Path.GetExtension(glossaryFile.Name);
            throw new PluginMisconfigurationException($"Glossary file must be in TBX format. But the provided file has {extension} extension.");
        }

        var glossaryStream = await FileManagementClient.DownloadAsync(glossaryFile);
        return await glossaryStream.ConvertFromTbx();
    }
    
    protected class GlossaryEntry
    {
        public string Term { get; set; }
        public string LanguageCode { get; set; }
        public GlossaryConceptEntry ConceptEntry { get; set; }
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

    protected async Task<ChatCompletionDto> ExecuteApiRequestAsync(IEnumerable<object> messages, string model, BaseChatRequest input = null, object responseFormat = null)
    {
        var body = GenerateResponseBody(messages, model, input, responseFormat);
        return await UniversalClient.ExecuteApiRequestAsync(body);
    }

    protected Dictionary<string, object> GenerateResponseBody(
        IEnumerable<object> messages,
        string model,
        BaseChatRequest input = null,
        object responseFormat = null)
    {
        var body = new Dictionary<string, object>
        {
            { "model", UniversalClient.GetModel(model) },
            { "store", false },
            { "input", MapMessagesToResponsesInput(messages) },
            { "top_p", input?.TopP ?? 1 }
        };

        if (input?.Temperature != null)
        {
            body.AppendIfNotNull("temperature", input.Temperature);
        }

        body.AppendIfNotNull("max_output_tokens", input?.MaximumTokens);

        if (SupportsReasoningEffort(model) && !string.IsNullOrWhiteSpace(input?.ReasoningEffort))
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
            ValidateWebSearchConfiguration(model, input, webSearchRequest);

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

    private static bool SupportsReasoningEffort(string model)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            return false;
        }

        var normalizedModel = model.Trim().ToLowerInvariant();
        return normalizedModel.StartsWith("gpt-5") || normalizedModel.StartsWith("o");
    }

    protected async Task<string> IdentifySourceLanguage(TextChatModelIdentifier modelIdentifier, string content)
    {
        var systemPrompt = "You are a linguist. Identify the language of the following text. Your response should be in the BCP 47 (language) or (language-country). You respond with the language only, not other text is required.";

        var snippet = content.Length > 200 ? content.Substring(0, 300) : content;
        var userPrompt = snippet + ". The BCP 47 language code: ";

        var messages = new List<ChatMessageDto> { new(MessageRoles.System, systemPrompt), new(MessageRoles.User, userPrompt) };
        var response = await ExecuteApiRequestAsync(messages, modelIdentifier.ModelId);

        return response.Choices.First().Message.Content;
    }
    
    protected async Task<BatchResponse> CreateBatchAsync(List<object> requests)
    {
        using var memoryStream = new MemoryStream();
        await using var streamWriter = new StreamWriter(memoryStream, Encoding.Default);
        foreach (var requestObj in requests)
        {
            var json = JsonConvert.SerializeObject(requestObj);
            await streamWriter.WriteLineAsync(json);
        }

        await streamWriter.FlushAsync();
        memoryStream.Position = 0;

        var bytes = memoryStream.ToArray();

        var uploadFileRequest = new OpenAIRequest("/files", Method.Post)
            .AddFile("file", bytes, $"{Guid.NewGuid()}.jsonl", "application/jsonl")
            .AddParameter("purpose", "batch");
        var file = await UniversalClient.ExecuteWithErrorHandling<FileDto>(uploadFileRequest);

        var createBatchRequest = new OpenAIRequest("/batches", Method.Post)
            .WithJsonBody(new
            {
                input_file_id = file.Id,
                endpoint = "/v1/responses",
                completion_window = "24h",
            });
        
        return await UniversalClient.ExecuteWithErrorHandling<BatchResponse>(createBatchRequest);
    }

    protected void ThrowForAzure(string actionType)
    {
        if (UniversalClient.ConnectionType == ConnectionTypes.AzureOpenAi)
            throw new PluginMisconfigurationException($"Azure OpenAI does not support {actionType} actions. Please use OpenAI for such tasks");
    }
}