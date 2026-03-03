using Newtonsoft.Json;
using System.Collections.Generic;

namespace Apps.OpenAI.Models.Responses.Review;
    public class ReviewJsonResponse
    {
        [JsonProperty("translations")]
        public List<ReviewItem>? Translations { get; set; }
    }

    public class ReviewItem
    {
        [JsonProperty("translation_id")]
        public string? TranslationId { get; set; }

        [JsonProperty("quality_score")]
        public float QualityScore { get; set; }
    }

