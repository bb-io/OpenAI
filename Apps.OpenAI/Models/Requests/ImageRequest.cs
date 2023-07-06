using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.OpenAI.Models.Requests
{
    public class ImageRequest
    {
        public string Prompt { get; set; }
        public string? Size { get; set; }
    }
}
