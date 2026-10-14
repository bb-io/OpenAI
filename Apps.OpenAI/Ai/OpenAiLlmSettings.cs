namespace Apps.OpenAI.Ai;

public sealed record OpenAiLlmSettings(
    string? ReasoningEffort = null,
    int? MaxOutputTokens = null,
    float? Temperature = null,
    float? TopP = null);
