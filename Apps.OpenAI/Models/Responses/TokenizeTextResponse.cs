using System.Collections.Generic;

namespace Apps.OpenAI.Models.Responses;

public class TokenizeTextResponse
{
    public IEnumerable<int> Tokens { get; set; }
}