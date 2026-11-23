using Apps.OpenAI.Actions;
using Apps.OpenAI.Ai;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Requests.Content;
using Apps.OpenAI.Models.Requests.Review;
using Blackbird.Ai.Llm.Abstract;
using Blackbird.Ai.Llm.Models;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Newtonsoft.Json.Linq;

namespace Tests.OpenAI.Ai;

[TestClass]
public class SegmentNotesOfflineTests
{
    private const string FileWithNotes = """
        <xliff version="2.0" srcLang="en" trgLang="de" xmlns="urn:oasis:names:tc:xliff:document:2.0">
          <file id="f1" original="labels.xlf">
            <unit id="u1">
              <notes><note>Label for missing job type</note></notes>
              <segment id="s1" state="translated"><source>Add job type</source><target>Jobart hinzufügen</target></segment>
            </unit>
            <unit id="u2">
              <notes><note>Add button</note></notes>
              <segment id="s2" state="translated"><source>Add job types</source><target>Jobarten hinzufügen</target></segment>
            </unit>
          </file>
        </xliff>
        """;

    private readonly InMemoryFileManagementClient _files = new();
    private readonly TextChatModelIdentifier _model = new() { ModelId = "gpt-4o" };

    [TestMethod]
    public async Task TranslateContent_IncludeSegmentNotes_SendsEachUnitsNotesToTheModel()
    {
        var client = new RecordingClient();
        var input = new TranslateContentRequest { File = UntranslatedFileWithNotes(), TargetLanguage = "de", IncludeSegmentNotes = true };

        await new RecordingTranslationActions(_files, client).TranslateContent(_model, input, null, new GlossaryRequest(), new ReasoningEffortRequest());

        CollectionAssert.AreEquivalent(
            new[] { "Label for missing job type", "Add button" },
            client.SentNotes.ToArray());
        StringAssert.Contains(client.Requests.Single().SystemPrompt, "### SEGMENT NOTES");
    }

    [TestMethod]
    public async Task TranslateContent_IncludeSegmentNotesNotSet_SendsNoNotes()
    {
        var client = new RecordingClient();
        var input = new TranslateContentRequest { File = UntranslatedFileWithNotes(), TargetLanguage = "de" };

        await new RecordingTranslationActions(_files, client).TranslateContent(_model, input, null, new GlossaryRequest(), new ReasoningEffortRequest());

        Assert.IsEmpty(client.SentNotes);
        Assert.DoesNotContain("### SEGMENT NOTES", client.Requests.Single().SystemPrompt);
    }

    [TestMethod]
    public async Task ReviewContent_IncludeSegmentNotes_SendsEachUnitsNotesToTheModel()
    {
        var client = new RecordingClient();
        var input = new ReviewContentRequest { File = _files.Add("labels.xlf", FileWithNotes), TargetLanguage = "de", IncludeSegmentNotes = true };

        await new RecordingReviewActions(_files, client).ReviewContent(_model, input);

        CollectionAssert.AreEquivalent(
            new[] { "Label for missing job type", "Add button" },
            client.SentNotes.ToArray());
    }

    private Blackbird.Applications.Sdk.Common.Files.FileReference UntranslatedFileWithNotes() =>
        _files.Add("labels.xlf", FileWithNotes
            .Replace(" state=\"translated\"", string.Empty)
            .Replace("<target>Jobart hinzufügen</target>", string.Empty)
            .Replace("<target>Jobarten hinzufügen</target>", string.Empty));

    private static InvocationContext Context() => new()
    {
        AuthenticationCredentialsProviders =
        [
            new AuthenticationCredentialsProvider(CredNames.ConnectionType, ConnectionTypes.OpenAi),
            new AuthenticationCredentialsProvider(CredNames.ApiKey, "unused")
        ]
    };

    private sealed class RecordingTranslationActions(IFileManagementClient files, ILlmClient client)
        : TranslationActions(Context(), files)
    {
        protected override ILlmClient CreateLlmClient(string? model, OpenAiLlmSettings settings) => client;
    }

    private sealed class RecordingReviewActions(IFileManagementClient files, ILlmClient client)
        : ReviewActions(Context(), files)
    {
        protected override ILlmClient CreateLlmClient(string? model, OpenAiLlmSettings settings) => client;
    }

    private sealed class RecordingClient : ILlmClient
    {
        public List<LlmRequest> Requests { get; } = [];

        public List<string> SentNotes { get; } = [];

        public LlmCapabilities Capabilities { get; } = new(
            SupportsStructuredOutput: true, ContextTokens: 128_000, MaxOutputTokens: 16_384);

        public Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            var start = request.UserPrompt.IndexOf('[');
            var end = request.UserPrompt.LastIndexOf(']');
            var items = JArray.Parse(request.UserPrompt[start..(end + 1)]).Cast<JObject>().ToList();

            SentNotes.AddRange(items.SelectMany(item => item["notes"]?.Values<string>() ?? []).OfType<string>());

            var answers = items.Select(item => new JObject
            {
                ["translation_id"] = item.Value<string>("id"),
                ["translated_text"] = "Übersetzt",
                ["quality_score"] = 0.9
            });

            var response = new JObject { ["translations"] = new JArray(answers) };
            return Task.FromResult(LlmResponse.Completed(response.ToString(), new LlmUsage(10, 5)));
        }
    }
}
