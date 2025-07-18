﻿using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.SDK.Blueprints.Interfaces.Edit;

namespace Apps.OpenAI.Models.Requests.Chat;

public class PostEditRequest : IEditTextInput
{
    [Display("Source text")]
    public string SourceText { get; set; }

    [Display("Target text")]
    public string TargetText { get; set; }

    [Display("Source language")]
    [StaticDataSource(typeof(LocaleDataSourceHandler))]
    public string? SourceLanguage { get; set; }

    [Display("Target language")]
    [StaticDataSource(typeof(LocaleDataSourceHandler))]
    public string TargetLanguage { get; set; }

    [Display("Target audience")]
    public string? TargetAudience { get; set; }

    [Display("Additional prompt")]
    public string? AdditionalPrompt { get; set; }
}