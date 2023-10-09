using Blackbird.Applications.Sdk.Common;
using System;

namespace Apps.OpenAI;

public class OpenAiApplication : IApplication
{
    public string Name
    {
        get => "OpenAI";
        set { }
    }

    public T GetInstance<T>()
    {
        throw new NotImplementedException();
    }
}