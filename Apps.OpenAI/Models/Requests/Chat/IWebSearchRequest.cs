#nullable enable

using System.Collections.Generic;

namespace Apps.OpenAI.Models.Requests.Chat;

public interface IWebSearchRequest
{
    bool? EnableWebSearch { get; set; }

    string? WebSearchContextSize { get; set; }

    bool? ExternalWebAccess { get; set; }

    IEnumerable<string>? AllowedDomains { get; set; }

    string? UserLocationCity { get; set; }

    string? UserLocationCountry { get; set; }

    string? UserLocationRegion { get; set; }

    string? UserLocationTimezone { get; set; }
}

#nullable disable
