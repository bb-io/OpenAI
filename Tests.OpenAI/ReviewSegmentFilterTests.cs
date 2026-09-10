using System.Text;
using Apps.OpenAI.Utils;
using Apps.OpenAI.Actions;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Review;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Extensions;
using Blackbird.Filters.Transformations;

namespace Tests.OpenAI;

[TestClass]
public class ReviewSegmentFilterTests
{
    internal static Transformation LoadContent()
    {
        const string xml = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en" trgLang="de">
              <file id="f1">
                <unit id="u1">
                  <segment id="initial" state="initial"><source>One</source><target>Eins</target></segment>
                  <segment id="missing"><source>Two</source><target>Zwei</target></segment>
                  <segment id="translated" state="translated"><source>Three</source><target>Drei</target></segment>
                  <segment id="reviewed" state="reviewed"><source>Four</source><target>Vier</target></segment>
                  <segment id="final" state="final"><source>Five</source><target>Fünf</target></segment>
                  <segment id="empty" state="translated"><source>Six</source><target> </target></segment>
                  <ignorable id="ignorable"><source> </source><target> </target></ignorable>
                </unit>
              </file>
            </xliff>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var result = Transformation.Load(stream, "review.xlf", "application/xliff+xml");
        Assert.IsTrue(result.Success, result.Error);
        return result.Value;
    }

    [TestMethod]
    public void Defaults_PreserveExistingEligibility()
    {
        var segments = LoadContent().GetUnits().SelectMany(unit => unit.Segments).ToList();
        // IsInitial is defined by the Filters library; preserve the existing predicate exactly.
        var expected = segments.Where(segment => !segment.IsIgnorbale && !segment.IsInitial
            && segment.State != SegmentState.Final && !string.IsNullOrWhiteSpace(segment.GetTarget()))
            .Select(segment => segment.Id).ToArray();
        AssertSelected(null, expected);
        AssertSelected([], expected);
    }

    [TestMethod]
    public void ExplicitExclusions_ReplaceDefaultsAndIgnoreCase()
    {
        AssertSelected(["rEvIeWeD"], "initial", "missing", "translated", "final");
        AssertSelected(["Initial", "Final"], "translated", "reviewed");
        AssertSelected(["Translated", "Reviewed"], "initial", "missing", "final");
        AssertSelected(["Initial", "Translated", "Reviewed", "Final"]);
    }

    [TestMethod]
    public void InvalidExclusions_ThrowConfigurationError()
    {
        foreach (var value in new[] { "unknown", "", " ", "1", "Translated, Reviewed", null })
            Assert.ThrowsExactly<PluginMisconfigurationException>(() => ReviewSegmentFilter.Create([value!]));
    }

    [TestMethod]
    public async Task Batching_OnlyProcessesEligibleSegmentsAndPreservesExcludedStates()
    {
        var content = LoadContent();
        var segments = content.GetUnits().SelectMany(unit => unit.Segments).ToList();
        var excluded = segments.Single(segment => segment.Id == "reviewed");
        var calls = new List<string>();
        var results = await content.GetUnits().Batch(10, ReviewSegmentFilter.Create(["Reviewed"]))
            .Process(batch =>
            {
                var scores = batch.Select(item =>
                {
                    calls.Add(item.Segment.Id);
                    return (float?)0.9f;
                }).ToList();
                return Task.FromResult<IEnumerable<float?>>(scores);
            });

        foreach (var (_, scores) in results)
            foreach (var (segment, score) in scores)
            {
                Assert.AreEqual(0.9f, score);
                segment.State = SegmentState.Final;
            }

        CollectionAssert.AreEquivalent(new[] { "initial", "missing", "translated", "final" }, calls);
        Assert.AreEqual(SegmentState.Reviewed, excluded.State);
        Assert.IsFalse(excluded.TargetAttributes.Any(attr => attr.Name == "extradata"));
    }

    [TestMethod]
    public async Task NoEligibleSegments_DoesNotInvokeBatchProcessor()
    {
        var calls = 0;
        await LoadContent().GetUnits()
            .Batch(10, ReviewSegmentFilter.Create(["Initial", "Translated", "Reviewed", "Final"]))
            .Process(batch =>
            {
                calls++;
                return Task.FromResult<IEnumerable<float?>>([]);
            });
        Assert.AreEqual(0, calls);
    }

    private static void AssertSelected(string[]? exclusions, params string[] expected)
    {
        var segments = LoadContent().GetUnits().SelectMany(unit => unit.Segments).ToList();
        // Give the ignorable a nonempty target to test its exclusion independently.
        var ignorable = segments.Single(segment => segment.Id == "ignorable");
        ignorable.SetTarget("Ignored target");
        var actual = segments.Where(ReviewSegmentFilter.Create(exclusions)).Select(segment => segment.Id).ToArray();
        CollectionAssert.AreEquivalent(expected, actual);
    }

    [TestMethod]
    public async Task ReviewWithAllStatesExcluded_ReturnsFileAndZeroMetrics()
    {
        var files = new MemoryFileClient();
        var action = new ReviewActions(new InvocationContext
        {
            AuthenticationCredentialsProviders =
            [
                new AuthenticationCredentialsProvider(CredNames.ConnectionType, ConnectionTypes.AzureOpenAi),
                new AuthenticationCredentialsProvider(CredNames.ApiKey, "unused"),
                new AuthenticationCredentialsProvider(CredNames.Url, "http://127.0.0.1:1")
            ]
        }, files);
        var response = await action.ReviewContent(new TextChatModelIdentifier { ModelId = "unused" },
            new ReviewContentRequest
            {
                File = new FileReference { Name = "review.xlf", ContentType = "application/xliff+xml" },
                ExcludeSegmentStates = ["Initial", "Translated", "Reviewed", "Final"]
            });

        Assert.IsNotNull(files.Output);
        Assert.AreEqual("review.xlf", response.File.Name);
        Assert.AreEqual(0, response.TotalSegmentsProcessed);
        Assert.AreEqual(0, response.TotalSegmentsFinalized);
        Assert.AreEqual(0, response.TotalSegmentsUnderThreshhold);
        Assert.AreEqual(0f, response.AverageMetric);
        Assert.AreEqual(0f, response.PercentageSegmentsUnderThreshhold);
        Assert.AreEqual(0d, Convert.ToDouble(response.Usage.TotalTokens));
        var output = Transformation.Load(new MemoryStream(files.Output), "review.xlf", "application/xliff+xml");
        Assert.IsTrue(output.Success, output.Error);
        CollectionAssert.AreEqual(
            LoadContent().GetUnits().SelectMany(unit => unit.Segments).Select(segment => segment.State).ToArray(),
            output.Value.GetUnits().SelectMany(unit => unit.Segments).Select(segment => segment.State).ToArray());
    }

    private sealed class MemoryFileClient : IFileManagementClient
    {
        public byte[]? Output { get; private set; }

        public Task<Stream> DownloadAsync(FileReference reference) => Task.FromResult(LoadContent().ToStream());

        public async Task<FileReference> UploadAsync(Stream stream, string contentType, string fileName)
        {
            using var output = new MemoryStream();
            await stream.CopyToAsync(output);
            Output = output.ToArray();
            return new FileReference { Name = "review.xlf", ContentType = contentType };
        }
    }
}
