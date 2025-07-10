using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Apps.OpenAI.Api;
using Apps.OpenAI.Invocables;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Applications.Sdk.Glossaries.Utils.Converters;
using Blackbird.Xliff.Utils;
using Blackbird.Xliff.Utils.Extensions;
using System.Xml;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Requests.Chat;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using RestSharp;
using System.Collections.Generic;

namespace Apps.OpenAI.Actions.Base;

public abstract class BaseActions : OpenAIInvocable
{
    protected readonly OpenAIClient Client;
    protected readonly IFileManagementClient FileManagementClient;

    protected BaseActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient) 
        : base(invocationContext)
    {
        Client = new OpenAIClient(invocationContext.AuthenticationCredentialsProviders);
        FileManagementClient = fileManagementClient;
    }
    
    protected async Task<string> GetGlossaryPromptPart(FileReference glossary, string sourceContent, bool filter)
    {
        if(!glossary.Name.EndsWith(".tbx", StringComparison.OrdinalIgnoreCase))
        {
            var extension = Path.GetExtension(glossary.Name);
            throw new PluginMisconfigurationException($"Glossary file must be in TBX format. But the provided file has {extension} extension.");
        }

        var glossaryStream = await FileManagementClient.DownloadAsync(glossary);
        var blackbirdGlossary = await glossaryStream.ConvertFromTbx();

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

    protected async Task<ChatCompletionDto> ExecuteChatCompletion(IEnumerable<object> messages, string model = "gpt-4-turbo-preview", BaseChatRequest input = null, object responseFormat = null)
    {
        var jsonBody = new
        {
            model,
            Messages = messages,
            max_completion_tokens = input?.MaximumTokens,
            top_p = input?.TopP ?? 1,
            presence_penalty = input?.PresencePenalty ?? 0,
            frequency_penalty = input?.FrequencyPenalty ?? 0,
            temperature = input?.Temperature ?? 1,
            response_format = responseFormat,
        };

        var jsonBodySerialized = JsonConvert.SerializeObject(jsonBody, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
        });

        var request = new OpenAIRequest("/chat/completions", Method.Post);
        request.AddJsonBody(jsonBodySerialized);

        return await Client.ExecuteWithErrorHandling<ChatCompletionDto>(request);
    }
}