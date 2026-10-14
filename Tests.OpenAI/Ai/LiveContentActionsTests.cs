using System.Globalization;
using System.Text;
using Apps.OpenAI.Actions;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Requests.Content;
using Apps.OpenAI.Models.Requests.Review;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Transformations;
using Newtonsoft.Json;
using Tests.OpenAI.Base;

namespace Tests.OpenAI.Ai;

[TestClass]
public class LiveContentActionsTests : TestBaseWithContext
{
    private readonly InMemoryFileManagementClient _files = new();
    private readonly List<string> _loggedWarnings = [];

    // End to end against the real API: every segment comes back translated, nothing is reported as
    // lost, and usage is recorded from the provider's own token counts.
    [TestMethod, ContextDataSource(ConnectionTypes.AzureOpenAi)]
    public async Task TranslateContent_RealModel_TranslatesEverySegment(InvocationContext context)
    {
        var file = _files.Add("live.xlf", TestXliff.Untranslated(
            "en",
            "de",
            ["The weather is lovely today.", "Please restart the application.", "Our office opens at nine."]));

        var result = await new TranslationActions(WithWarningLog(context), _files).TranslateContent(
            new TextChatModelIdentifier(),
            new TranslateContentRequest { File = file, TargetLanguage = "de" },
            null,
            new GlossaryRequest(),
            new ReasoningEffortRequest { ReasoningEffort = "low" });

        Print(result);
        var segments = Segments(result.File);
        Assert.AreEqual(3, result.TargetsUpdatedCount);
        AssertNoWarningsLogged();
        Assert.IsGreaterThan(0, result.Usage.TotalTokens);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Usage.ModelUsed));
        Assert.IsTrue(segments.All(segment => segment.State == SegmentState.Translated));
        Assert.IsTrue(segments.All(segment => !string.IsNullOrWhiteSpace(segment.GetTarget()) && segment.GetTarget() != segment.GetSource()));
    }

    // A plain HTML page has no source language, so the action detects it, translates, and returns
    // the page in its original format.
    [TestMethod, ContextDataSource(ConnectionTypes.AzureOpenAi)]
    public async Task TranslateContent_RealModelHtmlWithoutSourceLanguage_ReturnsTranslatedHtml(InvocationContext context)
    {
        var file = _files.Add("live.html", "<html><body><h1>Welcome</h1><p>Thank you for your order.</p></body></html>", "text/html");

        var result = await new TranslationActions(WithWarningLog(context), _files).TranslateContent(
            new TextChatModelIdentifier(),
            new TranslateContentRequest { File = file, TargetLanguage = "fr", OutputFileHandling = "original" },
            null,
            new GlossaryRequest(),
            new ReasoningEffortRequest { ReasoningEffort = "low" });

        Print(result);
        var html = _files.Read(result.File);
        TestContext.WriteLine(html);
        Assert.AreEqual(2, result.TargetsUpdatedCount);
        AssertNoWarningsLogged();
        Assert.IsTrue(html.Contains("<h1>") && !html.Contains("Thank you for your order"));
    }

    // A clear mistranslation (dog for cat) must be corrected by Edit, and the edited segment marked
    // Reviewed.
    [TestMethod, ContextDataSource(ConnectionTypes.AzureOpenAi)]
    public async Task EditContent_RealModelClearMistranslation_IsCorrected(InvocationContext context)
    {
        var file = _files.Add("live-edit.xlf", TestXliff.Translated(
            "en",
            "de",
            ("The cat is sleeping on the sofa.", "Der Hund schläft auf dem Sofa."),
            ("Good morning.", "Guten Morgen.")));

        var result = await new EditActions(WithWarningLog(context), _files).EditContent(
            new TextChatModelIdentifier(),
            new EditContentRequest { File = file },
            null,
            new GlossaryRequest(),
            new ReasoningEffortRequest { ReasoningEffort = "low" });

        Print(result);
        var segments = Segments(result.File);
        Assert.AreEqual(2, result.TotalSegmentsReviewed);
        Assert.IsEmpty(result.ErrorDetails!, string.Join(Environment.NewLine, result.ErrorDetails!));
        AssertNoWarningsLogged();
        Assert.Contains("Katze", segments[0].GetTarget());
        Assert.IsTrue(segments.All(segment => segment.State == SegmentState.Reviewed));
    }

    // Review must score a correct translation above a wrong one, write the scores into the file,
    // report both segments as processed, and name the model that answered even when none was chosen.
    [TestMethod, ContextDataSource(ConnectionTypes.AzureOpenAi)]
    public async Task ReviewContent_RealModel_ScoresGoodAboveBad(InvocationContext context)
    {
        var file = _files.Add("live-review.xlf", TestXliff.Translated(
            "en",
            "de",
            ("Please restart the application.", "Bitte starten Sie die Anwendung neu."),
            ("Our office opens at nine.", "Der Mond ist aus grünem Käse.")));

        var result = await new ReviewActions(WithWarningLog(context), _files).ReviewContent(
            new TextChatModelIdentifier(),
            new ReviewContentRequest { File = file, TargetLanguage = "de", Threshold = 0.8 });

        Print(result);
        var scores = Segments(result.File)
            .Select(segment => double.Parse(segment.TargetAttributes.Single(attribute => attribute.Name.LocalName == "extradata").Value, CultureInfo.InvariantCulture))
            .ToList();
        Assert.AreEqual(2, result.TotalSegmentsProcessed);
        AssertNoWarningsLogged();
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Usage.ModelUsed), "The model that answered was not reported.");
        Assert.IsGreaterThan(scores[1], scores[0], $"good {scores[0]} vs bad {scores[1]}");
        Assert.IsGreaterThanOrEqualTo(1, result.TotalSegmentsUnderThreshhold);
        Assert.IsTrue(result.PercentageSegmentsUnderThreshhold is > 0 and <= 1, "The percentage must stay a 0..1 fraction.");
    }

    // Translate text with the source language given makes exactly one request and returns a real
    // translation, plus the system and user prompt that were sent.
    [TestMethod, ContextDataSource(ConnectionTypes.AzureOpenAi)]
    public async Task TranslateText_RealModel_ReturnsTranslation(InvocationContext context)
    {
        const string source = "Please restart the application.";

        var result = await new TranslationActions(WithWarningLog(context), _files).LocalizeText(
            new TextChatModelIdentifier(),
            new LocalizeTextRequest { Text = source, SourceLanguage = "en", TargetLanguage = "de", ReasoningEffort = "low" },
            new GlossaryRequest());

        Print(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.TranslatedText));
        Assert.AreNotEqual(source, result.TranslatedText);
        AssertNoWarningsLogged();
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.SystemPrompt));
        Assert.Contains(source, result.UserPrompt);
        Assert.IsGreaterThan(0, result.Usage.TotalTokens);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Usage.ModelUsed));
    }

    // Edit text must fix a planted mistranslation (dog for cat) and leave a correct target alone.
    [TestMethod, ContextDataSource(ConnectionTypes.AzureOpenAi)]
    public async Task EditText_RealModel_CorrectsMistranslationAndKeepsCorrectTarget(InvocationContext context)
    {
        var actions = new EditActions(WithWarningLog(context), _files);

        var wrong = await actions.PostEditRequest(
            new TextChatModelIdentifier(),
            new PostEditRequest { SourceText = "The cat is sleeping on the sofa.", TargetText = "Der Hund schläft auf dem Sofa.", SourceLanguage = "en", TargetLanguage = "de", ReasoningEffort = "low" },
            new GlossaryRequest());
        var correct = await actions.PostEditRequest(
            new TextChatModelIdentifier(),
            new PostEditRequest { SourceText = "Good morning.", TargetText = "Guten Morgen.", SourceLanguage = "en", TargetLanguage = "de", ReasoningEffort = "low" },
            new GlossaryRequest());

        Print(wrong);
        Print(correct);
        Assert.Contains("Katze", wrong.EditedText);
        Assert.AreEqual("Guten Morgen.", correct.EditedText);
        AssertNoWarningsLogged();
    }

    // Review text must score a correct translation above a nonsense one, on the 0..1 scale.
    [TestMethod, ContextDataSource(ConnectionTypes.AzureOpenAi)]
    public async Task ReviewText_RealModel_ScoresGoodAboveBad(InvocationContext context)
    {
        var actions = new ReviewActions(WithWarningLog(context), _files);

        var good = await actions.ReviewText(
            new TextChatModelIdentifier(),
            new ReviewTextRequest { SourceText = "Please restart the application.", TargetText = "Bitte starten Sie die Anwendung neu.", SourceLanguage = "en", TargetLanguage = "de" });
        var bad = await actions.ReviewText(
            new TextChatModelIdentifier(),
            new ReviewTextRequest { SourceText = "Our office opens at nine.", TargetText = "Der Mond ist aus grünem Käse.", SourceLanguage = "en", TargetLanguage = "de" });

        Print(good);
        Print(bad);
        Assert.IsTrue(good.Score is >= 0 and <= 1 && bad.Score is >= 0 and <= 1);
        Assert.IsGreaterThan(bad.Score, good.Score, $"good {good.Score} vs bad {bad.Score}");
        AssertNoWarningsLogged();
        Assert.IsFalse(string.IsNullOrWhiteSpace(good.Usage.ModelUsed));
    }

    // Warnings are not part of the action output; the library logs them through InvocationContext.Logger.
    private InvocationContext WithWarningLog(InvocationContext context)
    {
        context.Logger = new Logger("Debug") { LogWarning = (message, _) => _loggedWarnings.Add(message ?? string.Empty) };
        return context;
    }

    private void AssertNoWarningsLogged() => Assert.IsEmpty(_loggedWarnings, string.Join(Environment.NewLine, _loggedWarnings));

    private List<Segment> Segments(FileReference file)
    {
        var loaded = Transformation.Load(new MemoryStream(Encoding.UTF8.GetBytes(_files.Read(file))), file.Name);
        Assert.IsTrue(loaded.Success, loaded.Error);
        return loaded.Value.GetUnits().SelectMany(unit => unit.Segments).ToList();
    }

    private void Print(object result) => TestContext.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
}
