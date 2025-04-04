using Apps.OpenAI.Models.Entities;
using Apps.OpenAI.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Linq;

namespace Tests.OpenAI;

[TestClass]
public class ResponseDeserializationServiceTests
{
    private ResponseDeserializationService _deserializationService = null!;

    [TestInitialize]
    public void Initialize()
    {
        _deserializationService = new ResponseDeserializationService();
    }

    [TestMethod]
    public void DeserializeResponse_ValidJson_ReturnsAllTranslations()
    {
        // Arrange
        var validJson = @"{
            ""translations"": [
                {
                    ""translation_id"": ""1"",
                    ""translated_text"": ""Bravo, vous êtes là ! La journée vient de s'améliorer - profitez des conseils suivants !""
                },
                {
                    ""translation_id"": ""2"",
                    ""translated_text"": ""Une section de texte comme celle-ci est appelée un segment de texte. Commencez à traduire avec brio dès maintenant !""
                },
                {
                    ""translation_id"": ""3"",
                    ""translated_text"": ""Arriba, Arriba ! Andale, Andale ! Soyez aussi rapide que Speedy Gonzales. Appuyez simplement sur TAB pour enregistrer et passer au segment de texte suivant une fois terminé.""
                }
            ]
        }";

        // Act
        var result = _deserializationService.DeserializeResponse(validJson);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(3, result.Translations.Count);
        Assert.AreEqual("1", result.Translations[0].TranslationId);
        Assert.AreEqual("2", result.Translations[1].TranslationId);
        Assert.AreEqual("3", result.Translations[2].TranslationId);

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [TestMethod]
    public void DeserializeResponse_TruncatedJson_ExtractsValidTranslations()
    {
        // Arrange
        var truncatedJson = @"{
            ""translations"": [
                {
                    ""translation_id"": ""1"",
                    ""translated_text"": ""Bravo, vous êtes là ! La journée vient de s'améliorer - profitez des conseils suivants !""
                },
                {
                    ""translation_id"": ""2"",
                    ""translated_text"": ""Une section de texte comme celle-ci est appelée un segment de texte. Commencez à traduire avec brio dès maintenant !""
                },
                {
                    ""translation_id"": ""3"",
                    ""translated_text"": ""Arriba, Arriba ! Andale, Andale ! Soyez aussi rapide que Speedy Gonzales. Appuyez simplement sur TAB pour enregistrer et passer au segment de texte suivant une fois terminé.""
                },
                {
                    ""translation_id"": ""6"",
                    ""translated_text"": ""Nous aimons simplement vous voir heureux, c'est pourquoi LingoChecks vérifie automatiquement les traductions selon des critères prédéfinis.\n    LingoHub vérifie notamment s";

        // Act
        var result = _deserializationService.DeserializeResponse(truncatedJson);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(3, result.Translations.Count);
        Assert.AreEqual("1", result.Translations[0].TranslationId);
        Assert.AreEqual("2", result.Translations[1].TranslationId);
        Assert.AreEqual("3", result.Translations[2].TranslationId);
        Assert.IsFalse(result.Translations.Any(t => t.TranslationId == "6"));

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [TestMethod]
    public void DeserializeResponse_WithQualityScore_ParsesScoreCorrectly()
    {
        // Arrange
        var jsonWithQualityScore = @"{
            ""translations"": [
                {
                    ""translation_id"": ""1"",
                    ""translated_text"": ""Test translation"",
                    ""quality_score"": 0.95
                }
            ]
        }";

        // Act
        var result = _deserializationService.DeserializeResponse(jsonWithQualityScore);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, result.Translations.Count);
        Assert.AreEqual(0.95f, result.Translations[0].QualityScore);

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [TestMethod]
    public void DeserializeResponse_WithEscapedCharacters_HandlesEscapingCorrectly()
    {
        // Arrange
        var jsonWithEscaping = @"{
            ""translations"": [
                {
                    ""translation_id"": ""1"",
                    ""translated_text"": ""Line 1\nLine 2\tTabbed\""Quoted\""""
                }
            ]
        }";

        // Act
        var result = _deserializationService.DeserializeResponse(jsonWithEscaping);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, result.Translations.Count);
        Assert.AreEqual("Line 1\nLine 2\tTabbed\"Quoted\"", result.Translations[0].TranslatedText);

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [TestMethod]
    public void DeserializeResponse_CompletelyInvalidJson_ReturnsFailure()
    {
        // Arrange
        var invalidJson = "This is not JSON at all";

        // Act
        var result = _deserializationService.DeserializeResponse(invalidJson);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual(0, result.Translations.Count);
        StringAssert.Contains(result.Error, "Failed to deserialize OpenAI response");

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [TestMethod]
    public void DeserializeResponse_MalformedButWithValidParts_ExtractsValidParts()
    {
        // Arrange
        var malformedJson = @"{ ""invalid"": true, 
            ""translations"": [ERROR HERE
                {
                    ""translation_id"": ""1"",
                    ""translated_text"": ""This should be extracted""
                },
                {
                    ""translation_id"": ""2"",
                    ""translated_text"": ""This should also be extracted""
                }
            ]";

        // Act
        var result = _deserializationService.DeserializeResponse(malformedJson);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(2, result.Translations.Count);
        Assert.AreEqual("1", result.Translations[0].TranslationId);
        Assert.AreEqual("This should be extracted", result.Translations[0].TranslatedText);
        Assert.AreEqual("2", result.Translations[1].TranslationId);
        Assert.AreEqual("This should also be extracted", result.Translations[1].TranslatedText);

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }
}
