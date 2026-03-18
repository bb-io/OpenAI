using System.Collections.Generic;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Apps.OpenAI.DataSourceHandlers;

namespace Apps.OpenAI.Models.Requests.Chat;

public class ChatRequest : BaseChatRequest, IWebSearchRequest
{
    [Display("System prompt")] 
    public string? SystemPrompt { get; set; }
    
    public string Message { get; set; }

    [Display("Texts",
        Description =
            "Texts that will be added to the user prompt along with the message. Useful if you want to add collection of messages to the prompt.")]
    public IEnumerable<string>? Parameters { get; set; }

    public FileReference? File { get; set; }

    [Display("Enable web search")]
    public bool? EnableWebSearch { get; set; }

    [Display("Web search context size")]
    [StaticDataSource(typeof(WebSearchContextSizeDataSourceHandler))]
    public string? WebSearchContextSize { get; set; }

    [Display("Allow live web access", Description = "If false, uses cache/indexed web content only")]
    public bool? ExternalWebAccess { get; set; }

    [Display("Allowed domains", Description = "Optional allow-list of domains for web search")]
    public IEnumerable<string>? AllowedDomains { get; set; }

    [Display("User location city")]
    public string? UserLocationCity { get; set; }

    [Display("User location country", Description = "Two-letter ISO country code, e.g. US")]
    public string? UserLocationCountry { get; set; }

    [Display("User location region")]
    public string? UserLocationRegion { get; set; }

    [Display("User location timezone", Description = "IANA timezone, e.g. Europe/Warsaw")]
    public string? UserLocationTimezone { get; set; }
}