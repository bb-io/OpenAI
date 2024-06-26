﻿using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.Models.Requests.Chat;

public class LocalizeTextRequest
{
    public string Text { get; set; }
        
    [StaticDataSource(typeof(LocaleDataSourceHandler))]
    public string Locale { get; set; }
    
    [Display("Maximum tokens")]
    public int? MaximumTokens { get; set; }
}