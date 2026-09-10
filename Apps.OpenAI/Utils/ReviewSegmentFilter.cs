using System;
using System.Collections.Generic;
using System.Linq;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Transformations;

namespace Apps.OpenAI.Utils;

public static class ReviewSegmentFilter
{
    public static Func<Segment, bool> Create(IEnumerable<string>? excludedStates)
    {
        var values = excludedStates?.ToList();
        if (values == null || values.Count == 0)
            return segment => !segment.IsIgnorbale && !segment.IsInitial
                && segment.State != SegmentState.Final
                && !string.IsNullOrWhiteSpace(segment.GetTarget());

        var exclusions = new HashSet<SegmentState>();
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value)
                || !Enum.GetNames<SegmentState>().Contains(value, StringComparer.OrdinalIgnoreCase)
                || !Enum.TryParse<SegmentState>(value, true, out var state))
                throw new PluginMisconfigurationException(
                    $"Invalid excluded segment state '{value}'. Choose Initial, Translated, Reviewed, or Final.");

            exclusions.Add(state);
        }

        return segment => !segment.IsIgnorbale
            && !exclusions.Contains(segment.State ?? SegmentState.Initial)
            && !string.IsNullOrWhiteSpace(segment.GetTarget());
    }
}
