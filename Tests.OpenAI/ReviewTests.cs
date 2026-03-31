using Apps.OpenAI.Actions;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Review;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Tests.OpenAI.Base;

namespace Tests.OpenAI;

[TestClass]
public class ReviewTests : TestBaseWithContext
{
    [TestMethod, ContextDataSource]
    public async Task ReviewTextTest(InvocationContext context)
    {
        var action = new ReviewActions(context, FileManagementClient);
        var model = new TextChatModelIdentifier { ModelId = "gpt-4.1" };

        var response  = await action.ReviewText(model, new ReviewTextRequest
        {
            SourceText = "This is a test text to review.",
            TargetText = "Este es un texto de prueba para revisar.",
            SourceLanguage = "en-US",
            TargetLanguage = "es-ES"
        });

        PrintResult(response);
        Assert.IsNotNull(response);
    }

    [TestMethod, ContextDataSource(ConnectionTypes.OpenAi)]
    public async Task ReviewFileTest(InvocationContext context)
    {
        var action = new ReviewActions(context, FileManagementClient);
        var model = new TextChatModelIdentifier { ModelId = "gpt-5.1" };
        var request = new ReviewContentRequest
        {
            SourceLanguage = "en-GB",
            TargetLanguage = "pt-PT",
            File = new FileReference { Name = "test.xliff" },
            OutputFileHandling = "original"
        };

        var response = await action.ReviewContent(model, request);

        PrintResult(response);
        Assert.IsNotNull(response);
    }
}
