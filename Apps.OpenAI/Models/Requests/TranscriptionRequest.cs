using Blackbird.Applications.Sdk.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.OpenAI.Models.Requests
{
    public class TranscriptionRequest
    {
        [Display("File name")]
        public string FileName { get; set; }
        public byte[] File { get; set; }

        [Display("Language (ISO 639-1)")]
        public string? Language { get; set; }

        [Display("Temperature")]
        public float? Temperature { get; set; }
    }
}
