using Apps.OpenAI.DataSourceHandlers;
using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Tests.OpenAI.Base;

namespace Tests.OpenAI;

[TestClass]
public class DataSourceHandlerTests : TestBase
{
    [TestMethod]
    public async Task GetDataAsync_ForTextChatModels_ReturnsNonEmptyCollection()
    {
        var handler = new TextChatModelDataSourceHandler(InvocationContext);
        var data = await handler.GetDataAsync(new DataSourceContext(), CancellationToken.None);

        PrintResult(data);
        Assert.AreNotEqual(data.Count(), 0);
    }

    [TestMethod]
    public async Task GetDataAsync_ForGenerateImagesModels_ReturnsNonEmptyCollection()
    {
        var handler = new ImageGenerationModelDataSourceHandler(InvocationContext);
        var data = await handler.GetDataAsync(new DataSourceContext(), CancellationToken.None);

        PrintResult(data);
        Assert.AreNotEqual(data.Count(), 0);
    }

    [TestMethod]
    public async Task GetDataAsync_ForAudioModels_ReturnsNonEmptyCollection()
    {
        var handler = new SpeechCreationModelDataSourceHandler(InvocationContext);
        var data = await handler.GetDataAsync(new DataSourceContext(), CancellationToken.None);

        PrintResult(data);
        Assert.AreNotEqual(data.Count(), 0);
    }

    [TestMethod]
    public async Task GetDataAsync_ForAssistantModels_ReturnsNonEmptyCollection()
    {
        var handler = new AssistantsDataSourceHandler(InvocationContext);
        var data = await handler.GetDataAsync(new DataSourceContext(), CancellationToken.None);

        PrintResult(data);
        Assert.AreNotEqual(data.Count(), 0);
    }

    [TestMethod]
    public async Task GetDataAsync_ForBatches_ReturnsNonEmptyCollection()
    {
        // Arrange
        var handler = new BatchDataSourceHandler(InvocationContext);

        // Act
        var data = await handler.GetDataAsync(new DataSourceContext(), CancellationToken.None);

        // Assert
        PrintResult(data);
        Assert.AreNotEqual(data.Count(), 0);
    }

    [TestMethod]
    public async Task Locales()
    {
        var handler = new LocaleDataSourceHandler();
        var data = handler.GetData();

        PrintResult(data);
        Assert.AreNotEqual(data.Count(), 0);
    }

    private static void PrintResult(IEnumerable<DataSourceItem> data)
    {
        foreach (var item in data)
            Console.WriteLine($"{item.Value}: {item.DisplayName}");
    }
}
