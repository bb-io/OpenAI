using Apps.OpenAI.Services;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Xliff.Utils.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tests.OpenAI.Base;

namespace Tests.OpenAI;

[TestClass]
public class JsonGlossaryServiceTests : TestBase
{
    private JsonGlossaryService _jsonGlossaryService = null!;
    private FileReference _glossaryFile = null!;
    
    [TestInitialize]
    public void Setup()
    {
        _jsonGlossaryService = new JsonGlossaryService(FileManagementClient);
        _glossaryFile = new FileReference { Name = "glossary.tbx" };
    }
    
    [TestMethod]
    public async Task BuildGlossaryPromptAsync_WithoutFiltering_ReturnsJsonFormattedGlossary()
    {
        // Arrange
        var translationUnits = new List<TranslationUnit>
        {
            new() { Id = "1", Source = "This is a test source text" }
        };
        
        // Act
        var result = await _jsonGlossaryService.BuildGlossaryPromptAsync(_glossaryFile, translationUnits, false);
        
        // Assert
        Assert.IsNotNull(result);
        Console.WriteLine(result);
        
        // Verify it contains the JSON structure explanation
        Assert.IsTrue(result.Contains("JSON format"));
        
        // Extract JSON part from the prompt
        var jsonStart = result.IndexOf("Glossary:") + "Glossary:".Length;
        var jsonString = result.Substring(jsonStart).Trim();
        
        // Verify it's valid JSON
        var jObject = JObject.Parse(jsonString);
        Assert.IsNotNull(jObject["conceptEntries"]);
        Assert.IsTrue(jObject["conceptEntries"]!.Type == JTokenType.Array);
        
        // Check that at least one entry exists with terms
        var entries = jObject["conceptEntries"]!.ToObject<JArray>();
        Assert.IsTrue(entries!.Count > 0);
        
        var firstEntry = entries[0];
        Assert.IsNotNull(firstEntry["terms"]);
    }
    
    [TestMethod]
    public async Task BuildGlossaryPromptAsync_WithFiltering_ReturnsOnlyRelevantEntriesInJson()
    {
        // Arrange
        var translationUnits = new List<TranslationUnit>
        {
            new() { Id = "1", Source = "text segment that contains some specific terms" }
        };
        
        // Act
        var result = await _jsonGlossaryService.BuildGlossaryPromptAsync(_glossaryFile, translationUnits, true);
        
        // Assert
        Assert.IsNotNull(result);
        Console.WriteLine(result);
        
        // Extract JSON part from the prompt
        var jsonStart = result.IndexOf("Glossary:") + "Glossary:".Length;
        var jsonString = result.Substring(jsonStart).Trim();
        
        // Verify it's valid JSON
        var jObject = JObject.Parse(jsonString);
        var entries = jObject["conceptEntries"]!.ToObject<JArray>();
        Assert.IsTrue(entries!.Count > 0);
        
        // Check that filtered entries contain at least one of the terms from the source
        var containsRelevantTerms = false;
        var sourceTerms = new[] { "text segment", "specific terms", "terms" };
        
        foreach (var entry in entries)
        {
            var terms = entry["terms"]!.ToObject<JObject>();
            foreach (var languageTerms in terms!.Properties())
            {
                var termValue = languageTerms.Value!.ToString();
                if (sourceTerms.Any(term => termValue.Contains(term, StringComparison.OrdinalIgnoreCase)))
                {
                    containsRelevantTerms = true;
                    break;
                }
            }
            
            if (containsRelevantTerms)
                break;
        }
        
        Assert.IsTrue(containsRelevantTerms, "The filtered JSON glossary should contain terms relevant to the source");
    }
}
