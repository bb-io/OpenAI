#nullable enable

using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Requests.Content;

public class ShortenContentRequest
{
    [Display("File")]
    public FileReference File { get; set; } = new();

    [Display("Maximum length", Description = "Maximum number of characters for units that have no size restriction in the file. Units with a size restriction always use their own limit.")]
    public int? MaximumLength { get; set; }

    [Display("Retries", Description = "Number of extra attempts for texts that are still too long. Defaults to 3.")]
    public int? Retries { get; set; }

    [Display("Additional instructions", Description = "Specify additional instructions to apply when shortening.")]
    public string? AdditionalInstructions { get; set; }

    [Display("Output file handling", Description = "Determine the format of the output file. The default Blackbird behavior is to convert to XLIFF for future steps.")]
    [StaticDataSource(typeof(OpenAiProcessFileFormatHandler))]
    public string? OutputFileHandling { get; set; }

    [Display("Bucket size", Description = "Maximum number of segments per model request. Defaults to 250, subject to the model's output limit.")]
    public int? BucketSize { get; set; }

    [Display("Glossary")]
    public FileReference? Glossary { get; set; }
}
