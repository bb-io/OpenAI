using Apps.OpenAI.Dtos;
using System.Threading.Tasks;
using System.Collections.Generic;
using Blackbird.Applications.Sdk.Common.Connections;

namespace Apps.OpenAI.Api.Interfaces;

public interface IOpenAiClient
{
    ValueTask<ConnectionValidationResponse> ValidateConnection();
    Task<ChatCompletionDto> ExecuteChatCompletion(Dictionary<string, object> input, string model);
}
