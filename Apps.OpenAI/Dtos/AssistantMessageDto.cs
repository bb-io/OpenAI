﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.OpenAI.Dtos
{
    public class AssistantMessageDto
    {
        public string Role { get; set; }
        public string Content { get; set; }

        [JsonProperty("file_ids")]
        public IEnumerable<string> FileIds { get; set;}
    }
}
