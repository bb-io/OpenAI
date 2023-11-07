using System.Collections.Generic;

namespace Apps.OpenAI.Dtos;

public record EmbeddingDto(IEnumerable<double> Embedding);