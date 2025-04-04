using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Apps.OpenAI.Models.Entities;
using Apps.OpenAI.Models.PostEdit;
using Apps.OpenAI.Services.Abstract;
using Newtonsoft.Json;

namespace Apps.OpenAI.Services;

public class ResponseDeserializationService : IResponseDeserializationService
{
    public DeserializeTranslationEntitiesResult DeserializeResponse(string content)
    {
        try
        {
            var deserializedResponse = JsonConvert.DeserializeObject<TranslationEntities>(content);
            return new(deserializedResponse.Translations, true, string.Empty);
        }
        catch (Exception ex)
        {
            var partialTranslations = ExtractValidTranslationsFromIncompleteJsonWithErrorHandling(content);
            
            if (partialTranslations.Count > 0)
            {
                return new(partialTranslations, true, $"Partial deserialization succeeded with {partialTranslations.Count} translations. Original error: {ex.Message}");
            }
            
            var truncatedContent = content.Substring(0, Math.Min(content.Length, 200)) + "...";
            return new(new(), false, $"Failed to deserialize OpenAI response: {ex.Message}. Response: {truncatedContent}");
        }
    }

    private List<TranslationEntity> ExtractValidTranslationsFromIncompleteJsonWithErrorHandling(string incompleteJson)
    {
        try 
        {
            return ExtractValidTranslationsFromIncompleteJson(incompleteJson);
        }
        catch (Exception)
        {
            return [];
        }
    }

    private List<TranslationEntity> ExtractValidTranslationsFromIncompleteJson(string incompleteJson)
    {
        var result = new List<TranslationEntity>();
        var pattern = @"\{\s*""translation_id""\s*:\s*""([^""]+)""\s*,\s*""translated_text""\s*:\s*""((?:[^""\\]|\\.)*)""(?:\s*,\s*""quality_score""\s*:\s*([0-9.]+))?\s*\}";
        
        var matches = Regex.Matches(incompleteJson, pattern);
        
        foreach (Match match in matches)
        {
            if (match.Groups.Count >= 3)
            {
                var entity = new TranslationEntity
                {
                    TranslationId = match.Groups[1].Value,
                    TranslatedText = ProcessJsonString(match.Groups[2].Value)
                };
                
                if (match.Groups.Count > 3 && !string.IsNullOrEmpty(match.Groups[3].Value))
                {
                    if (float.TryParse(match.Groups[3].Value, out float score))
                    {
                        entity.QualityScore = score;
                    }
                }
                
                result.Add(entity);
            }
        }
        
        return result;
    }

    private string ProcessJsonString(string jsonString)
    {
        return Regex.Replace(jsonString, @"\\(.)", match =>
        {
            char escapedChar = match.Groups[1].Value[0];
            switch (escapedChar)
            {
                case 'n': return "\n";
                case 'r': return "\r";
                case 't': return "\t";
                case '\\': return "\\";
                case '"': return "\"";
                default: return match.Value;
            }
        });
    }
}
