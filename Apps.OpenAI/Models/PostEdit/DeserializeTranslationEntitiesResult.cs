using System.Collections.Generic;
using Apps.OpenAI.Models.Entities;

namespace Apps.OpenAI.Models.PostEdit;

public record DeserializeTranslationEntitiesResult(List<TranslationEntity> Translations, bool Success, string Error);
