﻿using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using System.Collections.Generic;

namespace Apps.OpenAI.Models.Requests.Chat
{
    public class ExtractGlossaryRequest
    {
        [Display("Content")]
        public string Content { get; set; }

        [Display("Languages (ISO 639-1)")]
        [StaticDataSource(typeof(IsoLanguageDataSourceHandler))]
        public IEnumerable<string> Languages { get; set; }

        public string? Name { get; set; }

        [Display("Temperature")]
        [StaticDataSource(typeof(TemperatureDataSourceHandler))]
        public float? Temperature { get; set; }

    }
}
