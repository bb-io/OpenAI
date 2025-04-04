using System.Collections.Generic;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Entities;

namespace Apps.OpenAI.Models.PostEdit;

public record OpenAICompletionResult(
    bool IsSuccess,
    UsageDto Usage,
    List<string> Errors,
    List<TranslationEntity> Translations);
