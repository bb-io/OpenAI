﻿using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Responses.Chat;

public class TranslateXliffResponse
{
    public FileReference File { get; set; }

    public string[] LogString { get; set; }
}