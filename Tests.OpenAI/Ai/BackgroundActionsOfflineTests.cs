using System.Runtime.CompilerServices;
using Apps.OpenAI.Actions;
using Apps.OpenAI.Ai;
using Apps.OpenAI.Api;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Background;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Requests.Content;
using Blackbird.Ai.Llm.Abstract;
using Blackbird.Ai.Llm.Models;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Transformations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tests.OpenAI.Ai;

// Offline: the background actions run against a scripted batch provider, so the start → download
// round trip through Blackbird.Ai is checked without waiting for a real batch.
[TestClass]
public class BackgroundActionsOfflineTests
{
    private readonly InMemoryFileManagementClient _files = new();

    // Start submits one request per bucket and returns the batch ID with a transformation file; the
    // download applies the answers and reports what changed.
    [TestMethod]
    public async Task TranslateInBackground_ThenDownload_TranslatesEverySegment()
    {
        var batch = ScriptedBatchClient.Answering(item => $"DE:{item.Source}");
        var file = _files.Add("doc.xlf", TestXliff.Untranslated("en", "de", ["Hello", "World"]));

        var started = await new ScriptedTranslationActions(Context(), _files, batch).TranslateInBackground(
            new TextChatModelIdentifier { ModelId = "gpt-4o" },
            new TranslateContentRequest { File = file, TargetLanguage = "de", DocumentSummary = "never" },
            null,
            new GlossaryRequest(),
            new ReasoningEffortRequest());

        Assert.AreEqual(ScriptedBatchClient.JobId, started.BatchId);
        Assert.AreEqual("running", started.Status);
        Assert.AreEqual(1, started.RequestsCount);
        Assert.AreEqual(2, started.SegmentsToProcessCount);
        Assert.IsFalse(string.IsNullOrWhiteSpace(started.SystemPrompt));

        var downloaded = await new ScriptedBackgroundActions(Context(), _files, batch).DownloadContentFromBackground(
            new BackgroundDownloadRequest { TransformationFile = started.TransformationFile });

        Assert.AreEqual(2, downloaded.UpdatedSegmentsCount);
        Assert.AreEqual(0, downloaded.UnresolvedSegmentsCount);
        Assert.AreEqual("completed", downloaded.BatchStatus);
        Assert.AreEqual(30, downloaded.Usage.TotalTokens);
        CollectionAssert.AreEqual(new[] { "DE:Hello", "DE:World" }, Targets(downloaded.File));
    }

    // Edit in background keeps the edit-specific behaviour: segments end up Reviewed.
    [TestMethod]
    public async Task EditInBackground_ThenDownload_MarksSegmentsReviewed()
    {
        var batch = ScriptedBatchClient.Answering(item => $"{item.Target}!");
        var file = _files.Add("edit.xlf", TestXliff.Translated("en", "de", ("Hello", "Hallo"), ("World", "Welt")));

        var started = await new ScriptedEditActions(Context(), _files, batch).EditInBackground(
            new TextChatModelIdentifier { ModelId = "gpt-4o" },
            new EditContentRequest { File = file, DocumentSummary = "never" },
            null,
            new GlossaryRequest(),
            new ReasoningEffortRequest());

        var downloaded = await new ScriptedBackgroundActions(Context(), _files, batch).DownloadContentFromBackground(
            new BackgroundDownloadRequest { TransformationFile = started.TransformationFile, BatchId = started.BatchId });

        CollectionAssert.AreEqual(new[] { "Hallo!", "Welt!" }, Targets(downloaded.File));
        Assert.IsTrue(Segments(downloaded.File).All(segment => segment.State == SegmentState.Reviewed));
    }

    // Downloading before the batch has finished is something the user fixes by waiting.
    [TestMethod]
    public async Task DownloadContentFromBackground_JobStillRunning_ThrowsMisconfiguration()
    {
        var batch = ScriptedBatchClient.Answering(item => item.Source);
        var file = _files.Add("doc.xlf", TestXliff.Untranslated("en", "de", ["Hello"]));
        var started = await new ScriptedTranslationActions(Context(), _files, batch).TranslateInBackground(
            new TextChatModelIdentifier { ModelId = "gpt-4o" },
            new TranslateContentRequest { File = file, TargetLanguage = "de", DocumentSummary = "never" },
            null,
            new GlossaryRequest(),
            new ReasoningEffortRequest());
        batch.State = BatchJobState.Running;

        var exception = await Assert.ThrowsExactlyAsync<PluginMisconfigurationException>(() =>
            new ScriptedBackgroundActions(Context(), _files, batch).DownloadContentFromBackground(
                new BackgroundDownloadRequest { TransformationFile = started.TransformationFile }));

        StringAssert.Contains(exception.Message, "has not finished");
    }

    // Transformation files from before the move to Blackbird.Ai have no job information and still
    // need the batch ID to find their results.
    [TestMethod]
    public async Task DownloadContentFromBackground_LegacyFileWithoutBatchId_ThrowsMisconfiguration()
    {
        var file = _files.Add("legacy.xlf", TestXliff.Untranslated("en", "de", ["Hello"]));

        var exception = await Assert.ThrowsExactlyAsync<PluginMisconfigurationException>(() =>
            new ScriptedBackgroundActions(Context(), _files, ScriptedBatchClient.Answering(item => item.Source))
                .DownloadContentFromBackground(new BackgroundDownloadRequest { TransformationFile = file }));

        StringAssert.Contains(exception.Message, "Batch ID is required");
    }

    // A successful Responses API line maps exactly like a direct call, including truncation.
    [TestMethod]
    public void ToResult_IncompleteResponse_IsTruncatedWithUsage()
    {
        var line = Line("""
            {"custom_id":"b0","response":{"status_code":200,"body":{"model":"gpt-4o-2024-08-06","status":"incomplete",
             "incomplete_details":{"reason":"max_output_tokens"},
             "output":[{"type":"message","content":[{"type":"output_text","text":"{\"translations\":["}]}],
             "usage":{"input_tokens":12,"output_tokens":34,"total_tokens":46}}},"error":null}
            """);

        var client = BatchClient();
        var result = client.ToResult(line);

        Assert.AreEqual("b0", result.CustomId);
        Assert.AreEqual(FinishReason.Truncated, result.Response!.Finish);
        Assert.AreEqual(new LlmUsage(12, 34), result.Response.Usage);
        Assert.AreEqual("{\"translations\":[", result.Response.Text);
        Assert.AreEqual("gpt-4o-2024-08-06", client.ModelUsed);
    }

    // Rejected requests carry the provider's reason, whether it arrives as a line error or as an
    // error body with a non-200 status.
    [TestMethod]
    public void ToResult_RejectedRequests_CarryTheProviderMessage()
    {
        var lineError = BatchClient().ToResult(Line("""{"custom_id":"b1","response":null,"error":{"code":"batch_expired","message":"This request could not be executed before the completion window expired."}}"""));
        var httpError = BatchClient().ToResult(Line("""{"custom_id":"b2","response":{"status_code":400,"body":{"error":{"message":"Invalid schema."}}},"error":null}"""));

        Assert.IsNull(lineError.Response);
        StringAssert.Contains(lineError.Error, "completion window expired");
        Assert.AreEqual("Invalid schema.", httpError.Error);
    }

    private static BatchOutputLineDto Line(string json) => JsonConvert.DeserializeObject<BatchOutputLineDto>(json)!;

    private static OpenAiBatchLlmClient BatchClient() =>
        new(new OpenAiUniversalClient(Context().AuthenticationCredentialsProviders), "gpt-4o", new OpenAiLlmSettings());

    private string[] Targets(FileReference file) => Segments(file).Select(segment => segment.GetTarget()).ToArray();

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

    private sealed class ScriptedTranslationActions(InvocationContext context, IFileManagementClient files, IBatchLlmClient batch)
        : TranslationActions(context, files)
    {
        protected override ILlmClient CreateLlmClient(string? model, OpenAiLlmSettings settings) => new SilentClient();

        protected override IBatchLlmClient CreateBatchClient(string? model, OpenAiLlmSettings settings) => batch;
    }

    private sealed class ScriptedEditActions(InvocationContext context, IFileManagementClient files, IBatchLlmClient batch)
        : EditActions(context, files)
    {
        protected override ILlmClient CreateLlmClient(string? model, OpenAiLlmSettings settings) => new SilentClient();

        protected override IBatchLlmClient CreateBatchClient(string? model, OpenAiLlmSettings settings) => batch;
    }

    private sealed class ScriptedBackgroundActions(InvocationContext context, IFileManagementClient files, IBatchLlmClient batch)
        : BackgroundActions(context, files)
    {
        protected override IBatchLlmClient CreateBatchClient(string? model, OpenAiLlmSettings settings) => batch;
    }

    // Starting a job may only call the synchronous client for language detection or a summary; these
    // tests avoid both, so any call is a bug.
    private sealed class SilentClient : ILlmClient
    {
        public LlmCapabilities Capabilities { get; } = new(SupportsStructuredOutput: true, ContextTokens: 128_000, MaxOutputTokens: 16_384);

        public Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken cancellationToken) =>
            throw new AssertFailedException("The synchronous client should not be called.");
    }

    private sealed record PromptItem(string Id, string Source, string? Target);

    private sealed class ScriptedBatchClient(Func<PromptItem, string> answer) : IBatchLlmClient
    {
        public const string JobId = "batch_scripted";

        private readonly List<BatchLlmRequest> _submitted = [];

        public BatchJobState State { get; set; } = BatchJobState.Completed;

        public LlmCapabilities Capabilities { get; } = new(SupportsStructuredOutput: true, ContextTokens: 128_000, MaxOutputTokens: 16_384);

        public static ScriptedBatchClient Answering(Func<PromptItem, string> answer) => new(answer);

        public Task<BatchJob> SubmitAsync(IReadOnlyList<BatchLlmRequest> requests, CancellationToken cancellationToken)
        {
            _submitted.AddRange(requests);
            return Task.FromResult(new BatchJob(JobId, BatchJobState.Running, requests.Count));
        }

        public Task<BatchJob> GetJobAsync(string jobId, CancellationToken cancellationToken) =>
            Task.FromResult(new BatchJob(jobId, State, _submitted.Count));

        public async IAsyncEnumerable<BatchLlmResult> GetResultsAsync(
            BatchJob job,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var request in _submitted)
            {
                await Task.Yield();

                var translations = ItemsOf(request.Request.UserPrompt).Select(item => new JObject
                {
                    ["translation_id"] = item.Id,
                    ["translated_text"] = answer(item)
                });

                yield return BatchLlmResult.Succeeded(
                    request.CustomId,
                    LlmResponse.Completed(new JObject { ["translations"] = new JArray(translations) }.ToString(), new LlmUsage(20, 10)));
            }
        }

        private static IEnumerable<PromptItem> ItemsOf(string userPrompt)
        {
            var start = userPrompt.IndexOf('[');
            var end = userPrompt.LastIndexOf(']');

            return JArray.Parse(userPrompt[start..(end + 1)])
                .OfType<JObject>()
                .Select(item => new PromptItem(
                    item.Value<string>("id")!,
                    item.Value<string>("source")!,
                    item.Value<string>("target")));
        }
    }
}
