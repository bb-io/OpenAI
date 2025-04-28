using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Tests.OpenAI.Base;

namespace Tests.OpenAI;

[TestClass]
public class ModelDataSourceHandlerTests : TestBase
{
    [TestMethod]
    public async Task GetDataAsync_ForTextChatModels_ReturnsNonEmptyCollection()
    {
        var handler = new TextChatModelDataSourceHandler(InvocationContext);
        var data = await handler.GetDataAsync(new DataSourceContext(), CancellationToken.None);

        foreach (var item in data)
        {
            Console.WriteLine($"{item.Value}: {item.DisplayName}");
        }

        Assert.AreNotEqual(data.Count(), 0);
    }


    [TestMethod]
    public async Task GetDataAsync_ForGenerateImagesModels_ReturnsNonEmptyCollection()
    {
        var handler = new ImageGenerationModelDataSourceHandler(InvocationContext);
        var data = await handler.GetDataAsync(new DataSourceContext(), CancellationToken.None);

        foreach (var item in data)
        {
            Console.WriteLine($"{item.Value}: {item.DisplayName}");
        }

        Assert.AreNotEqual(data.Count(), 0);
    }

    [TestMethod]
    public async Task GetDataAsync_ForAudioModels_ReturnsNonEmptyCollection()
    {
        var handler = new SpeechCreationModelDataSourceHandler(InvocationContext);
        var data = await handler.GetDataAsync(new DataSourceContext(), CancellationToken.None);

        foreach (var item in data)
        {
            Console.WriteLine($"{item.Value}: {item.DisplayName}");
        }

        Assert.AreNotEqual(data.Count(), 0);
    }

}
