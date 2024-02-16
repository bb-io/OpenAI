using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.OpenAI.Models.Requests.Chat
{
    public class ExtractGlossaryRequest
    {
        [Display("Content")]
        public string Content { get; set; }

        [Display("Languages (ISO 639-1)")]
        [DataSource(typeof(IsoLanguageDataSourceHandler))]
        public IEnumerable<string> Languages { get; set; }

        public string? Name { get; set; }

        [Display("Temperature")]
        [DataSource(typeof(TemperatureDataSourceHandler))]
        public float? Temperature { get; set; }

    }
}
