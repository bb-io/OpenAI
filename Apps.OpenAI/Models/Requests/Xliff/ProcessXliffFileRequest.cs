using Apps.OpenAI.Models.Identifiers;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Requests.Xliff;

public class ProcessXliffFileRequest : TextChatModelIdentifier
{
    public FileReference File { get; set; }
    
    public FileReference? Glossary { get; set; }
}