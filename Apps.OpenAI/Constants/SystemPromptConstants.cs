namespace Apps.OpenAI.Constants;

public static class SystemPromptConstants
{
    private const string ProcessXliffFile =
        "You will receive a list of texts to process based on the following instructions: {instructions}. " +
        "The goal is to adapt, modify, or translate these texts as required. " +
        "The source and target languages for these operations are: {source_language} and {target_language}. " +
        "Please process each text accordingly and provide only the target text for each source unit. " +
        "Do not include any additional information in your response. This is crucial as the response will be inserted back into the XLIFF file without modifications.";

    public static string ProcessXliffFileWithInstructions(string instructions, string sourceLanguage,
        string targetLanguage)
    {
        return ProcessXliffFile
            .Replace("{instructions}", instructions)
            .Replace("{source_language}", sourceLanguage)
            .Replace("{target_language}", targetLanguage);
    }
}