using Apps.OpenAI.Actions;
using Apps.OpenAI.Constants;
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

        var response  = await action.ReviewText(new ReviewTextRequest
        {
            SourceText = "This is a test text to review.",
            TargetText = "Este es un texto de prueba para revisar.",
            SourceLanguage = "en-US",
            TargetLanguage = "es-ES",
            Model = "gpt-4.1"
        });

        PrintResult(response);
        Assert.IsNotNull(response);
    }

    [TestMethod, ContextDataSource(ConnectionTypes.OpenAi)]
    public async Task ReviewFileTest(InvocationContext context)
    {
        var action = new ReviewActions(context, FileManagementClient);
        var request = new ReviewContentRequest
        {
            SourceLanguage = "en-GB",
            TargetLanguage = "pt-PT",
            Model = "gpt-5.1",
            File = new FileReference { Name = "test.xliff" },
            OutputFileHandling = "original"
        };

        var response = await action.ReviewContent(request);

        PrintResult(response);
        Assert.IsNotNull(response);
    }
}
