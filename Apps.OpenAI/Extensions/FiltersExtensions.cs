using System.Collections.Generic;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Transformations;

namespace Apps.OpenAI.Extensions;

public static class FiltersExtensions
{
    public static bool ShouldBeProcessed(this Segment segment, IReadOnlySet<SegmentState> states)
    {
        if (segment.IsIgnorbale)
        {
            return false;
        }

        var effectiveState = segment.State ?? SegmentState.Initial;
        return states.Contains(effectiveState);
    }
}