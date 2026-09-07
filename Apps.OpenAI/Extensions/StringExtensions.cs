using System;
using System.Collections.Generic;
using System.Linq;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Transformations.Annotation;

namespace Apps.OpenAI.Extensions;

public static class StringExtensions
{
    public static int CountWords(this IEnumerable<LineElement> line)
    {
        return string.Concat(line.Where(IsText).Select(element => element.Value)).CountWords();
    }

    public static int CountWords(this string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Count(token => token.Any(char.IsLetterOrDigit));
    }

    private static bool IsText(LineElement element) => element is not (InlineTag or AnnotationStart or AnnotationEnd);
}