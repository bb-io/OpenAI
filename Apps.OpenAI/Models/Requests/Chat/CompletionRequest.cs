﻿using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.Models.Requests.Chat;

public class CompletionRequest
{
    public string Prompt { get; set; }

    [Display("Maximum tokens")]
    public int? MaximumTokens { get; set; }
        
    [DataSource(typeof(CompletionsModelDataSourceHandler))]
    public string? Model { get; set; }

    [Display("Temperature")]
    [DataSource(typeof(TemperatureDataSourceHandler))]
    public float? Temperature { get; set; }

    [Display("top_p")]
    [DataSource(typeof(TopPDataSourceHandler))]
    public float? TopP { get; set; }

    [Display("Presence penalty")]
    [DataSource(typeof(PenaltyDataSourceHandler))]
    public float? PresencePenalty { get; set; }

    [Display("Frequency penalty")]
    [DataSource(typeof(PenaltyDataSourceHandler))]
    public float? FrequencyPenalty { get; set; }
}