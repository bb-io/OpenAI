using System.Text;
using Apps.OpenAI.Actions;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Requests.Content;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Transformations;
using Newtonsoft.Json;
using Tests.OpenAI.Base;

namespace Tests.OpenAI.Ai;

// Live: "Apply prompt to bilingual content" against the real API, through the library's ApplyPrompt.
[TestClass]
public class LiveApplyPromptTests : TestBaseWithContext
{
    private const string Model = "gpt-4.1-mini";

    private readonly InMemoryFileManagementClient _files = new();
    private readonly List<string> _loggedWarnings = [];

    // A clear stylistic instruction is applied to every target, sources stay as they are, and the
    // processed segments are marked Reviewed.
    [TestMethod, ContextDataSource(ConnectionTypes.OpenAi)]
    public async Task Prompt_RealModelInformalRewrite_ChangesEveryTarget(InvocationContext context)
    {
        var file = _files.Add("live-prompt.xlf", TestXliff.Translated(
            "en",
            "de",
            ("Please restart the application.", "Bitte starten Sie die Anwendung neu."),
            ("Can you send us your invoice?", "Können Sie uns Ihre Rechnung schicken?"),
            ("Thank you for your order.", "Vielen Dank für Ihre Bestellung.")));

        var result = await new EditActions(WithWarningLog(context), _files).Prompt(
            new TextChatModelIdentifier { ModelId = Model },
            new EditContentRequest { File = file },
            "Rewrite every target in the informal 'du' form instead of the formal 'Sie' form. Keep the meaning.",
            new GlossaryRequest(),
            new ReasoningEffortRequest());

        Print(result);
        var segments = Segments(result.File);
        foreach (var segment in segments)
        {
            TestContext.WriteLine($"{segment.GetSource()} => {segment.GetTarget()}");
        }

        Assert.AreEqual(3, result.TotalSegmentsUpdated);
        AssertNoWarningsLogged();
        Assert.IsGreaterThan(0, result.Usage.TotalTokens);
        Assert.IsTrue(segments.All(segment => segment.State == SegmentState.Reviewed));
        Assert.IsFalse(segments.Any(segment => segment.GetTarget().Contains(" Sie ") || segment.GetTarget().Contains("Ihre")),
            "A formal form survived the rewrite.");
        CollectionAssert.AreEqual(
            new[] { "Please restart the application.", "Can you send us your invoice?", "Thank you for your order." },
            segments.Select(segment => segment.GetSource()).ToArray());
    }

    // A real interoperable XLIFF with inline tags: a narrow instruction changes only the segments it
    // names, and every inline tag survives.
    [TestMethod, ContextDataSource(ConnectionTypes.OpenAi)]
    public async Task Prompt_RealModelTaggedFile_ChangesOnlyWhatWasAskedAndKeepsTags(InvocationContext context)
    {
        var original = File.ReadAllText(InputPath("mqm-min.xlf"));
        var file = _files.Add("mqm-min.xlf", original, "application/xliff+xml");
        var before = Load(original, "mqm-min.xlf");

        var result = await new EditActions(WithWarningLog(context), _files).Prompt(
            new TextChatModelIdentifier { ModelId = Model },
            new EditContentRequest { File = file },
            "In the target text, write the name Loire in capital letters as LOIRE. Change nothing else.",
            new GlossaryRequest(),
            new ReasoningEffortRequest());

        Print(result);
        var segments = Segments(result.File);
        var expectedChanges = before.Count(segment => segment.GetTarget().Contains("Loire"));
        TestContext.WriteLine($"Segments with Loire in the target before: {expectedChanges}");

        AssertNoWarningsLogged();
        Assert.AreEqual(0, result.TotalSegmentsWithErrors);
        Assert.AreEqual(expectedChanges, result.TotalSegmentsUpdated);
        Assert.IsFalse(segments.Any(segment => segment.GetTarget().Contains("Loire")), "A Loire was left in a target.");

        for (var index = 0; index < before.Count; index++)
        {
            Assert.AreEqual(TagCount(before[index].GetTarget()), TagCount(segments[index].GetTarget()),
                $"Inline tags changed in segment {index}: {segments[index].GetTarget()}");

            if (!before[index].GetTarget().Contains("Loire"))
            {
                Assert.AreEqual(before[index].GetTarget(), segments[index].GetTarget(), $"Segment {index} changed although it has no Loire.");
            }
        }
    }

    private static int TagCount(string text) =>
        text.Split('<').Length - 1 + text.Split('{').Length - 1;

    private static string InputPath(string name)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "Input", name);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException($"Input/{name} was not found above {AppContext.BaseDirectory}.");
    }

    private InvocationContext WithWarningLog(InvocationContext context)
    {
        context.Logger = new Logger("Debug") { LogWarning = (message, _) => _loggedWarnings.Add(message ?? string.Empty) };
        return context;
    }

    private void AssertNoWarningsLogged() => Assert.IsEmpty(_loggedWarnings, string.Join(Environment.NewLine, _loggedWarnings));

    private List<Segment> Segments(FileReference file) => Load(_files.Read(file), file.Name);

    private static List<Segment> Load(string content, string name)
    {
        var loaded = Transformation.Load(new MemoryStream(Encoding.UTF8.GetBytes(content)), name);
        Assert.IsTrue(loaded.Success, loaded.Error);
        return loaded.Value.GetUnits().SelectMany(unit => unit.Segments).ToList();
    }

    private void Print(object result) => TestContext.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
}
