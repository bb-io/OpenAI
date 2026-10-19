using Apps.OpenAI.Actions;
using Apps.OpenAI.Ai;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Requests.Content;
using Blackbird.Ai.Llm.Abstract;
using Blackbird.Ai.Llm.Models;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Transformations;
using Newtonsoft.Json.Linq;

namespace Tests.OpenAI.Ai;

// Offline: "Apply prompt to bilingual content" runs on the library's ApplyPrompt operation.
[TestClass]
public class ApplyPromptOfflineTests
{
    private readonly InMemoryFileManagementClient _files = new();

    // The user's prompt reaches the model, every Initial and Translated segment gets the answer, and
    // the action keeps marking processed segments Reviewed as it always did.
    [TestMethod]
    public async Task Prompt_AppliesTheInstructionAndMarksSegmentsReviewed()
    {
        var client = new UppercasingClient();
        var file = _files.Add("prompt.xlf", TestXliff.Translated("en", "de", ("Hello", "Hallo"), ("World", "Welt")));

        var result = await new ScriptedEditActions(Context(), _files, client).Prompt(
            new TextChatModelIdentifier { ModelId = "gpt-4o" },
            new EditContentRequest { File = file },
            "Write every target in capital letters.",
            new GlossaryRequest(),
            new ReasoningEffortRequest());

        Assert.AreEqual(2, result.TotalSegmentsUpdated);
        Assert.IsTrue(client.SystemPrompts.All(prompt => prompt.Contains("Write every target in capital letters.")));
        var segments = Segments(result.File);
        CollectionAssert.AreEqual(new[] { "HALLO", "WELT" }, segments.Select(segment => segment.GetTarget()).ToArray());
        Assert.IsTrue(segments.All(segment => segment.State == SegmentState.Reviewed));
    }

    private List<Segment> Segments(FileReference file)
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(_files.Read(file)));
        return Transformation.Load(stream, file.Name).Value.GetUnits().SelectMany(unit => unit.Segments).ToList();
    }

    private static InvocationContext Context() => new()
    {
        AuthenticationCredentialsProviders =
        [
            new AuthenticationCredentialsProvider(CredNames.ConnectionType, ConnectionTypes.OpenAi),
            new AuthenticationCredentialsProvider(CredNames.ApiKey, "unused")
        ]
    };

    private sealed class ScriptedEditActions(InvocationContext context, IFileManagementClient files, ILlmClient client)
        : EditActions(context, files)
    {
        protected override ILlmClient CreateLlmClient(string? model, OpenAiLlmSettings settings) => client;
    }

    // Answers with each segment's current target in capitals, in the library's response envelope.
    private sealed class UppercasingClient : ILlmClient
    {
        public List<string> SystemPrompts { get; } = [];

        public LlmCapabilities Capabilities { get; } = new(SupportsStructuredOutput: true, ContextTokens: 128_000, MaxOutputTokens: 16_384);

        public Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken cancellationToken)
        {
            SystemPrompts.Add(request.SystemPrompt);

            var start = request.UserPrompt.IndexOf('[');
            var end = request.UserPrompt.LastIndexOf(']');
            var translations = JArray.Parse(request.UserPrompt[start..(end + 1)])
                .OfType<JObject>()
                .Select(item => new JObject
                {
                    ["translation_id"] = item.Value<string>("id"),
                    ["translated_text"] = item.Value<string>("target")!.ToUpperInvariant()
                });

            return Task.FromResult(LlmResponse.Completed(
                new JObject { ["translations"] = new JArray(translations) }.ToString(),
                new LlmUsage(10, 5)));
        }
    }
}
