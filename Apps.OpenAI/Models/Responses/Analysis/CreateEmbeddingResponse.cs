using System.Collections.Generic;

namespace Apps.OpenAI.Models.Responses.Analysis;

public class CreateEmbeddingResponse
{
    public IEnumerable<double> Embedding { get; set; }
}