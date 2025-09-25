using Apps.OpenAI.DataSourceHandlers;
using Apps.OpenAI.Models.Identifiers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Requests.Background;

public class StartBackgroundProcessRequest : TextChatModelIdentifier
{
    public FileReference File { get; set; }

    [Display("Source language"), StaticDataSource(typeof(LocaleDataSourceHandler))]
    public string? SourceLanguage { get; set; }

    [Display("Target language"), StaticDataSource(typeof(LocaleDataSourceHandler))]
    public string TargetLanguage { get; set; }
    
    [Display("Additional instructions", Description = "Additional instructions to guide the translation process.")]
    public string? AdditionalInstructions { get; set; }
    
    public FileReference? Glossary { get; set; }
}