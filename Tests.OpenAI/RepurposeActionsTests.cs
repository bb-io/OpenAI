using Tests.OpenAI.Base;
using Apps.OpenAI.Actions;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Tests.OpenAI;

[TestClass]
public class RepurposeActionsTests : TestBaseWithContext
{
    [TestMethod, ContextDataSource]
    public async Task CreateSummary_ReturnsRepurposeResponse(InvocationContext context)
    {
		// Arrange
		var actions = new RepurposeActions(context, FileManagementClient);
		var content = "Hello world! This needs to be repurposed";
        var model = new TextChatModelIdentifier { ModelId = "gpt-4.1" };
        var request = new RepurposeRequest { ToneOfVOice = "very angry" };

        // Act
        var result = await actions.CreateSummary(model, content, request, new GlossaryRequest { });

        // Assert
        PrintResult(result);
        Assert.IsNotNull(result);
    }
}
