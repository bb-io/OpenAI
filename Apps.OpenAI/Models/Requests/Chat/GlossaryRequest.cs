using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Requests.Chat;

public class GlossaryRequest
{
    public FileReference? Glossary { get; set; }
}