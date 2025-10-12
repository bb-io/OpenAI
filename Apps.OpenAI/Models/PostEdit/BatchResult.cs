using System.Collections.Generic;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Entities;

namespace Apps.OpenAI.Models.PostEdit;

public class BatchResult
{
    public List<TranslationEntity> UpdatedTranslations { get; set; } = new();
    public UsageDto Usage { get; set; } = new();
    public List<string> ErrorMessages { get; set; } = new();
    public bool IsSuccess { get; set; } = true;
    public string SystemPrompt { get; set; }
}