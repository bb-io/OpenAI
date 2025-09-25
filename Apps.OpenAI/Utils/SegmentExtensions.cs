using System.Collections.Generic;
using System.Linq;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Transformations;

namespace Apps.OpenAI.Utils;

public static class SegmentExtensions
{
    public static IEnumerable<Segment> GetSegmentsForTranslation(this IEnumerable<Segment> segments)
    {
        return segments.Where(x => !x.IsIgnorbale && x.IsInitial);
    }
    
    public static IEnumerable<Segment> GetSegmentsForEditing(this IEnumerable<Segment> segments)
    {
        return segments.Where(x => !x.IsIgnorbale && x.State == SegmentState.Translated);
    }
}