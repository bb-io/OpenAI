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

    public static string? SelectDefaultTextModel(IEnumerable<ModelDto> models)
    {
        var orderedModels = Sort(models.Where(model => IsRelevantTextModel(model.Id))).ToList();

        return orderedModels.FirstOrDefault(model => IsGenericGptAlias(model.Id))?.Id
            ?? orderedModels.FirstOrDefault(model => IsGenericGptSnapshot(model.Id))?.Id
            ?? orderedModels.FirstOrDefault(model => IsGptProModel(model.Id))?.Id
            ?? orderedModels.FirstOrDefault(model => IsGptMiniOrNanoModel(model.Id))?.Id
            ?? orderedModels.FirstOrDefault(model => IsOModel(model.Id))?.Id
            ?? orderedModels.FirstOrDefault()?.Id;
    }

    private static bool IsSnapshot(string modelId) => SnapshotSuffixRegex.IsMatch(modelId);

    private static string GetFamilyKey(string modelId)
    {
        var match = SnapshotSuffixRegex.Match(modelId);
        return match.Success
            ? modelId[..match.Index]
            : modelId;
    }

    private static bool IsGenericGptAlias(string modelId)
    {
        var normalizedModelId = modelId.Trim().ToLowerInvariant();
        return normalizedModelId.StartsWith("gpt-") &&
               !IsSnapshot(normalizedModelId) &&
               !normalizedModelId.Contains("-mini") &&
               !normalizedModelId.Contains("-nano") &&
               !normalizedModelId.Contains("-pro");
    }

    private static bool IsGenericGptSnapshot(string modelId)
    {
        if (!IsSnapshot(modelId))
            return false;

        var familyKey = GetFamilyKey(modelId).Trim().ToLowerInvariant();
        return familyKey.StartsWith("gpt-") &&
               !familyKey.Contains("-mini") &&
               !familyKey.Contains("-nano") &&
               !familyKey.Contains("-pro");
    }

    private static bool IsGptProModel(string modelId)
        => modelId.Trim().ToLowerInvariant().StartsWith("gpt-") &&
           modelId.Contains("-pro", StringComparison.OrdinalIgnoreCase);

    private static bool IsGptMiniOrNanoModel(string modelId)
    {
        var normalizedModelId = modelId.Trim().ToLowerInvariant();
        return normalizedModelId.StartsWith("gpt-") &&
               (normalizedModelId.Contains("-mini") || normalizedModelId.Contains("-nano"));
    }

    private static bool IsOModel(string modelId)
        => modelId.Trim().ToLowerInvariant().StartsWith("o");
}
