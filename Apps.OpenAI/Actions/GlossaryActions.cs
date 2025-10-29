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
using MoreLinq;

namespace Apps.OpenAI.Actions;

[ActionList("Glossaries")]
public class GlossaryActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : BaseActions(invocationContext, fileManagementClient)
{
    [Action("Extract glossary", Description = "Extract glossary terms from a given text. Use in combination with other glossary actions.")]
    public async Task<GlossaryResponse> ExtractGlossary([ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] ExtractGlossaryRequest input)
    {
        var systemPrompt = $"Extract and list all the subject matter terminologies and proper nouns from the text " +
                           $"inputted by the user. Extract words and phrases, instead of sentences. For each term, " +
                           $"provide a terminology entry for the connected language codes: {string.Join(", ", input.Languages)}. Extract words and phrases, instead of sentences. " +
                           $"Return a JSON of the following structure: {{\"result\": [{{{string.Join(", ", input.Languages.Select(x => $"\"{x}\": \"\""))}}}].";

        var messages = new List<ChatMessageDto> { new(MessageRoles.System, systemPrompt), new(MessageRoles.User, input.Content) };
        var response = await ExecuteChatCompletion(
            messages, 
            UniversalClient.GetModel(modelIdentifier.ModelId), 
            input, 
            new { type = "json_object" }
        );

        List<Dictionary<string, string>> items = null;
        try
        {
            items = JsonConvert.DeserializeObject<GlossaryItemWrapper>(response.Choices.First().Message.Content).Result;
        }
        catch
        {
            throw new Exception(
                "Something went wrong parsing the output from OpenAI, most likely due to a hallucination!");
        }

        var conceptEntries = new List<GlossaryConceptEntry>();
        int counter = 0;
        foreach (var item in items)
        {
            var languageSections = item.Select(x =>
                    new GlossaryLanguageSection(x.Key,
                        new List<GlossaryTermSection> { new GlossaryTermSection(x.Value) }))
                .ToList();

            conceptEntries.Add(new GlossaryConceptEntry(counter.ToString(), languageSections));
            ++counter;
        }

        var blackbirdGlossary = new Glossary(conceptEntries);

        var name = input.Name ?? "New glossary";
        blackbirdGlossary.Title = name;
        using var stream = blackbirdGlossary.ConvertToTbx();
        return new GlossaryResponse()
        {
            UserPrompt = input.Content,
            SystemPrompt = systemPrompt,
            Glossary = await FileManagementClient.UploadAsync(stream, MediaTypeNames.Application.Xml, $"{name}.tbx"),
            Usage = response.Usage,
        };
    }
}