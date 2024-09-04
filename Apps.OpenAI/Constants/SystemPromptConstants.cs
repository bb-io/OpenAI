namespace Apps.OpenAI.Constants;

public static class SystemPromptConstants
{
    private const string ProcessXliffFile =
        "You will receive a list of texts to process based on the following instructions: {instructions}. " +
        "The goal is to adapt, modify, or translate these texts as required. " +
        "The source and target languages for these operations are: {source_language} and {target_language}. " +
        "Please process each text accordingly and provide only the target text for each source unit. " +
        "Do not include any additional information in your response. This is crucial as the response will be inserted back into the XLIFF file without modifications.";
    
    private const string PostEditXliffFile =
        "You will receive a list of texts that have been machine translated. Your task is to post-edit these translations according to the following instructions: {instructions}. " +
        "Ensure that the translations are accurate, natural, and fluently expressed in {target_language}, while preserving the meaning of the original text in {source_language}. " +
        "Make corrections where necessary to fix grammar, style, and contextual errors. " +
        "Please provide only the corrected target text for each source unit. " +
        "Do not include any additional information in your response, as the corrected text will be inserted back into the XLIFF file without further modification.";
    
    private const string EvaluateTranslationQuality =
        "You will receive a source text in {source_language} and its corresponding translation in {target_language}. " +
        "Your task is to evaluate the quality of the translation on a scale from 1 to 10, where 1 indicates a poor translation and 10 indicates an excellent translation. " +
        "Consider the following criteria: accuracy, fluency, grammatical correctness, and preservation of meaning. " +
        "Provide only the score (a number between 1 and 10) as your response, without. Do not include any additional text or explanations.";
    
    public static string ProcessXliffFileWithInstructions(string instructions, string sourceLanguage,
        string targetLanguage)
    {
        return ProcessXliffFile
            .Replace("{instructions}", instructions)
            .Replace("{source_language}", sourceLanguage)
            .Replace("{target_language}", targetLanguage);
    }
    
    public static string PostEditXliffFileWithInstructions(string instructions, string sourceLanguage, string targetLanguage)
    {
        return PostEditXliffFile
            .Replace("{instructions}", instructions)
            .Replace("{source_language}", sourceLanguage)
            .Replace("{target_language}", targetLanguage);
    }
    
    public static string EvaluateTranslationQualityWithLanguages(string sourceLanguage, string targetLanguage)
    {
        return EvaluateTranslationQuality
            .Replace("{source_language}", sourceLanguage)
            .Replace("{target_language}", targetLanguage);
    }
}