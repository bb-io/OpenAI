namespace Apps.OpenAI.Utils;

public static class PromptBuilder
{
    private const string TranslationPrompt = "Translate the following texts from {source_language} to {target_language}";
    
    private const string CustomInstructionPrompt = "Process the following texts as per the custom instructions: {prompt}. The source language is {source_language} and the target language is {target_language}. This information might be useful for the custom instructions";
    
    private const string ProcessXliffSummary = "Please provide a translation for each individual text, even if similar texts have been provided more than once. " +
                                               "{custom_instruction_prompt}. Original texts (in serialized array format): {json}";

    private const string ReviewJsonOutputExample = @"{ ""translations"": [ { ""translation_id"": ""123"", ""quality_score"": 0.85 } ] }";


    public static string BuildUserPrompt(string prompt, string sourceLanguage, string targetLanguage, string json)
    {
        var instruction = string.IsNullOrEmpty(prompt)
            ? TranslationPrompt.Replace("{source_language}", sourceLanguage).Replace("{target_language}", targetLanguage)
            : CustomInstructionPrompt.Replace("{prompt}", prompt).Replace("{source_language}", sourceLanguage).Replace("{target_language}", targetLanguage);

        return ProcessXliffSummary.Replace("{custom_instruction_prompt}", instruction).Replace("{json}", json);
    }
    
    private const string TranslatorSystemPrompt = "You are tasked with localizing the provided text. Consider cultural nuances, idiomatic expressions, and locale-specific references to make the text feel natural in the target language. Ensure the structure of the original text is preserved. Respond with the localized text.";
    
    private const string CustomInstructionSystemPrompt = "You are tasked with processing the provided text according to the custom instructions. Consider the specific requirements outlined in the instructions and adapt the text accordingly. Respond with the processed text.";
    
    private const string SystemPromptSummary = "Please note that each text is considered as an individual item for translation. Even if there are entries that are identical or similar, each one should be processed separately. This is crucial because the output should be an array with the same number of elements as the input. This array will be used programmatically, so maintaining the same element count is essential.";
    
    public static string BuildSystemPrompt(bool translator)
    {
        return (translator ? TranslatorSystemPrompt : CustomInstructionSystemPrompt) + SystemPromptSummary;
    }

    public const string DefaultSystemPrompt =
        "You are a linguistic expert that should process the following texts according to the given instructions";
    
    private const string QualityScorePrompt = "Your input is going to be a group of sentences in {source_language} and their translation into {target_language}. " +
                                              "Only provide as output the ID of the sentence and the score number as a comma separated array of tuples. " +
                                              "The score number is a score from 1 to 10 assessing the quality of the translation, considering the following criteria: {criteria}. Sentences: {json}";

    public static string BuildQualityScorePrompt(string sourceLanguage, string targetLanguage, string criteria, string json)
    {
        return QualityScorePrompt.Replace("{source_language}", sourceLanguage).Replace("{target_language}", targetLanguage).Replace("{criteria}", criteria).Replace("{json}", json);
    }

    public static string BuildReviewSystemPrompt()
    {
        return @"You are a professional translation quality assessor with expertise in linguistic analysis and cultural adaptation across multiple languages.

Your core responsibilities:
- Evaluate translation quality objectively and consistently
- Consider accuracy, fluency, cultural appropriateness, and technical correctness
- Provide precise decimal scores between 0.0 and 1.0
- Maintain consistency across similar translation patterns
- Focus on practical usability of translations

Scoring guidelines:
- 0.9-1.0: Excellent - Natural, accurate, culturally appropriate
- 0.7-0.8: Good - Minor issues that don't affect meaning
- 0.5-0.6: Acceptable - Some issues but generally understandable
- 0.3-0.4: Poor - Significant issues affecting meaning or readability
- 0.0-0.2: Unacceptable - Major errors or incomprehensible

Always respond with valid JSON format only, without any additional commentary or explanation.";
    }

    public static string BuildReviewUserPrompt(
        string? additionalInstructions,
        string? sourceLanguage,
        string targetLanguage,
        string json)
    {
        string criteria = "fluency, grammar, terminology, style, and punctuation";

        if (!string.IsNullOrWhiteSpace(additionalInstructions))
            criteria = additionalInstructions;

        var srcLangText = string.IsNullOrWhiteSpace(sourceLanguage) ? "the source language" : sourceLanguage;

        return $@"You are a professional translation quality assessor specializing in XLIFF review.

# **Instructions:**
Review the translation quality from {srcLangText} to {targetLanguage}.

# **Quality Assessment Criteria:**
Evaluate each translation based on: {criteria}

# **Critical Requirements:**
- Assess each translation unit individually
- Provide quality scores as decimal values between 0.0 and 1.0 (where 1.0 is perfect)
- Consider accuracy, fluency, cultural appropriateness, and technical correctness
- Be consistent in your scoring methodology

**Output Format:**
Return a valid JSON object containing:
- ""translations"": an array of review objects, each with:
- ""translation_id"": the original ID (unchanged)
- ""quality_score"": decimal score from 0.0 to 1.0

**Example Output:**
{ReviewJsonOutputExample}

**Input Data (ID, Source Text, Target Text):**
{json}

Respond only with the JSON object, no additional text or formatting.";
    }
}