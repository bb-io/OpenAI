using Apps.OpenAI.Actions;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Requests.Content;
using Blackbird.Applications.Sdk.Common.Files;
using Newtonsoft.Json;
using Apps.OpenAI.Models.Requests.Background;
using Tests.OpenAI.Base;

namespace Tests.OpenAI;

[TestClass]
public class EditTests : TestBase
{
    [TestMethod]
    public async Task Edit_xliff()
    {
        foreach (var context in InvocationContext)
        {
            var actions = new EditActions(context, FileManagementClient);
            var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4o" };
            var editRequest = new EditContentRequest
            {
                File = new FileReference { Name = "contentful.html.xlf" },
            };
            var reasoningEffortRequest = new ReasoningEffortRequest
            {
                ReasoningEffort = "low"
            };
            string? systemMessage = null;
            var glossaryRequest = new GlossaryRequest();

            var result = await actions.EditContent(modelIdentifier, editRequest, systemMessage, glossaryRequest, reasoningEffortRequest);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.File.Name.Contains("contentful"));

            Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
        }
    }

    [TestMethod]
    public async Task Taus_edit()
    {
        foreach (var context in InvocationContext)
        {
            var actions = new EditActions(context, FileManagementClient);
            var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4.1" };
            var editRequest = new EditContentRequest
            {
                File = new FileReference { Name = "taus.xliff" },
            };
            var reasoningEffortRequest = new ReasoningEffortRequest
            {
                ReasoningEffort = "low"
            };
            string? systemMessage = null;
            var glossaryRequest = new GlossaryRequest();

            var result = await actions.EditContent(modelIdentifier, editRequest, systemMessage, glossaryRequest, reasoningEffortRequest);
            Assert.IsNotNull(result);

            Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
        }
    }

    [TestMethod]
    public async Task EditInBackground_WithXliffFile_Success()
    {
        foreach (var context in InvocationContext)
        {
            var actions = new EditActions(context, FileManagementClient);
            
            var editRequest = new StartBackgroundProcessRequest
            {
                ModelId = "gpt-4.1",
                File = new FileReference { Name = "The Hobbit, or There and Back Again_en-US.html.xlf" },
                TargetLanguage = "fr"
            };
            
            var response = await actions.EditInBackground(editRequest);
            
            Assert.IsNotNull(response);
            Assert.IsNotNull(response.BatchId);
            Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
        }
    }
}
