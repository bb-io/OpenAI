using Apps.OpenAI.Actions;
using Apps.OpenAI.Models.Requests.Background;
using Blackbird.Applications.Sdk.Common.Files;
using Newtonsoft.Json;
using Tests.OpenAI.Base;

namespace Tests.OpenAI;

[TestClass]
public class ReportingActionsTests : TestBase
{
    [TestMethod]
    public async Task CreateMqmReportInBackground_XliffFile_Success()
    {
        foreach (var context in InvocationContext)
        {
            var actions = new ReportingActions(context, FileManagementClient);
            var request = new CreateMqmReportInBackgroundRequest()
            {
                ModelId = "gpt-4.1",
                File = new FileReference
                {
                    Name = "mqm.xlf"
                },
                TargetLanguage = "fr"
            };
            
            var result = await actions.CreateMqmReportInBackground(request);
            
            Assert.IsNotNull(result);
            Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
        }
    }
}