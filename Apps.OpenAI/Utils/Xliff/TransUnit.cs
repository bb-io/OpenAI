using System.Collections.Generic;

namespace Apps.OpenAI.Utils.Xliff
{
    public class TransUnit
    {
        public string Source { get; set; }

        public string Target { get; set; }

        public string Id { get; set; }

        public List<Blackbird.Xliff.Utils.Models.Tag> Tags { get; set; } 
    }
}