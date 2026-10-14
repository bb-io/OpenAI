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

// Offline: a client that never returns usable JSON drives the library into warnings, which must reach
// InvocationContext.Logger, because the actions no longer return warnings as output.
[TestClass]
public class WarningLoggingTests
{
    private readonly InMemoryFileManagementClient _files = new();
    private readonly List<string> _loggedWarnings = [];

    [TestMethod]
    public async Task TranslateContent_UnusableAnswers_LogsWarningsAndLeavesSegmentsUntouched()
    {
        var file = _files.Add("broken.xlf", TestXliff.Untranslated("en", "de", ["One", "Two"]));

        var result = await new UnusableAnswerTranslationActions(Context(), _files).TranslateContent(
            new TextChatModelIdentifier { ModelId = "gpt-4o" },
            new TranslateContentRequest { File = file, TargetLanguage = "de", DocumentSummary = "never" },
            null,
            new GlossaryRequest(),
            new ReasoningEffortRequest());

        Assert.AreEqual(0, result.TargetsUpdatedCount);
        Assert.IsNotEmpty(_loggedWarnings);
        Assert.IsTrue(_loggedWarnings.All(warning => warning.Contains("[OpenAI]")), string.Join(Environment.NewLine, _loggedWarnings));
    }

    [TestMethod]
    public async Task TranslateText_UnusableAnswers_LogsWarningsAndThrows()
    {
        var actions = new UnusableAnswerTranslationActions(Context(), _files);

        var exception = await Assert.ThrowsExactlyAsync<PluginApplicationException>(() => actions.LocalizeText(
            new TextChatModelIdentifier { ModelId = "gpt-4o" },
            new LocalizeTextRequest { Text = "Hello", SourceLanguage = "en", TargetLanguage = "de" },
            new GlossaryRequest()));

        Assert.IsNotEmpty(_loggedWarnings);
        Assert.IsTrue(_loggedWarnings.All(warning => exception.Message.Split(Environment.NewLine).Any(warning.Contains)),
            $"logged: {string.Join(" | ", _loggedWarnings)}; thrown: {exception.Message}");
    }

    private InvocationContext Context() => new()
    {
        AuthenticationCredentialsProviders =
        [
            new AuthenticationCredentialsProvider(CredNames.ConnectionType, ConnectionTypes.OpenAi),
            new AuthenticationCredentialsProvider(CredNames.ApiKey, "unused")
        ],
        Logger = new Logger("Debug") { LogWarning = (message, _) => _loggedWarnings.Add(message ?? string.Empty) }
    };

    private sealed class UnusableAnswerTranslationActions(InvocationContext context, IFileManagementClient files)
        : TranslationActions(context, files)
    {
        protected override ILlmClient CreateLlmClient(string? model, OpenAiLlmSettings settings) => new UnusableAnswerClient();
    }

    private sealed class UnusableAnswerClient : ILlmClient
    {
        public LlmCapabilities Capabilities { get; } = new(SupportsStructuredOutput: true, ContextTokens: 128_000, MaxOutputTokens: 16_384);

        public Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(LlmResponse.Completed("not json", new LlmUsage(10, 2)));
    }
}
