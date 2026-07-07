using Apps.OpenAI.Actions;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Requests.Glossary;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Tests.OpenAI.Base;

namespace Tests.OpenAI;

[TestClass]
public class GlossaryTests : TestBase
{
    [TestMethod, ContextDataSource(ConnectionTypes.OpenAi)]
    public async Task ExtractGlossaryFromXliff_IsSuccess(InvocationContext context)
    {
        // Arrange
        var actions = new GlossaryActions(context, FileManagementClient);
        var model = new TextChatModelIdentifier { ModelId = "gpt-5.1" };
        var extractInput = new ExtractGlossaryFromXliffRequest
        {
            File = new FileReference { Name = "The Hobbit, or There and Back Again_en-US.html.xlf" },
        };
        var chatInput = new BaseChatRequest { };

        // Act
        await actions.ExtractGlossaryFromXliff(model, extractInput, chatInput);
    }
}