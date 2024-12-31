using Blackbird.Xliff.Utils.Models;

namespace Apps.OpenAI.Utils;

public static class TranslationUnitExtensions
{
    public static bool IsLocked(this TranslationUnit translationUnit)
    {
        if (translationUnit.Attributes.TryGetValue("mq:locked", out var mqXliffLocked))
        {
            if (mqXliffLocked == "locked")
            {
                return true;
            }
        }
        
        if (translationUnit.Attributes.TryGetValue("m:locked", out var mXliffLocked))
        {
            if (mXliffLocked == "true")
            {
                return true;
            }
        }

        return false;
    }
}