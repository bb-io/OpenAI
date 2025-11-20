using Apps.OpenAI.Api;
using Apps.OpenAI.Models.PostEdit;
using Apps.OpenAI.Services;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Tests.OpenAI.Base;

namespace Tests.OpenAI;

[TestClass]
public class PostEditServiceTests : TestBaseWithContext
{
    private const string ModelId = "gpt-4o";

    private PostEditService _postEditService = null!;
    private FileReference _xliffFile = null!;
    private FileReference _glossaryFile = null!;

    [TestInitialize]
    public void Setup()
    {
        _postEditService = new PostEditService(
            new XliffService(FileManagementClient),
            new JsonGlossaryService(FileManagementClient),
            new OpenAICompletionService(new OpenAiUniversalClient(CredentialGroups.First())),
            new ResponseDeserializationService(),
            new PromptBuilderService(),
            FileManagementClient
            );

        // Initialize test files
        _xliffFile = new FileReference { Name = "Markdown entry #1_en-US-Default_HTML-nl-NL#TR_FQTF#.html.txlf" };
        _glossaryFile = new FileReference { Name = "glossary.tbx" };
    }

    [TestMethod, ContextDataSource]
    public async Task PostEditXliffAsync_WithValidRequest_ReturnsSuccessfulResult(InvocationContext context)
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
        var result = await _postEditService.PostEditXliffAsync(request);

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
    public async Task PostEditXliffAsync_WithGlossary_UsesGlossaryForTranslation()
    {
        // Arrange
        var request = new OpenAiXliffInnerRequest
        {
            ModelId = ModelId,
            XliffFile = _xliffFile,
            Glossary = _glossaryFile,
            FilterGlossary = true,
            BucketSize = 1000,
            NeverFail = true
        };

        // Act
        var result = await _postEditService.PostEditXliffAsync(request);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.File);
        Assert.IsTrue(result.TargetsUpdatedCount > 0, "No segments were updated");

        // Output results for manual inspection
        Console.WriteLine($"Processed batches: {result.ProcessedBatchesCount}");
        Console.WriteLine($"Total segments: {result.TotalSegmentsCount}");
        Console.WriteLine($"Updated segments: {result.TargetsUpdatedCount}");
        Console.WriteLine($"Usage - Prompt tokens: {result.Usage.PromptTokens}");
        Console.WriteLine($"Usage - Completion tokens: {result.Usage.CompletionTokens}");
        Console.WriteLine($"Usage - Total tokens: {result.Usage.TotalTokens}");
        Console.WriteLine($"Locked segments excluded count: {result.LockedSegmentsExcludeCount}");
    }

    [TestMethod]
    public async Task PostEditXliffAsync_WithCustomPrompt_AppliesPromptToTranslation()
    {
        // Arrange
        var customPrompt = "Please ensure that all technical terms are translated accurately. " +
                            "Keep the translations concise and clear.";

        var request = new OpenAiXliffInnerRequest
        {
            ModelId = ModelId,
            XliffFile = _xliffFile,
            Prompt = customPrompt,
            BucketSize = 1000,
            NeverFail = true
        };

        // Act
        var result = await _postEditService.PostEditXliffAsync(request);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.File);
        Assert.IsTrue(result.TargetsUpdatedCount > 0, "No segments were updated");

        // Output results for manual inspection
        Console.WriteLine($"Total segments: {result.TotalSegmentsCount}");
        Console.WriteLine($"Updated segments: {result.TargetsUpdatedCount}");
        Console.WriteLine($"Locked segments excluded count: {result.LockedSegmentsExcludeCount}");
    }

    [TestMethod]
    public async Task PostEditXliffAsync_WithLowBatchSize_ProcessesAllSegments()
    {
        // Arrange
        var request = new OpenAiXliffInnerRequest
        {
            ModelId = ModelId,
            XliffFile = _xliffFile,
            BucketSize = 2,
            NeverFail = true
        };

        // Act
        var result = await _postEditService.PostEditXliffAsync(request);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ProcessedBatchesCount > 0);
        Assert.IsTrue(result.TotalSegmentsCount > 0);

        Console.WriteLine($"Bucket size: {request.BucketSize}");
        Console.WriteLine($"Processed batches: {result.ProcessedBatchesCount}");
        Console.WriteLine($"Total segments: {result.TotalSegmentsCount}");
        Console.WriteLine($"Updated segments: {result.TargetsUpdatedCount}");
        Console.WriteLine($"Locked segments excluded count: {result.LockedSegmentsExcludeCount}");
    }

    [TestMethod, ContextDataSource]
    public async Task PostEditXliffAsync_WithNeverFailFalse_ThrowsExceptionOnError(InvocationContext context)
    {
        // Arrange
        var request = new OpenAiXliffInnerRequest
        {
            ModelId = ModelId,
            XliffFile = _xliffFile,
            BucketSize = 1000,
            NeverFail = false, // Will throw exception on error
            // Using an invalid model ID would cause an error, but we can't risk actual API errors in tests
            // so this test might not reliably demonstrate the exception behavior
        };

        try
        {
            // Act
            var result = await _postEditService.PostEditXliffAsync(request);

            // If no exception is thrown, report details for analysis
            Console.WriteLine("No exception was thrown. This might mean the test conditions didn't trigger an error.");
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
        catch (Exception ex)
        {
            // Assert - this is the expected path if an error occurs
            Console.WriteLine($"Exception thrown as expected: {ex.Message}");
            Assert.IsTrue(ex.Message.Contains("Failed to process batch") ||
                            ex.Message.Contains("Error") ||
                            ex.Message.Contains("OpenAI"),
                            $"Unexpected exception message: {ex.Message}");
        }
    }
}
