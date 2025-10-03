using Tests.OpenAI.Base;
using Apps.OpenAI.Actions;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Tests.OpenAI;

[TestClass]
public class RepurposeActionsTests : TestBase
{
    [TestMethod]
    public async Task CreateSummary_EmptyRequiredInputs_ThrowsException()
    {
		// Arrange
		var actions = new RepurposeActions(InvocationContext, FileManagementClient);
		string emptyContent = "";
        var emptyModelIdentifier = new TextChatModelIdentifier { ModelId = "" };

        // Act
        var ex = await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(
            async () => await actions.CreateSummary(emptyModelIdentifier, emptyContent, new RepurposeRequest { }, new GlossaryRequest { })
        );

        // Assert
        Equals(ex.Message, "These parameters are required and can't be empty: Model; Text; ");
    }
}
