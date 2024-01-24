using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.OpenAI.Models.Responses.Chat
{
    public class ExtractedGlossaryItem
    {
        public string Term { get; set; }
        public string Description { get; set; }
    }

    public class GlossaryItemWrapper
    {
        public List<ExtractedGlossaryItem> Result { get; set; }
    }
}
