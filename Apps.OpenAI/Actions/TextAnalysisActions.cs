using System.Linq;
using System.Threading.Tasks;
using Apps.OpenAI.Extensions;
using Apps.OpenAI.Invocables;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Analysis;
using Apps.OpenAI.Models.Responses.Analysis;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using OpenAI.Interfaces;
using OpenAI.ObjectModels.RequestModels;
using TiktokenSharp;

namespace Apps.OpenAI.Actions;

[ActionList]
public class TextAnalysisActions : OpenAiInvocable
{
    private IOpenAIService Client { get; }

    public TextAnalysisActions(InvocationContext invocationContext) : base(invocationContext)
    {
        Client = Creds.CreateOpenAiServiceSdk();
    }

    [Action("Create embedding", Description = "Generate an embedding for a text provided. An embedding is a list of " +
                                              "floating point numbers that captures semantic information about the " +
                                              "text that it represents.")]
    public async Task<CreateEmbeddingResponse> CreateEmbedding([ActionParameter] ModelIdentifier modelIdentifier, 
        [ActionParameter] EmbeddingRequest input)
    {
        var model = modelIdentifier.Model ?? "text-embedding-ada-002";
        var embedResult = await Client.Embeddings.CreateEmbedding(new EmbeddingCreateRequest
        {
            Input = input.Text,
            Model = model
        });
        embedResult.ThrowOnError();

        return new()
        {
            Embedding = embedResult.Data.First().Embedding
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