using System;
using System.Collections.Generic;
using System.Linq;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models;

namespace Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;

public static class TextChatModelOrdering
{
    public static bool IsRelevantTextModel(string modelId) => new OpenAiModel(modelId).IsRelevantTextModel();

    public static IReadOnlyList<ModelDto> Sort(IEnumerable<ModelDto> models)
    {
        return models
            .Select(model => new { Model = model, Info = new OpenAiModel(model.Id) })
            .GroupBy(x => x.Info.GetFamilyKey(), StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Max(x => x.Model.Created))
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .SelectMany(group => group
                .OrderBy(x => x.Info.IsSnapshot() ? 1 : 0)
                .ThenByDescending(x => x.Model.Created)
                .ThenBy(x => x.Model.Id, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.Model))
            .ToList();
    }

    public static string? SelectDefaultTextModel(IEnumerable<ModelDto> models)
    {
        var orderedModels = Sort(models.Where(model => IsRelevantTextModel(model.Id)))
            .Select(model => new { Model = model, Info = new OpenAiModel(model.Id) })
            .ToList();

        return orderedModels.FirstOrDefault(model => model.Info.IsGenericGptAlias())?.Model.Id
            ?? orderedModels.FirstOrDefault(model => model.Info.IsGenericGptSnapshot())?.Model.Id
            ?? orderedModels.FirstOrDefault(model => model.Info.IsGptProModel())?.Model.Id
            ?? orderedModels.FirstOrDefault(model => model.Info.IsGptMiniOrNanoModel())?.Model.Id
            ?? orderedModels.FirstOrDefault(model => model.Info.IsOModel())?.Model.Id;
    }
}
