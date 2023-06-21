using Blackbird.Applications.Sdk.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.OpenAI.Models.Requests
{
    public class TranslationRequest
    {
        [Display("File name")]
        public string FileName { get; set; }
        public byte[] File { get; set; }
    }
}
