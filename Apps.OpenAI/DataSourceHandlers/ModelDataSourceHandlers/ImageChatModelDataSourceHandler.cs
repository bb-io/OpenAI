using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers
{
    public class ImageChatModelDataSourceHandler : IStaticDataSourceItemHandler
    {
        public IEnumerable<DataSourceItem> GetData()
        {
            return new List<DataSourceItem>
            {
                new("gpt-5.2", "gpt-5.2"),
                new("gpt-5.2-pro", "gpt-5.2-pro"),
                new( "gpt-4o", "gpt-4o"),
                new( "gpt-4o-mini", "gpt-4o-mini" ),
                new( "chatgpt-4o-latest", "chatgpt-4o-latest" ),
                new ( "gpt-4-turbo", "gpt-4-turbo" ),
            };
        }
    }
}
