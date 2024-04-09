using Blackbird.Applications.Sdk.Common;
using System;
using System.Collections.Generic;
using Blackbird.Applications.Sdk.Common.Metadata;

namespace Apps.OpenAI;

public class OpenAIApplication : IApplication, ICategoryProvider
{
    public IEnumerable<ApplicationCategory> Categories
    {
        get => [ApplicationCategory.ArtificialIntelligence, ApplicationCategory.Multimedia];
        set { }
    }
    
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