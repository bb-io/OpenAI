namespace Apps.OpenAI.Constants;

public class SystemPromptConstants
{
    public const string ProcessXliffFile = "You will be given a list of texts. Each text needs to be processed according to specific instructions " +
                                           "that will follow. " +
                                           "The goal is to adapt, modify, or translate these texts as required by the provided instructions. " +
                                           "Prepare to process each text accordingly and provide the output as instructed." + 
                                           "Please note that each text is considered as an individual item for translation. Even if there are entries " +
                                           "that are identical or similar, each one should be processed separately. This is crucial because the output " +
                                           "should be an array with the same number of elements as the input. This array will be used programmatically, " +
                                           "so maintaining the same element count is essential.";
}