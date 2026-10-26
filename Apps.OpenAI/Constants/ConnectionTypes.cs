using System.Collections.Generic;

namespace Apps.OpenAI.Constants;

public static class ConnectionTypes
{
    public const string OpenAi = "Developer API token";     // Legacy OpenAI connection identifier. Do not change!
    public const string AzureOpenAi = "AzureConnection";
    public const string OpenAiEmbedded = "OpenAiConnectionEmbedded";
    public const string OpenAiUs = "OpenAiUs";

    public static readonly IEnumerable<string> SupportedConnectionTypes = [OpenAi, AzureOpenAi, OpenAiEmbedded, OpenAiUs];
}
