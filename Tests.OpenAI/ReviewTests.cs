using Apps.OpenAI.Actions;
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

        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(response));

        Assert.IsNotNull(response);
    }

    [TestMethod, ContextDataSource]
    public async Task ReviewFileTest(InvocationContext context)
    {
        var action = new ReviewActions(context, FileManagementClient);

        var response = await action.ReviewContent(new ReviewContentRequest
        {
            SourceLanguage = "en-US",
            TargetLanguage = "es-ES",
            Model = "gpt-4.1",
            File = new FileReference
            {
              Name = "taus.xliff"
            },
            Threshold = 0.8,
        });

        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(response));

        Assert.IsNotNull(response);
    }
}
