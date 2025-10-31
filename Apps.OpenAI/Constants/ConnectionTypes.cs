using System.Collections.Generic;

namespace Apps.OpenAI.Constants;

public static class ConnectionTypes
{
    public const string OpenAi = "Developer API token";
    public const string AzureOpenAi = "AzureConnection";
    public const string OpenAiEmbedded = "OpenAiConnectionEmbedded";

    public static readonly IEnumerable<string> SupportedConnectionTypes = [OpenAi, AzureOpenAi, OpenAiEmbedded];
}
