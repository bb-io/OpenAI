using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Apps.OpenAI.Dtos
{
    public class UsageDto
    {
        [Display("Model used")]
        public string? ModelUsed { get; set; }

        [Display("Prompt tokens")]
        [JsonProperty("prompt_tokens")]
        public int PromptTokens { get; set; }

        [Display("Completion tokens")]
        [JsonProperty("completion_tokens")]
        public int CompletionTokens { get; set; }

        [Display("Total tokens")]
        [JsonProperty("total_tokens")]
        public int TotalTokens { get; set; }

        [JsonProperty("input_tokens")]
        private int InputTokens
        {
            set => PromptTokens = value;
        }

        [JsonProperty("output_tokens")]
        private int OutputTokens
        {
            set => CompletionTokens = value;
        }

        public static UsageDto operator +(UsageDto u1, UsageDto u2)
        {
            return new UsageDto
            {
                ModelUsed = MergeModelUsed(u1.ModelUsed, u2.ModelUsed),
                PromptTokens = u1.PromptTokens + u2.PromptTokens,
                CompletionTokens = u1.CompletionTokens + u2.CompletionTokens,
                TotalTokens = u1.TotalTokens + u2.TotalTokens,
            };
        }

        public static UsageDto Zero => new() { CompletionTokens = 0, TotalTokens = 0, PromptTokens = 0 };

        public static UsageDto Sum(IEnumerable<UsageDto> usages)
        {
            return usages.Aggregate(Zero, (acc, x) => acc + x);
        }

        private static string? MergeModelUsed(string? model1, string? model2)
        {
            if (string.IsNullOrWhiteSpace(model1))
                return model2;

            if (string.IsNullOrWhiteSpace(model2))
                return model1;

            return string.Equals(model1, model2) ? model1 : "multiple";
        }
    }    
}
