using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using System.Collections.Generic;

namespace Apps.OpenAI.DataSourceHandlers
{
    public class ConditionDataSourceHandler : IStaticDataSourceItemHandler
    {
        public IEnumerable<DataSourceItem> GetData()
        {
            return new List<DataSourceItem>()
            {
                new( ">", "Score is above threshold" ),
                new( ">=", "Score is above or equal threshold" ),
                new( "=", "Score is same as threshold" ),
                new( "<", "Score is below threshold" ),
                new( "<=", "Score is below or equal threshold"),
            };
        }
    }
}
