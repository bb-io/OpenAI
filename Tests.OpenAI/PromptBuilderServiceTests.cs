using Apps.OpenAI.Services;
using Apps.OpenAI.Utils;
using Blackbird.Xliff.Utils.Models;
using Tests.OpenAI.Base;

namespace Tests.OpenAI;

[TestClass]
public class PromptBuilderServiceTests : TestBase
{
    private PromptBuilderService _promptBuilderService = null!;
    
    [TestInitialize]
    public void Setup()
    {
        _promptBuilderService = new PromptBuilderService();
    }
    
    [TestMethod]
    public void GetSystemPrompt_ReturnsCorrectPrompt()
    {
        // Act
        var result = _promptBuilderService.GetSystemPrompt();
        
        Console.WriteLine(result);

        // Assert
        Assert.IsNotNull(result);
    }
    
    [TestMethod]
    public void BuildUserPrompt_WithAllParameters_ReturnsCorrectPrompt()
    {
        // Arrange
        var sourceLanguage = "en";
        var targetLanguage = "fr";
        var batch = new TranslationUnit[]
        {
            new() { Id = "1", Source = "Hello world", Target = "Bonjour le monde" },
            new() { Id = "2", Source = "Testing the prompt builder", Target = "Tester le générateur de prompt" }
        };
        var additionalPrompt = "Please be formal in your translations.";
        var glossaryPrompt = "Use 'monde' for 'world'.";
        
        // Act
        var result = _promptBuilderService.BuildUserPrompt(
            sourceLanguage,
            targetLanguage,
            batch,
            additionalPrompt,
            glossaryPrompt);

        Console.WriteLine(result);

        // Assert
        Assert.IsNotNull(result);
        
        // Check that all required parts are in the prompt
        Assert.IsTrue(result.Contains(sourceLanguage));
        Assert.IsTrue(result.Contains(targetLanguage));
        Assert.IsTrue(result.Contains(additionalPrompt));
        Assert.IsTrue(result.Contains(glossaryPrompt));
        Assert.IsTrue(result.Contains("Hello world"));
        Assert.IsTrue(result.Contains("Testing the prompt builder"));
        Assert.IsTrue(result.Contains("1"));
        Assert.IsTrue(result.Contains("2"));
        Assert.IsTrue(result.Contains("Review each translation unit containing source text and initial target translation"));
    }
    
    [TestMethod]
    public void BuildUserPrompt_WithoutOptionalParameters_ReturnsCorrectPrompt()
    {
        // Arrange
        var sourceLanguage = "en";
        var targetLanguage = "fr";
        var batch = new TranslationUnit[]
        {
            new() { Id = "1", Source = "Hello world", Target = "Bonjour le monde" }
        };
        
        // Act
        var result = _promptBuilderService.BuildUserPrompt(
            sourceLanguage,
            targetLanguage,
            batch,
            null,
            null);
        
        Console.WriteLine(result);

        // Assert
        Assert.IsNotNull(result);
        
        // Check that all required parts are in the prompt
        Assert.IsTrue(result.Contains(sourceLanguage));
        Assert.IsTrue(result.Contains(targetLanguage));
        Assert.IsTrue(result.Contains("Hello world"));
        Assert.IsTrue(result.Contains("1"));
        Assert.IsTrue(result.Contains("Review each translation unit containing source text and initial target translation"));
        
        // Check that optional parameters are not included
        Assert.IsFalse(result.Contains("Please be formal"));
        Assert.IsFalse(result.Contains("Use 'monde'"));
    }
}
