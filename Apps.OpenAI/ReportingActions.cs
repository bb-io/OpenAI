using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Apps.OpenAI.Actions.Base;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Background;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Responses.Background;
using Apps.OpenAI.Models.Responses.Chat;
using Apps.OpenAI.Utils;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Constants;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Extensions;
using Blackbird.Filters.Transformations;

namespace Apps.OpenAI;

[ActionList("Reporting")]
public class ReportingActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : BaseActions(invocationContext, fileManagementClient)
{
    [Action("Create MQM report",
        Description = "Perform an LQA Analysis of the translation. The result will be in the MQM framework form.")]
    public async Task<ChatResponse> GetLqaAnalysis([ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] GetTranslationIssuesRequest input, [ActionParameter] GlossaryRequest glossary)
    {
        var systemPrompt = "Perform an LQA analysis and use the MQM error typology format using all 7 dimensions. " +
                           "Here is a brief description of the seven high-level error type dimensions: " +
                           "1. Terminology – errors arising when a term does not conform to normative domain or organizational terminology standards or when a term in the target text is not the correct, normative equivalent of the corresponding term in the source text. " +
                           "2. Accuracy – errors occurring when the target text does not accurately correspond to the propositional content of the source text, introduced by distorting, omitting, or adding to the message. " +
                           "3. Linguistic conventions  – errors related to the linguistic well-formedness of the text, including problems with grammaticality, spelling, punctuation, and mechanical correctness. " +
                           "4. Style – errors occurring in a text that are grammatically acceptable but are inappropriate because they deviate from organizational style guides or exhibit inappropriate language style. " +
                           "5. Locale conventions – errors occurring when the translation product violates locale-specific content or formatting requirements for data elements. " +
                           "6. Audience appropriateness – errors arising from the use of content in the translation product that is invalid or inappropriate for the target locale or target audience. " +
                           "7. Design and markup – errors related to the physical design or presentation of a translation product, including character, paragraph, and UI element formatting and markup, integration of text with graphical elements, and overall page or window layout. " +
                           "Provide a quality rating for each dimension from 0 (completely bad) to 10 (perfect). You are an expert linguist and your task is to perform a Language Quality Assessment on input sentences. " +
                           "Try to propose a fixed translation that would have no LQA errors. " +
                           "Formatting: use line spacing between each category. The category name should be bold.";

        if (glossary.Glossary != null)
            systemPrompt +=
                " Use the provided glossary entries for the respective languages. If there are discrepancies " +
                "between the translation and glossary, note them in the 'Terminology' part of the report, " +
                "along with terminology problems not related to the glossary.";

        if (input.AdditionalPrompt != null)
            systemPrompt = $"{systemPrompt} {input.AdditionalPrompt}";

        var userPrompt =
            $"{(input.SourceLanguage != null ? $"The {input.SourceLanguage} " : "")}\"{input.SourceText}\" was translated as \"{input.TargetText}\"{(input.TargetLanguage != null ? $" into {input.TargetLanguage}" : "")}.{(input.TargetAudience != null ? $" The target audience is {input.TargetAudience}" : "")}";

        if (glossary.Glossary != null)
        {
            var glossaryPromptPart = await GetGlossaryPromptPart(glossary.Glossary, input.SourceText, true);
            if (glossaryPromptPart != null) userPrompt += glossaryPromptPart;
        }

        var messages = new List<ChatMessageDto> { new(MessageRoles.System, systemPrompt), new(MessageRoles.User, userPrompt) };
        var response = await ExecuteChatCompletion(messages, modelIdentifier.GetModel());

        return new()
        {
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            Message = response.Choices.First().Message.Content,
            Usage = response.Usage,
        };
    }
    
    [Action("Create MQM report in background",
        Description = "Perform an LQA Analysis on a translated file in the MQM framework form.")]
    public async Task<BackgroundProcessingResponse> CreateMqmReportInBackground([ActionParameter] CreateMqmReportInBackgroundRequest request)
    {
        var stream = await fileManagementClient.DownloadAsync(request.File);
        var content = await ErrorHandler.ExecuteWithErrorHandlingAsync(() => Transformation.Parse(stream, request.File.Name));
        
        content.SourceLanguage ??= request.SourceLanguage;
        content.TargetLanguage ??= request.TargetLanguage;
        
        if (content.TargetLanguage == null) 
            throw new PluginMisconfigurationException("The target language is not defined yet. Please assign the target language in this action.");

        if (content.SourceLanguage == null)
        {
            content.SourceLanguage = await IdentifySourceLanguage(request, content.Source().GetPlaintext());
        }

        var segments = ErrorHandler.ExecuteWithErrorHandling(() => content.GetSegments());
        segments = segments.Where(x => !x.IsIgnorbale && x.State == SegmentState.Translated).ToList();

        var batchRequests = new List<object>();
        foreach (var pair in segments.Select((segment, index) => new { Segment = segment, Index = index }))
        {
            var sourceText = pair.Segment.GetSource();
            var targetText = pair.Segment.GetTarget();
            
            var systemPrompt = "Perform an LQA analysis and use the MQM error typology format using all 7 dimensions: " +
                           "1. Terminology – errors arising when a term does not conform to normative domain or organizational terminology standards or when a term in the target text is not the correct, normative equivalent of the corresponding term in the source text. " +
                           "2. Accuracy – errors occurring when the target text does not accurately correspond to the propositional content of the source text, introduced by distorting, omitting, or adding to the message. " +
                           "3. Linguistic conventions  – errors related to the linguistic well-formedness of the text, including problems with grammaticality, spelling, punctuation, and mechanical correctness. " +
                           "4. Style – errors occurring in a text that are grammatically acceptable but are inappropriate because they deviate from organizational style guides or exhibit inappropriate language style. " +
                           "5. Locale conventions – errors occurring when the translation product violates locale-specific content or formatting requirements for data elements. " +
                           "6. Audience appropriateness – errors arising from the use of content in the translation product that is invalid or inappropriate for the target locale or target audience. " +
                           "7. Design and markup – errors related to the physical design or presentation of a translation product, including character, paragraph, and UI element formatting and markup, integration of text with graphical elements, and overall page or window layout. " +
                           "Provide a quality rating for each dimension from 0 (completely bad) to 10 (perfect). You are an expert linguist and your task is to perform a Language Quality Assessment on input sentences. " +
                           "Try to propose a fixed translation that would have no LQA errors. " +
                           "Formatting: use line spacing between each category. The category name should be bold.";

            if (request.AdditionalInstructions != null)
                systemPrompt = $"{systemPrompt} {request.AdditionalInstructions}";

            var userPrompt =
                $"{(content.SourceLanguage != null ? $"The {content.SourceLanguage} " : "")}\"{sourceText}\" was translated as \"{targetText}\"{(content.TargetLanguage != null ? $" into {request.TargetLanguage}" : "")}.{(request.TargetAudience != null ? $" The target audience is {request.TargetAudience}" : "")}";

            if (request.Glossary != null)
            {
                var glossaryPromptPart = await GetGlossaryPromptPart(request.Glossary, sourceText, true);
                if (!string.IsNullOrEmpty(glossaryPromptPart))
                {
                    userPrompt += glossaryPromptPart;
                }
            }
            
            var batchRequest = new
            {
                custom_id = pair.Index.ToString(),
                method = "POST",
                url = "/v1/chat/completions",
                body = new
                {
                    model = request.GetModel(),
                    messages = new object[]
                    {
                        new
                        {
                            role = "system",
                            content = systemPrompt
                        },
                        new
                        {
                            role = "user",
                            content = userPrompt
                        }
                    }
                }
            };

            batchRequests.Add(batchRequest);
        }

        if (!batchRequests.Any())
        {
            throw new PluginApplicationException("The file does not contain any segments to process. Segments must be translated and have a non-ignorable state.");
        }

        var batchResponse = await CreateBatchAsync(batchRequests);
        content.MetaData.Add(new Metadata("background-type", "mqm-report") { Category = [Meta.Categories.Blackbird]});
        
        return new BackgroundProcessingResponse
        {
            BatchId = batchResponse.Id,
            Status = batchResponse.Status,
            CreatedAt = batchResponse.CreatedAt,
            ExpectedCompletionTime = batchResponse.ExpectedCompletionTime,
            TransformationFile = await fileManagementClient.UploadAsync(content.Serialize().ToStream(), MediaTypes.Xliff, content.XliffFileName)
        };
    }
}