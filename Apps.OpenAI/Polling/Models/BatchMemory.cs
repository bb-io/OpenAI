using System;

namespace Apps.OpenAI.Polling.Models;

public class BatchMemory
{
    public DateTime? LastPollingTime { get; set; }

    public bool Triggered { get; set; }
}