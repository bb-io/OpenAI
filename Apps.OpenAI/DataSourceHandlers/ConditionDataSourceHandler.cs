using Blackbird.Applications.Sdk.Common.Dictionaries;
using System.Collections.Generic;

namespace Apps.OpenAI.DataSourceHandlers
{
    public class ConditionDataSourceHandler : IStaticDataSourceHandler
    {
        private static Dictionary<string, string> EnumValues => new()
        {
            { ">", "Score is above threshold" },
            { ">=", "Score is above or equal threshold" },
            { "=", "Score is same as threshold" },
            { "<", "Score is below threshold" },
            { "<=", "Score is below or equal threshold" }
        };

        public Dictionary<string, string> GetData()
        {
            return EnumValues;
        }

    }
}
