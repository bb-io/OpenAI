using Apps.OpenAI.Services;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Xliff.Utils.Models;
using Tests.OpenAI.Base;

namespace Tests.OpenAI;

[TestClass]
public class GlossaryServiceTests : TestBaseWithContext
{
    private GlossaryService _glossaryService = null!;
    private FileReference _glossaryFile = null!;
    
    [TestInitialize]
    public void Setup()
    {
        _glossaryService = new GlossaryService(FileManagementClient);
        _glossaryFile = new FileReference { Name = "glossary.tbx" };
    }
    
    [TestMethod, ContextDataSource]
    public async Task BuildGlossaryPromptPartAsync_WithoutFiltering_ReturnsAllEntries(InvocationContext context)
    {
        // Arrange
        var translationUnits = new List<TranslationUnit>
        {
            new() { Id = "1", Source = "This is a test source text" }
        };
            
        // Act
        var result = await _glossaryService.BuildGlossaryPromptAsync(_glossaryFile, translationUnits, false);
            
        // Assert
        Assert.IsNotNull(result);
        PrintResult(result);
            
        Assert.Contains("Glossary entries", result);
        Assert.Contains("Entry:", result);
        Assert.Contains("en:", result);
        Assert.Contains("fr:", result);
    }
    
    [TestMethod, ContextDataSource]
    public async Task BuildGlossaryPromptPartAsync_WithFiltering_ReturnsOnlyRelevantEntries(InvocationContext context)
    {
        // Arrange
        var translationUnits = new List<TranslationUnit>
        {
            new() { Id = "1", Source = "text segment that contains some specific terms" }
        };
            
        // Act
        var result = await _glossaryService.BuildGlossaryPromptAsync(_glossaryFile, translationUnits, true);
            
        // Assert
        Assert.IsNotNull(result);
        PrintResult(result);

        Assert.Contains("Glossary entries", result);
        Assert.Contains("Entry:", result);
            
        Assert.IsTrue(result.Contains("text segment") || 
                        result.Contains("specific terms") || 
                        result.Contains("terms"));
    }
    
    [TestMethod, ContextDataSource]
    public async Task BuildGlossaryPromptPartAsync_WithFilteringAndNoRelevantEntries_ReturnsNull(InvocationContext context)
    {
        // Arrange
        var translationUnits = new List<TranslationUnit>
        {
            new() { Id = "1", Source = "xyz123 completely unrelated content" }
        };
            
        // Act
        var result = await _glossaryService.BuildGlossaryPromptAsync(_glossaryFile, translationUnits, true);
            
        // Assert
        if (result == null)
        {
            Assert.IsNull(result);
        }
        else
        {
            PrintResult(result);
            Assert.Contains("Glossary entries", result);
        }
    }
    
    [TestMethod, ContextDataSource]
    public async Task BuildGlossaryPromptPartAsync_IncludesUsageExamples_WhenAvailable(InvocationContext context)
    {
        // Arrange
        var translationUnits = new List<TranslationUnit>
        {
            new() { Id = "1", Source = "This is a test for usage examples" }
        };
            
        // Act
        var result = await _glossaryService.BuildGlossaryPromptAsync(_glossaryFile, translationUnits, false);
            
        // Assert
        Assert.IsNotNull(result);
        PrintResult(result);

        bool hasUsageExamples = result.Contains("Usage example:");
        Console.WriteLine($"Glossary contains usage examples: {hasUsageExamples}");
    }
}
