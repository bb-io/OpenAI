using System.Linq;
using System.Threading.Tasks;
using Apps.OpenAI.Actions.Base;
using Apps.OpenAI.Api.Requests;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Analysis;
using Apps.OpenAI.Models.Responses.Analysis;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using RestSharp;
using TiktokenSharp;

namespace Apps.OpenAI.Actions;

[ActionList("Text analysis")]
public class TextAnalysisActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient) 
    : BaseActions(invocationContext, fileManagementClient)
{
    [Action("Create embedding", Description = "Generates an embedding vector for input text.")]
    public async Task<CreateEmbeddingResponse> CreateEmbedding(
        [ActionParameter] EmbeddingModelIdentifier modelIdentifier,
        [ActionParameter] EmbeddingRequest input)
    {
        var model = UniversalClient.GetModel(modelIdentifier.ModelId ?? "text-embedding-ada-002");

        var request = new OpenAIRequest("/embeddings", Method.Post);
        request.AddJsonBody(new
        {
            model,
            input = input.Text
        });

        var response = await UniversalClient.ExecuteWithErrorHandling<DataDto<EmbeddingDto>>(request);
        return new() { Embedding = response.Data.First().Embedding };
    }

    [Action("Tokenize text", Description = "Tokenizes input text and outputs token IDs.")]
    public async Task<TokenizeTextResponse> TokenizeText([ActionParameter] TokenizeTextRequest input)
    {
        var encoding = input.Encoding ?? "cl100k_base";
        var tikToken = await TikToken.GetEncodingAsync(encoding);

        var tokens = tikToken.Encode(input.Text);

        return new() { Tokens = tokens };
    }
}
