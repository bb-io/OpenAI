using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.OpenAI.Models.Requests.Assistant
{
    public class RunRequest
    {
        [Display("Assistant ID")]
        [DataSource(typeof(AssistantsDataSourceHandler))]
        public string AssistantId { get; set; }

        [Display("Message")]
        public string? Message { get; set; }

        [Display("Files")]
        public IEnumerable<FileReference>? Files { get; set; }
    }
}
