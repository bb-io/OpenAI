using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Apps.OpenAI.Actions.Base;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Responses.Chat;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Applications.Sdk.Glossaries.Utils.Converters;
using Newtonsoft.Json;
using Blackbird.Applications.Sdk.Glossaries.Utils.Dtos;
using System.Net.Mime;
using Apps.OpenAI.Models.Requests.Glossary;
using Apps.OpenAI.Services;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Transformations;

namespace Apps.OpenAI.Actions;

[ActionList("Glossaries")]
public class GlossaryActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : BaseActions(invocationContext, fileManagementClient)
{
    [Action("Extract glossary", Description = "Extracts glossary terms from text and outputs a glossary file.")]
    public async Task<GlossaryResponse> ExtractGlossary(
        [ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] ExtractGlossaryRequest input)
    {
        return await BuildGlossary(modelIdentifier.ModelId, input, input.Content, input.Languages, input.Name);
    }

    [Action("Extract glossary from XLIFF", Description = "Extracts glossary terms from XLIFF and outputs a glossary file.")]
    public async Task<GlossaryResponse> ExtractGlossaryFromXliff(
        [ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] ExtractGlossaryFromXliffRequest extractInput,
        [ActionParameter] BaseChatRequest chatInput)
    {
        await using var inputFileStream = await fileManagementClient.DownloadAsync(extractInput.File);

        var transformationLoad = Transformation.Load(inputFileStream, extractInput.File.Name);
        if (!transformationLoad.Success)
            throw new PluginMisconfigurationException(transformationLoad.Error);
        
        var transformation = transformationLoad.Value;
        string sourceLang = transformation.SourceLanguage;
        string targetLang = transformation.TargetLanguage;
        
        if (string.IsNullOrEmpty(sourceLang) || string.IsNullOrEmpty(targetLang))
            throw new PluginMisconfigurationException("The XLIFF file must declare both a source and target language");

        var allowedStates = ParseStates(extractInput.SegmentStates);
        var pairs = transformation.GetUnits()
            .Where(u => allowedStates is null || allowedStates.Contains(u.State))
            .Select(u => new
            {
                Source = u.GetSource().GetPlainText(),
                Target = u.GetTarget().GetPlainText(),
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Source))
            .Where(x => !string.IsNullOrWhiteSpace(x.Target));

        var content = string.Join("\n\n", pairs.Select(x => $"{sourceLang}: {x.Source}\n{targetLang}: {x.Target}"));

        if (string.IsNullOrWhiteSpace(content))
            throw new PluginMisconfigurationException("The XLIFF file has no usable bilingual segments to extract terminology from");
        
        var languages = new[] { sourceLang, targetLang };
        return await BuildGlossary(modelIdentifier.ModelId, chatInput, content, languages, extractInput.Name);
    }
    
    private static HashSet<SegmentState>? ParseStates(IEnumerable<string>? states)
    {
        var list = states?.ToList();
        if (list is null || list.Count == 0)
            return null;

        var parsed = new HashSet<SegmentState>();
        foreach (var s in list)
        {
            if (Enum.TryParse<SegmentState>(s, ignoreCase: true, out var state))
                parsed.Add(state);
        }

        return parsed.Count > 0 ? parsed : null;
    }
    
    private async Task<GlossaryResponse> BuildGlossary(
        string modelId,
        BaseChatRequest chatInput,
        string content,
        IEnumerable<string> languages,
        string? name)
    {
        var systemPrompt = ContentPromptBuilderService.BuildGlossaryPrompt(languages);
        var messages = new List<ChatMessageDto>
        {
            new(MessageRoles.System, systemPrompt),
            new(MessageRoles.User, content)
        };

        var response = await ExecuteApiRequestAsync(messages, modelId, chatInput, new { type = "json_object" });

        List<Dictionary<string, string>> items;
        try
        {
            items = JsonConvert.DeserializeObject<GlossaryItemWrapper>(response.Choices.First().Message.Content).Result;
        }
        catch (Exception ex)
        {
            InvocationContext.Logger?.LogError($"[OpenAI Glossaries] Could not parse the output from OpenAI. {ex}", []);
            throw new PluginApplicationException("Could not parse the output from OpenAI");
        }

        var conceptEntries = new List<GlossaryConceptEntry>();
        var counter = 0;
        foreach (var item in items)
        {
            var languageSections = item
                .Select(x => new GlossaryLanguageSection(x.Key, [new(x.Value)]))
                .ToList();

            conceptEntries.Add(new GlossaryConceptEntry((counter++).ToString(), languageSections));
        }

        var glossaryName = name ?? "New glossary";
        var blackbirdGlossary = new Glossary(conceptEntries) { Title = glossaryName };
        await using var stream = blackbirdGlossary.ConvertToTbx();

        return new GlossaryResponse
        {
            UserPrompt = content,
            SystemPrompt = systemPrompt,
            Glossary = await fileManagementClient.UploadAsync(stream, MediaTypeNames.Application.Xml, $"{glossaryName}.tbx"),
            Usage = response.Usage,
        };
    }
}
