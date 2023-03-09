using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.OpenAI.Model.Requests
{
    public class ChatRequest
    {
        public string Message { get; set; }

        public int MaximumTokens { get; set; }
    }
}
