﻿using Apps.OpenAI.Dtos;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.SDK.Blueprints.Interfaces.Edit;

namespace Apps.OpenAI.Models.Responses.Chat;

public class EditResponse : IEditTextOutput
{
    [Display("System prompt")]
    public string SystemPrompt { get; set; }

    [Display("User prompt")]
    public string UserPrompt { get; set; }

    [Display("Edited text")]
    public string EditedText { get; set; }

    [Display("Usage")]
    public UsageDto Usage { get; set; }
}