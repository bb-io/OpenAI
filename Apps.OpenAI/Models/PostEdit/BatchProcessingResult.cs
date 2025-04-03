using System.Collections.Generic;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Entities;

namespace Apps.OpenAI.Models.PostEdit;

public record BatchProcessingResult(
    int BatchesProcessed,
    List<TranslationEntity> Results,
    List<UsageDto> Usages,
    List<string> Errors);
