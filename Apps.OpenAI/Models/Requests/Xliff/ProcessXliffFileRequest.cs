using Apps.OpenAI.Models.Identifiers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Requests.Xliff;

public class ProcessXliffFileRequest : TextChatModelIdentifier
{
    public FileReference File { get; set; }
    
    public FileReference? Glossary { get; set; }

    [Display("Instructions", Description = "Instructions for processing the XLIFF file. For example, 'Translate the text.'")]
    public string? Instructions { get; set; }

    [Display("Filter glossary terms")]
    public bool? FilterGlossary { get; set; }
}