using Tests.OpenAI.Base;
using Apps.OpenAI.Actions;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;

namespace Tests.OpenAI;

[TestClass]
public class RepurposeActionsTests : TestBase
{
    [TestMethod]
    public async Task CreateSummary_ReturnsRepurposeResponse()
    {
        foreach (var context in InvocationContext)
        {
		    // Arrange
		    var actions = new RepurposeActions(context, FileManagementClient);
		    var content = "Hello world! This needs to be repurposed";
            var model = new TextChatModelIdentifier { ModelId = "gpt-4.1" };
            var request = new RepurposeRequest { ToneOfVOice = "very angry" };

            // Act
            var result = await actions.CreateSummary(model, content, request, new GlossaryRequest { });

            // Assert
            PrintResult(context, result);
            Assert.IsNotNull(result);
        }
    }
}
