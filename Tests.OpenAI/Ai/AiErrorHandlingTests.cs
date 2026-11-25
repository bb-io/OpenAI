using Apps.OpenAI.Actions;
using Apps.OpenAI.Ai;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Requests.Content;
using Blackbird.Ai.Llm.Abstract;
using Blackbird.Ai.Llm.Models;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;

namespace Tests.OpenAI.Ai;

[TestClass]
public class AiErrorHandlingTests
{
    private readonly InMemoryFileManagementClient _files = new();

    [TestMethod]
    public async Task EditContent_UnsupportedFile_ThrowsMisconfigurationWithoutLibraryException()
    {
        var file = _files.Add("archive.zip", "PK not translatable content", "application/zip");
        var actions = new NeverCalledClientEditActions(Context(), _files);

        var exception = await Assert.ThrowsExactlyAsync<PluginMisconfigurationException>(() => actions.EditContent(
            new TextChatModelIdentifier { ModelId = "gpt-4o" },
            new EditContentRequest { File = file, DocumentSummary = "never" },
            null,
            new GlossaryRequest(),
            new ReasoningEffortRequest()));

        Assert.IsFalse(string.IsNullOrWhiteSpace(exception.Message));
        Assert.IsNull(exception.InnerException);
    }

    [TestMethod]
    public async Task Mapped_UnexpectedException_IsThrownAsApplicationExceptionWithSameMessage()
    {
        var exception = await Assert.ThrowsExactlyAsync<PluginApplicationException>(() =>
            AiAdapters.Mapped<int>(() => throw new InvalidOperationException("Unexpected provider answer")));

        Assert.AreEqual("Unexpected provider answer", exception.Message);
        Assert.IsNull(exception.InnerException);
    }

    [TestMethod]
    public async Task Mapped_PluginException_IsRethrownUnchanged()
    {
        var thrown = new PluginMisconfigurationException("Please specify the model in the input");

        var exception = await Assert.ThrowsExactlyAsync<PluginMisconfigurationException>(() =>
            AiAdapters.Mapped<int>(() => throw thrown));

        Assert.AreSame(thrown, exception);
    }

    private static InvocationContext Context() => new()
    {
        AuthenticationCredentialsProviders =
        [
            new AuthenticationCredentialsProvider(CredNames.ConnectionType, ConnectionTypes.OpenAi),
            new AuthenticationCredentialsProvider(CredNames.ApiKey, "unused")
        ]
    };

    private sealed class NeverCalledClientEditActions(InvocationContext context, IFileManagementClient files)
        : EditActions(context, files)
    {
        protected override ILlmClient CreateLlmClient(string? model, OpenAiLlmSettings settings) => new NeverCalledClient();
    }

    private sealed class NeverCalledClient : ILlmClient
    {
        public LlmCapabilities Capabilities { get; } = new(SupportsStructuredOutput: true, ContextTokens: 128_000, MaxOutputTokens: 16_384);

        public Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken cancellationToken) =>
            throw new AssertFailedException("The provider must not be called for an unreadable file.");
    }
}
