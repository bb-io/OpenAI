using Blackbird.Applications.Sdk.Common;

namespace Apps.OpenAI.Models.Requests
{
    public class TranslationRequest
    {
        [Display("File name")]
        public string FileName { get; set; }
        
        public byte[] File { get; set; }

        [Display("Temperature")]
        public float? Temperature { get; set; }
    }
}
