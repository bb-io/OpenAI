using Apps.OpenAI.Connections;
using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Tests.OpenAI.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.OpenAI
{
    [TestClass]
    public class DataSources : TestBase
    {
        [TestMethod]
        public async Task TextChatModelsReturnsValues()
        {
            var handler = new TextChatModelDataSourceHandler(InvocationContext);
            var data = await handler.GetDataAsync(new DataSourceContext(), CancellationToken.None);

            foreach(var item in data)
            {
                Console.WriteLine($"{item.Value}: {item.DisplayName}");
            }

            Assert.AreNotEqual(data.Count(), 0);
        }
    }
}
