using System.Linq;
using System.Text.RegularExpressions;

namespace Apps.OpenAI.Models;

public sealed class OpenAiModel(string? modelId)
{
    private static readonly Regex SnapshotSuffixRegex = new(@"-\d{4}-\d{2}-\d{2}$", RegexOptions.Compiled);
    private static readonly Regex OModelRegex = new(@"^o\d", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly string[] ExcludedTextModelTokens =
    [
        "audio",
        "codex",
        "dall",
        "embedding",
        "image",
        "instruct",
        "moderation",
        "realtime",
        "search",
        "sora",
        "text-similarity",
        "transcribe",
        "tts",
        "vision",
        "whisper"
    ];

    public string Id { get; } = modelId ?? string.Empty;

    public string NormalizedId { get; } = modelId?.Trim().ToLowerInvariant() ?? string.Empty;

    public bool IsSpecified => !string.IsNullOrWhiteSpace(NormalizedId);

    public bool IsSnapshot() => SnapshotSuffixRegex.IsMatch(NormalizedId);

    public string GetFamilyKey()
    {
        var match = SnapshotSuffixRegex.Match(NormalizedId);
        return match.Success
            ? NormalizedId[..match.Index]
            : NormalizedId;
    }

    public bool IsRelevantTextModel()
    {
        if (!IsSpecified)
            return false;

        if (NormalizedId == "chat-latest" || NormalizedId.Contains("chat-latest"))
            return false;

        return ExcludedTextModelTokens.All(token => !NormalizedId.Contains(token));
    }

    public bool IsGenericGptAlias()
        => NormalizedId.StartsWith("gpt-") &&
           !IsSnapshot() &&
           !NormalizedId.Contains("-mini") &&
           !NormalizedId.Contains("-nano") &&
           !NormalizedId.Contains("-pro");

    public bool IsGenericGptSnapshot()
    {
        if (!IsSnapshot())
            return false;

        var familyKey = GetFamilyKey();
        return familyKey.StartsWith("gpt-") &&
               !familyKey.Contains("-mini") &&
               !familyKey.Contains("-nano") &&
               !familyKey.Contains("-pro");
    }

    public bool IsGptProModel()
        => NormalizedId.StartsWith("gpt-") && NormalizedId.Contains("-pro");

    public bool IsGptMiniOrNanoModel()
        => NormalizedId.StartsWith("gpt-") &&
           (NormalizedId.Contains("-mini") || NormalizedId.Contains("-nano"));

    public bool IsOModel() => OModelRegex.IsMatch(NormalizedId);

    public bool SupportsReasoningEffort()
        => NormalizedId.StartsWith("gpt-5") || IsOModel();

    public bool SupportsTopP(string? reasoningEffort)
    {
        if (!IsSpecified)
            return true;

        var normalizedReasoningEffort = reasoningEffort?.Trim().ToLowerInvariant();

        if (NormalizedId.StartsWith("gpt-5"))
        {
            if (NormalizedId.StartsWith("gpt-5.1") || NormalizedId.StartsWith("gpt-5.2"))
                return normalizedReasoningEffort == "none";

            return false;
        }

        return true;
    }
}
