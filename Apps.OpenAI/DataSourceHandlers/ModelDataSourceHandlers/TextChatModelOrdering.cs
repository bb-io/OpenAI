using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Apps.OpenAI.Dtos;

namespace Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;

public static class TextChatModelOrdering
{
    private static readonly Regex SnapshotSuffixRegex = new(@"-\d{4}-\d{2}-\d{2}$", RegexOptions.Compiled);
    private static readonly string[] ExcludedModelTokens =
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

    public static bool IsRelevantTextModel(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            return false;

        var normalizedModelId = modelId.Trim().ToLowerInvariant();

        if (normalizedModelId == "chat-latest" || normalizedModelId.Contains("chat-latest"))
            return false;

        return ExcludedModelTokens.All(token => !normalizedModelId.Contains(token));
    }

    public static IReadOnlyList<ModelDto> Sort(IEnumerable<ModelDto> models)
    {
        return models
            .GroupBy(model => GetFamilyKey(model.Id), StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Max(model => model.Created))
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .SelectMany(group => group
                .OrderBy(model => IsSnapshot(model.Id) ? 1 : 0)
                .ThenByDescending(model => model.Created)
                .ThenBy(model => model.Id, StringComparer.OrdinalIgnoreCase))
            .ToList();
    }

    private static bool IsSnapshot(string modelId) => SnapshotSuffixRegex.IsMatch(modelId);

    private static string GetFamilyKey(string modelId)
    {
        var match = SnapshotSuffixRegex.Match(modelId);
        return match.Success
            ? modelId[..match.Index]
            : modelId;
    }
}
