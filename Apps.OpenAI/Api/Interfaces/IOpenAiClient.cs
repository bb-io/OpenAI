using System.Threading.Tasks;
using Blackbird.Applications.Sdk.Common.Connections;

namespace Apps.OpenAI.Api.Interfaces;

public interface IOpenAiClient
{
    ValueTask<ConnectionValidationResponse> ValidateConnection();
}
