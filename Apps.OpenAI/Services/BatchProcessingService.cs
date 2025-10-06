using Apps.OpenAI.Api;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Entities;
using Apps.OpenAI.Models.PostEdit;
using Apps.OpenAI.Models.Requests.Chat;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Transformations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.OpenAI.Services;
public class BatchProcessingService(OpenAIClient openAIClient, IFileManagementClient fileManagementClient)
{
    public async Task<BatchResult> ProcessBatchAsync(
    Dictionary<string, Segment> batch,
    BatchProcessingOptions options,
    bool postEdit)
    {
        var result = new BatchResult();
        var glossaryService = new ContentGlossaryService(fileManagementClient);
        var promptBuilderService = new ContentPromptBuilderService();

        try
        {
            string? glossaryPrompt = null;
            if (options.Glossary != null)
            {
                glossaryPrompt = await glossaryService.BuildGlossaryPromptAsync(
                    options.Glossary, batch.Select(x => x.Value), options.FilterGlossary);
            }

            var userPrompt = postEdit ? promptBuilderService.BuildPostEditUserPrompt(
                options.SourceLanguage,
                options.TargetLanguage,
                batch,
                options.Prompt,
                glossaryPrompt,
                options.Notes
                ) : promptBuilderService.BuildProcessUserPrompt(
                options.SourceLanguage,
                options.TargetLanguage,
                batch,
                options.Prompt,
                glossaryPrompt,
                options.Notes);

            var messages = new List<ChatMessageDto>
            {
                new(MessageRoles.System, postEdit ? promptBuilderService.GetPostEditSystemPrompt() : promptBuilderService.GetProcessSystemPrompt()),
                new(MessageRoles.User, userPrompt)
            };

            var completionResult = await CallOpenAIAndProcessResponseAsync(
                messages, options);

            result.IsSuccess = completionResult.IsSuccess;
            result.Usage = completionResult.Usage;
            result.ErrorMessages.AddRange(completionResult.Errors);
            result.UpdatedTranslations.AddRange(completionResult.Translations);

            return result;
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessages.Add($"Unexpected error processing batch: {ex.Message}");
            return result;
        }
    }

    private async Task<OpenAICompletionResult> CallOpenAIAndProcessResponseAsync(List<ChatMessageDto> messages, BatchProcessingOptions options)
    {
        var errors = new List<string>();
        var translations = new List<TranslationEntity>();
        var usage = new UsageDto();
        var openaiService = new OpenAICompletionService(openAIClient);
        var deserializationService = new ResponseDeserializationService();

        int currentAttempt = 0;
        bool success = false;
        while (!success && currentAttempt < options.MaxRetryAttempts)
        {
            currentAttempt++;

            var chatCompletionResult = await openaiService.ExecuteChatCompletionAsync(
                messages,
                options.ModelId,
                new BaseChatRequest { MaximumTokens = options.MaxTokens, ReasoningEffort = options.ReasoningEffort},
                ResponseFormats.GetXliffResponseFormat());

            if (!chatCompletionResult.Success)
            {
                var errorMessage = $"Attempt {currentAttempt}/{options.MaxRetryAttempts}: API call failed - {chatCompletionResult.Error ?? "Unknown error during OpenAI completion"}";
                errors.Add(errorMessage);
                continue;
            }

            usage = chatCompletionResult.ChatCompletion.Usage;
            var choice = chatCompletionResult.ChatCompletion.Choices.First();
            var content = choice.Message.Content;

            if (choice.FinishReason == "length")
            {
                errors.Add($"Attempt {currentAttempt}/{options.MaxRetryAttempts}: The response from OpenAI was truncated. Try reducing the batch size.");
            }

            var deserializationResult = deserializationService.DeserializeResponse(content);
            if (deserializationResult.Success)
            {
                success = true;
                translations.AddRange(deserializationResult.Translations);
            }
            else
            {
                errors.Add($"Attempt {currentAttempt}/{options.MaxRetryAttempts}: {deserializationResult.Error}");
            }
        }

        return new OpenAICompletionResult(success, usage, errors, translations);
    }
}
