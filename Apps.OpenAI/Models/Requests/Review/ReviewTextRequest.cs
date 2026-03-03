using Apps.OpenAI.DataSourceHandlers;
using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Interfaces.Review;
using Newtonsoft.Json;

namespace Apps.OpenAI.Models.Requests.Review;

public class ReviewTextRequest : IReviewTextInput
{
    [Display("Source text")]
    public string SourceText { get; set; } = string.Empty;

    [Display("Target text")]
    public string TargetText { get; set; } = string.Empty;

    [Display("Source language")]
    [StaticDataSource(typeof(LocaleDataSourceHandler))]
    public string? SourceLanguage { get; set; }

    [Display("Target language")]
    [StaticDataSource(typeof(LocaleDataSourceHandler))]
    public string TargetLanguage { get; set; } = string.Empty;

    [Display("Model", Description = "This parameter controls which version of OpenAI answers your request")]
    [DataSource(typeof(TextChatModelDataSourceHandler))]
    [JsonProperty("model")]
    public string Model { get; set; } = string.Empty;

    [Display("Additional instructions", Description = "The additional instructions that you want to apply to the review.\nFor example, 'Focus on technical terminology accuracy.'")]
    public string? AdditionalInstructions { get; set; }

    [Display("Max tokens", Description = "The maximum number of tokens to generate before stopping.")]
    [JsonProperty("max_tokens_to_sample")]
    public int? MaxTokensToSample { get; set; }

    [Display("Temperature", Description = "Amount of randomness injected into the response.")]
    [StaticDataSource(typeof(TemperatureDataSourceHandler))]
    [JsonProperty("temperature")]
    public string? Temperature { get; set; }

    [Display("top_p", Description = "Use nucleus sampling.")]
    [StaticDataSource(typeof(TopPDataSourceHandler))]
    [JsonProperty("top_p")]
    public string? TopP { get; set; }

    [Display("top_k", Description = "Only sample from the top K options for each subsequent token.")]
    [JsonProperty("top_k")]
    public int? TopK { get; set; }

    public FileReference? Glossary { get; set; }

}