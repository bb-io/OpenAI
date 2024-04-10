using System.Linq;
using System.Threading.Tasks;
using Apps.OpenAI.Actions.Base;
using Apps.OpenAI.Api;
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

[ActionList]
public class TextAnalysisActions : BaseActions
{
    public TextAnalysisActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
        : base(invocationContext, fileManagementClient)
    {
    }

    [Action("Create embedding", Description = "Generate an embedding for a text provided. An embedding is a list of " +
                                              "floating point numbers that captures semantic information about the " +
                                              "text that it represents.")]
    public async Task<CreateEmbeddingResponse> CreateEmbedding(
        [ActionParameter] EmbeddingModelIdentifier modelIdentifier,
        [ActionParameter] EmbeddingRequest input)
    {
        var model = modelIdentifier.ModelId ?? "text-embedding-ada-002";

        var request = new OpenAIRequest("/embeddings", Method.Post, Creds);
        request.AddJsonBody(new
        {
            model,
            input = input.Text
        });

        var response = await Client.ExecuteWithErrorHandling<DataDto<EmbeddingDto>>(request);
        return new()
        {
            Embedding = response.Data.First().Embedding
        };
    }

    [Action("Tokenize text", Description = "Tokenize the text provided. Optionally specify encoding: cl100k_base " +
                                           "(used by gpt-4, gpt-3.5-turbo, text-embedding-ada-002) or p50k_base " +
                                           "(used by codex models, text-davinci-002, text-davinci-003).")]
    public async Task<TokenizeTextResponse> TokenizeText([ActionParameter] TokenizeTextRequest input)
    {
        var encoding = input.Encoding ?? "cl100k_base";
        var tikToken = await TikToken.GetEncodingAsync(encoding);

        var tokens = tikToken.Encode(input.Text);

        return new()
        {
            Tokens = tokens
        };
    }
}