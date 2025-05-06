using Apps.OpenAI.Models.PostEdit;
using Apps.OpenAI.Services;
using Blackbird.Applications.Sdk.Common.Files;
using Tests.OpenAI.Base;

namespace Tests.OpenAI;

[TestClass]
public class ProcessXliffServiceTests : TestBase
{
    private const string ModelId = "gpt-4o";

    private ProcessXliffService _processXliffService = null!;
    private FileReference _xliffFile = null!;
    private FileReference _glossaryFile = null!;

    [TestInitialize]
    public void Setup()
    {
        _processXliffService = new ProcessXliffService(
            new XliffService(FileManagementClient),
            new JsonGlossaryService(FileManagementClient),
            new OpenAICompletionService(new Apps.OpenAI.Api.OpenAIClient(Creds)),
            new ResponseDeserializationService(),
            new PromptBuilderService(),
            FileManagementClient
            );

        // Initialize test files
        _xliffFile = new FileReference { Name = "Markdown entry #1_en-US-Default_HTML-nl-NL#TR_FQTF#.html.txlf" };
        _glossaryFile = new FileReference { Name = "glossary.tbx" };
    }

    [TestMethod]
    public async Task ProcessXliffAsync_WithValidRequest_ReturnsSuccessfulResult()
    {
        // Arrange
        var request = new OpenAiXliffInnerRequest
        {
            ModelId = ModelId,
            XliffFile = _xliffFile,
            NeverFail = true,
            BucketSize = 50
        };

        // Act
        var result = await _processXliffService.ProcessXliffAsync(request);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.File);
        Assert.IsNotNull(result.Usage);
        Assert.IsTrue(result.ProcessedBatchesCount > 0);
        Assert.IsTrue(result.TotalSegmentsCount > 0);

        // Output results for manual inspection
        Console.WriteLine($"Processed batches: {result.ProcessedBatchesCount}");
        Console.WriteLine($"Total segments: {result.TotalSegmentsCount}");
        Console.WriteLine($"Updated segments: {result.TargetsUpdatedCount}");
        Console.WriteLine($"Errors count: {result.ErrorMessages.Count}");
        Console.WriteLine($"Locked segments excluded count: {result.LockedSegmentsExcludeCount}");

        foreach (var error in result.ErrorMessages)
        {
            Console.WriteLine($"Error: {error}");
        }
    }

    [TestMethod]
    public async Task ProcessXliffAsync_WithGlossaryAndCustomPrompt_ProcessesCorrectly()
    {
        // Arrange
        var customPrompt = "Please ensure accurate translation of technical terms and maintain consistent terminology.";
        
        var request = new OpenAiXliffInnerRequest
        {
            ModelId = ModelId,
            XliffFile = _xliffFile,
            Glossary = _glossaryFile,
            Prompt = customPrompt,
            FilterGlossary = true,
            BucketSize = 25,
            NeverFail = true,
            MaxTokens = 4000
        };

        // Act
        var result = await _processXliffService.ProcessXliffAsync(request);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.File);
        Assert.IsTrue(result.ProcessedBatchesCount > 0);
        Assert.IsTrue(result.TargetsUpdatedCount > 0, "No segments were updated");

        // Output results for manual inspection
        Console.WriteLine($"Processed batches: {result.ProcessedBatchesCount}");
        Console.WriteLine($"Total segments: {result.TotalSegmentsCount}");
        Console.WriteLine($"Updated segments: {result.TargetsUpdatedCount}");
        Console.WriteLine($"Usage - Prompt tokens: {result.Usage.PromptTokens}");
        Console.WriteLine($"Usage - Completion tokens: {result.Usage.CompletionTokens}");
        Console.WriteLine($"Usage - Total tokens: {result.Usage.TotalTokens}");
        Console.WriteLine($"Locked segments excluded count: {result.LockedSegmentsExcludeCount}");

        foreach (var error in result.ErrorMessages)
        {
            Console.WriteLine($"Error: {error}");
        }
    }
}
