using Blackbird.Applications.Sdk.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.OpenAI.Model.Responses
{
    public class EditResponse
    {
        [Display("Edited text")]
        public string EditText { get; set; }
    }
}
