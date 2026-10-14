using Apps.OpenAI.Api.Requests;
using Apps.OpenAI.Constants;
using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Utils;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Apps.OpenAI.Api;

public class OpenAiUniversalClient(IEnumerable<AuthenticationCredentialsProvider> credentials)
    : BlackBirdRestClient(CreateOptions(credentials))
{
    private const int RetryCount = 8;
    private const int BaseBackoffSeconds = 1;
    private const int MaxBackoffSeconds = 16;
    private const int MaxRetryAfterSeconds = 60;
    private const string InsufficientQuotaCode = "insufficient_quota";

    private static readonly AsyncRetryPolicy<RestResponse> RateLimitRetryPolicy = Policy
        .HandleResult<RestResponse>(IsRetryableRateLimit)
        .WaitAndRetryAsync(RetryCount, (retryAttempt, result, _) => GetRetryDelay(retryAttempt, result.Result),
            (_, _, _, _) => Task.CompletedTask);

    private readonly AuthHeaderDto _authHeader = new(credentials);

    public string ConnectionType => credentials.First(x => x.KeyName == CredNames.ConnectionType).Value;

    public async ValueTask<ConnectionValidationResponse> ValidateConnection()
    {
        var request = new OpenAIRequest("/models", Method.Get);

        try
        {
            await ExecuteWithErrorHandling(request);
            return new() { IsValid = true };
        }
        catch (Exception ex)
        {
            return new()
            {
                IsValid = false,
                Message = ex.Message
            };
        }
    }

    public async Task<ChatCompletionDto> ExecuteApiRequestAsync(Dictionary<string, object> input)
    {
        var request = new OpenAIRequest("/responses", Method.Post, input);
        var response = await ExecuteWithErrorHandling<OpenAiResponseDto>(request);
        return MapToChatCompletionDto(response);
    }

    public override async Task<T> ExecuteWithErrorHandling<T>(RestRequest request)
    {
        string content = (await ExecuteWithErrorHandling(request)).Content;
        T val = JsonConvert.DeserializeObject<T>(content, JsonSettings);
        return val == null ? throw new Exception($"Could not parse {content} to {typeof(T)}") : val;
    }

    public override async Task<RestResponse> ExecuteWithErrorHandling(RestRequest request)
    {
        SetAuthHeader(request);

        RestResponse restResponse = await RateLimitRetryPolicy.ExecuteAsync(() => ExecuteAsync(request));
        if (!restResponse.IsSuccessStatusCode)
            throw ConfigureErrorException(restResponse);

        return restResponse;
    }

    // A 429 with "insufficient_quota" means the account is out of credit, so it is not retried.
    private static bool IsRetryableRateLimit(RestResponse response)
        => response.StatusCode == HttpStatusCode.TooManyRequests
           && response.Content?.Contains(InsufficientQuotaCode, StringComparison.OrdinalIgnoreCase) != true;

    private static TimeSpan GetRetryDelay(int retryAttempt, RestResponse response)
    {
        var retryAfterMs = HeaderValue(response, "retry-after-ms");
        if (double.TryParse(retryAfterMs, NumberStyles.Float, CultureInfo.InvariantCulture, out var milliseconds) && milliseconds > 0)
            return TimeSpan.FromMilliseconds(Math.Min(milliseconds, MaxRetryAfterSeconds * 1000));

        var retryAfter = HeaderValue(response, "Retry-After");
        if (double.TryParse(retryAfter, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) && seconds > 0)
            return TimeSpan.FromSeconds(Math.Min(seconds, MaxRetryAfterSeconds));

        var backoff = Math.Min(BaseBackoffSeconds * Math.Pow(2, retryAttempt - 1), MaxBackoffSeconds);
        return TimeSpan.FromSeconds(backoff) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 500));
    }

    private static string? HeaderValue(RestResponse response, string name)
        => response.Headers?
            .FirstOrDefault(header => string.Equals(header.Name, name, StringComparison.OrdinalIgnoreCase))?
            .Value?.ToString();

    public string GetModel(string defaultValue = null)
    {
        return ConnectionType switch
        {
            ConnectionTypes.OpenAiEmbedded =>
                defaultValue
                ?? credentials.FirstOrDefault(x => x.KeyName == CredNames.Model)?.Value
                ?? throw new PluginMisconfigurationException("Model must be provided in the input or connection"),

            ConnectionTypes.OpenAi =>
                defaultValue
                ?? throw new PluginMisconfigurationException("Model must be provided in the input"),

            ConnectionTypes.AzureOpenAi =>
                credentials.FirstOrDefault(x => x.KeyName == CredNames.Model)?.Value
                ?? throw new PluginMisconfigurationException("Model must be provided in the connection"),

            _ => throw new PluginApplicationException($"Unsupported connection type: {ConnectionType}")
        };
    }

    public async Task<string> ResolveTextChatModelAsync(string? inputModel)
    {
        if (!string.IsNullOrWhiteSpace(inputModel))
        {
            return ConnectionType == ConnectionTypes.AzureOpenAi
                ? GetModel()
                : inputModel;
        }

        return ConnectionType switch
        {
            ConnectionTypes.OpenAiEmbedded => GetModel(),
            ConnectionTypes.AzureOpenAi => GetModel(),
            ConnectionTypes.OpenAi => await ResolveLatestGeneralTextModelAsync(),
            _ => throw new PluginApplicationException($"Unsupported connection type: {ConnectionType}")
        };
    }

    private async Task<string> ResolveLatestGeneralTextModelAsync()
    {
        var request = new OpenAIRequest("/models", Method.Get);
        var models = await ExecuteWithErrorHandling<ModelsList>(request);

        var selectedModel = TextChatModelOrdering.SelectDefaultTextModel(models.Data);
        if (string.IsNullOrWhiteSpace(selectedModel))
        {
            throw new PluginMisconfigurationException("Couldn't find a compatible model for this action. Please specify a model in the 'Model' input.");
        }

        return selectedModel;
    }

    private static ChatCompletionDto MapToChatCompletionDto(OpenAiResponseDto response)
    {
        var outputTextItems = response.Output
            .Where(x => x.Type == "message")
            .SelectMany(x => x.Content ?? [])
            .Where(x => x.Type == "output_text")
            .ToList();

        var content = string.Join("", outputTextItems.Select(x => x.Text ?? string.Empty));

        var citations = outputTextItems
            .SelectMany(x => x.Annotations ?? [])
            .Where(x => x.Type == "url_citation" && !string.IsNullOrEmpty(x.Url))
            .Select(x => new UrlCitationDto
            {
                Title = x.Title ?? string.Empty,
                Url = x.Url ?? string.Empty
            })
            .ToList();

        var sources = response.Output
            .Where(x => x.Type == "web_search_call")
            .SelectMany(x => x.Action?.Sources ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x.Url))
            .Select(x => x.Url)
            .Distinct()
            .ToList();

        var usage = new UsageDto
        {
            ModelUsed = response.Model,
            PromptTokens = response.Usage?.InputTokens ?? 0,
            CompletionTokens = response.Usage?.OutputTokens ?? 0,
            TotalTokens = response.Usage?.TotalTokens ?? 0
        };

        var finishReason = response.IncompleteDetails?.Reason == "max_output_tokens"
            ? "length"
            : "stop";

        return new ChatCompletionDto
        {
            Choices = [
                new ChatCompletionChoiceDto
                {
                    Message = new ChatMessageDto("assistant", content),
                    FinishReason = finishReason
                }
            ],
            Usage = usage,
            Citations = citations,
            Sources = sources
        };
    }

    private void SetAuthHeader(RestRequest request) => request.AddHeader(_authHeader.HeaderKey, _authHeader.HeaderValue);

    private static RestClientOptions CreateOptions(IEnumerable<AuthenticationCredentialsProvider> credentials)
    {
        var connectionType = credentials.First(x => x.KeyName == CredNames.ConnectionType).Value;

        var baseUrl = connectionType switch
        {
            ConnectionTypes.AzureOpenAi => $"{credentials.Get(CredNames.Url).Value}/openai/v1",
            ConnectionTypes.OpenAi => "https://api.openai.com/v1",
            ConnectionTypes.OpenAiEmbedded => "https://api.openai.com/v1",
            _ => throw new Exception($"Connection type is not supported: {connectionType}")
        };

        return new RestClientOptions
        {
            ThrowOnAnyError = false,
            BaseUrl = new Uri(baseUrl),
            MaxTimeout = (int)TimeSpan.FromMinutes(15).TotalMilliseconds
        };
    }

    protected override Exception ConfigureErrorException(RestResponse response)
    {
        return ErrorHelper.ConfigureErrorException(response, JsonSettings);
    }
}
