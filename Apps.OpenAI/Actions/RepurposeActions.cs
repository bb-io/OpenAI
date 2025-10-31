using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Apps.OpenAI.Actions.Base;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Requests.Content;
using Apps.OpenAI.Models.Responses.Chat;
using Apps.OpenAI.Utils;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Transformations;
using MoreLinq;

namespace Apps.OpenAI.Actions;

[ActionList("Repurposing")]
public class RepurposeActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : BaseActions(invocationContext, fileManagementClient)
{
    [Action("Summarize text",
        Description = "Summarizes text for different target audiences, languages, tone of voices and platforms. Summary extracts a shorter variant of the original text.")]
    public async Task<RepurposeResponse> CreateSummary(
        [ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] [Display("Text")] string content,
        [ActionParameter] RepurposeRequest input,
        [ActionParameter] GlossaryRequest glossary)
    {
        return await HandleRepurposeRequest(
            "You are a text summarizer. Generate a summary of the message of the user. Be very brief, concise and comprehensive.",
            modelIdentifier, content, input, glossary
        );
    }        

    [Action("Summarize", Description = "Summarizes content for different target audiences, languages, tone of voices and platforms. Summary extracts a shorter variant of the original text.")]
    public async Task<RepurposeResponse> CreateContentSummary(
        [ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] ContentRequest file, 
        [ActionParameter] RepurposeRequest input, 
        [ActionParameter] GlossaryRequest glossary)
    {
        var stream = await fileManagementClient.DownloadAsync(file.File);
        var transformation = await ErrorHandler.ExecuteWithErrorHandlingAsync(() => Transformation.Parse(stream, file.File.Name));

        var text = transformation.Target().GetPlaintext();
        if (string.IsNullOrWhiteSpace(text))
        {
            text = transformation.Source().GetPlaintext();
        }

        return await CreateSummary(modelIdentifier, text, input, glossary);
    }

    [Action("Repurpose text",
        Description = "Repurpose text for different target audiences, languages, tone of voices and platforms. Repurpose does not significantly change the length of the content.")]
    public async Task<RepurposeResponse> RepurposeContent(
        [ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] [Display("Original content")] string content, 
        [ActionParameter] RepurposeRequest input, 
        [ActionParameter] GlossaryRequest glossary) =>
        await HandleRepurposeRequest("Repurpose the content of the message of the user", modelIdentifier, content, input, glossary);

    [Action("Repurpose", Description = "Repurpose content for different target audiences, languages, tone of voices and platforms. Repurpose does not significantly change the length of the content.")]
    public async Task<RepurposeResponse> RepurposeContentFromFile(
        [ActionParameter] TextChatModelIdentifier modelIdentifier, 
        [ActionParameter] ContentRequest file, 
        [ActionParameter] RepurposeRequest input, 
        [ActionParameter] GlossaryRequest glossary)
    {
        var stream = await fileManagementClient.DownloadAsync(file.File);
        var transformation = await Transformation.Parse(stream, file.File.Name);

        var text = transformation.Target().GetPlaintext();
        if (string.IsNullOrWhiteSpace(text))
        {
            text = transformation.Source().GetPlaintext();
        }

        return await RepurposeContent(modelIdentifier, text, input, glossary);
    }

    private async Task<RepurposeResponse> HandleRepurposeRequest(
        string initialPrompt,
        TextChatModelIdentifier modelIdentifier, 
        string content, 
        RepurposeRequest input, 
        GlossaryRequest glossary)
    {
        var prompt = @$"
                {initialPrompt}. 
                {input.AdditionalPrompt}. 
                {(input.TargetAudience != null ? $"The target audience is {input.TargetAudience}" : string.Empty)}.
                {input.ToneOfVOice}
                {(input.Locale != null ? $"The response should be in {input.Locale}" : string.Empty)}

            ";

        if (glossary.Glossary != null)
        {
            var glossaryAddition =
                " Enhance the target text by incorporating relevant terms from our glossary where applicable. " +
                "Ensure that the translation aligns with the glossary entries for the respective languages. " +
                "If a term has variations or synonyms, consider them and choose the most appropriate " +
                "translation to maintain consistency and precision. ";

            var glossaryPromptPart = await GetGlossaryPromptPart(glossary.Glossary, content, true);
            if (glossaryPromptPart != null) prompt += (glossaryAddition + glossaryPromptPart);
        }
        var messages = new List<ChatMessageDto> { new(MessageRoles.System, prompt), new(MessageRoles.User, content) };
        var response = await ExecuteChatCompletion(messages, UniversalClient.GetModel(modelIdentifier.ModelId), input);

        return new()
        {
            SystemPrompt = prompt,
            Response = response.Choices.First().Message.Content,
            Usage = response.Usage,
        };
    }
}