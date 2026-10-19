using Apps.OpenAI.Actions;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Background;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Requests.Content;
using Apps.OpenAI.Polling;
using Apps.OpenAI.Polling.Models;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Polling;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Transformations;
using Newtonsoft.Json;
using Tests.OpenAI.Base;

namespace Tests.OpenAI.Ai;

// Live: submits a real OpenAI batch and waits for it, which can take from minutes up to the 24-hour
// completion window. Skipped unless RUN_LIVE_BATCH=1, so a normal test run never waits on a batch.
[TestClass]
public class LiveBackgroundActionsTests : TestBaseWithContext
{
    private const string RunVariable = "RUN_LIVE_BATCH";

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MaxWait = TimeSpan.FromMinutes(90);

    private readonly InMemoryFileManagementClient _files = new();

    // Start → the job-finished event → download, the way a Bird runs it: every segment comes back
    // translated, the event fires exactly once, and usage comes from the batch.
    [TestMethod, TestCategory("LiveBatch"), ContextDataSource(ConnectionTypes.OpenAi)]
    public async Task TranslateInBackground_RealBatch_EventFiresOnceAndDownloadTranslatesEverySegment(InvocationContext context)
    {
        if (Environment.GetEnvironmentVariable(RunVariable) != "1")
        {
            Assert.Inconclusive($"Live batch test skipped. Set {RunVariable}=1 to run it.");
        }

        var file = _files.Add("live-batch.xlf", TestXliff.Untranslated(
            "en",
            "de",
            ["The weather is lovely today.", "Please restart the application.", "Our office opens at nine."]));

        var started = await new TranslationActions(context, _files).TranslateInBackground(
            new TextChatModelIdentifier { ModelId = "gpt-4.1-mini" },
            new TranslateContentRequest { File = file, TargetLanguage = "de", DocumentSummary = "never" },
            null,
            new GlossaryRequest(),
            new ReasoningEffortRequest());

        Print("Started", started);
        Assert.IsFalse(string.IsNullOrWhiteSpace(started.BatchId));
        Assert.AreEqual(1, started.RequestsCount);

        var events = new BatchPollingList(context);
        var memory = new BatchMemory { LastPollingTime = DateTime.UtcNow };
        var waited = TimeSpan.Zero;
        PollingEventResponse<BatchMemory, Apps.OpenAI.Models.Responses.Batch.BatchResponse> poll;

        do
        {
            await Task.Delay(PollInterval);
            waited += PollInterval;

            poll = await events.OnBatchFinished(
                new PollingEventRequest<BatchMemory> { Memory = memory },
                new BatchIdentifier { BatchId = started.BatchId });
            memory = poll.Memory!;

            TestContext.WriteLine($"{DateTime.UtcNow:HH:mm:ss} status={poll.Result?.Status} fired={poll.FlyBird}");
        }
        while (!poll.FlyBird && waited < MaxWait);

        Assert.IsTrue(poll.FlyBird, $"The batch did not finish within {MaxWait}.");
        Assert.AreEqual("completed", poll.Result!.Status);

        var again = await events.OnBatchFinished(
            new PollingEventRequest<BatchMemory> { Memory = memory },
            new BatchIdentifier { BatchId = started.BatchId });
        Assert.IsFalse(again.FlyBird, "The event fired a second time for the same finished batch.");

        var downloaded = await new BackgroundActions(context, _files).DownloadContentFromBackground(
            new BackgroundDownloadRequest { TransformationFile = started.TransformationFile });

        Print("Downloaded", downloaded);
        var segments = Segments(downloaded.File);
        foreach (var segment in segments)
        {
            TestContext.WriteLine($"{segment.GetSource()} => {segment.GetTarget()}");
        }

        Assert.AreEqual(3, downloaded.UpdatedSegmentsCount);
        Assert.AreEqual(0, downloaded.UnresolvedSegmentsCount);
        Assert.IsGreaterThan(0, downloaded.Usage.TotalTokens);
        Assert.IsFalse(string.IsNullOrWhiteSpace(downloaded.Usage.ModelUsed));
        Assert.IsTrue(segments.All(segment => segment.State == SegmentState.Translated));
        Assert.IsTrue(segments.All(segment => !string.IsNullOrWhiteSpace(segment.GetTarget()) && segment.GetTarget() != segment.GetSource()));
    }

    private List<Segment> Segments(FileReference file)
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(_files.Read(file)));
        return Transformation.Load(stream, file.Name).Value.GetUnits().SelectMany(unit => unit.Segments).ToList();
    }

    private void Print(string label, object value) =>
        TestContext.WriteLine($"{label}: {JsonConvert.SerializeObject(value, Formatting.Indented)}");
}
