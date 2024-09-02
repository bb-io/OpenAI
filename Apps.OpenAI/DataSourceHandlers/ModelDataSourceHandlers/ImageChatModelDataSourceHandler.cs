using Blackbird.Applications.Sdk.Common.Dictionaries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers
{
    public class ImageChatModelDataSourceHandler : IStaticDataSourceHandler
    {
        public Dictionary<string, string> GetData()
        {
            return new Dictionary<string, string> {
                { "gpt-4o", "gpt-4o"},
                { "gpt-4o-mini", "gpt-4o-mini" },
                { "chatgpt-4o-latest", "chatgpt-4o-latest" },
                { "gpt-4-turbo", "gpt-4-turbo" }
            };
        }
    }
}
