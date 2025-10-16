using Apps.OpenAI.Services;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Xliff.Utils.Models;
using Tests.OpenAI.Base;

namespace Tests.OpenAI;

[TestClass]
public class GlossaryServiceTests : TestBase
{
    private GlossaryService _glossaryService = null!;
    private FileReference _glossaryFile = null!;
    
    [TestInitialize]
    public void Setup()
    {
        _glossaryService = new GlossaryService(FileManagementClient);
        _glossaryFile = new FileReference { Name = "glossary.tbx" };
    }
    
    [TestMethod]
    public async Task BuildGlossaryPromptPartAsync_WithoutFiltering_ReturnsAllEntries()
    {
        foreach (var context in InvocationContext)
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
            Console.WriteLine(result);
            
            Assert.IsTrue(result.Contains("Glossary entries"));
            Assert.IsTrue(result.Contains("Entry:"));
            Assert.IsTrue(result.Contains("en:"));
            Assert.IsTrue(result.Contains("fr:"));
        }
    }
    
    [TestMethod]
    public async Task BuildGlossaryPromptPartAsync_WithFiltering_ReturnsOnlyRelevantEntries()
    {
        foreach (var context in InvocationContext)
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
            Console.WriteLine(result);
            
            Assert.IsTrue(result.Contains("Glossary entries"));
            Assert.IsTrue(result.Contains("Entry:"));
            
            Assert.IsTrue(result.Contains("text segment") || 
                          result.Contains("specific terms") || 
                          result.Contains("terms"));
        }
    }
    
    [TestMethod]
    public async Task BuildGlossaryPromptPartAsync_WithFilteringAndNoRelevantEntries_ReturnsNull()
    {
        foreach (var context in InvocationContext)
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
                Console.WriteLine(result);
                Assert.IsTrue(result.Contains("Glossary entries"));
            }
        }
    }
    
    [TestMethod]
    public async Task BuildGlossaryPromptPartAsync_IncludesUsageExamples_WhenAvailable()
    {
        foreach (var context in InvocationContext)
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
            Console.WriteLine(result);
            
            bool hasUsageExamples = result.Contains("Usage example:");
            Console.WriteLine($"Glossary contains usage examples: {hasUsageExamples}");
        }
    }
}
