using Blackbird.Applications.Sdk.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.OpenAI.Model.Requests
{
    public class EditRequest
    {
        [Display("Input Text")]
        public string InputText { get; set; }

        public string Instruction { get; set; }
        public string Model { get; set; }
    }
}
