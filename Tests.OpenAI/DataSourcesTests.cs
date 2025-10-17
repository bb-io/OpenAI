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
        foreach (var context in InvocationContext)
        {
            var handler = new TextChatModelDataSourceHandler(context);
            var data = await handler.GetDataAsync(new DataSourceContext(), CancellationToken.None);

            PrintResult(data);
            Assert.AreNotEqual(data.Count(), 0);
        }
    }

    [TestMethod]
    public async Task GetDataAsync_ForGenerateImagesModels_ReturnsNonEmptyCollection()
    {
        foreach (var context in InvocationContext)
        {
            var handler = new ImageGenerationModelDataSourceHandler(context);
            var data = await handler.GetDataAsync(new DataSourceContext(), CancellationToken.None);

            PrintResult(data);
            Assert.AreNotEqual(data.Count(), 0);
        }
    }

    [TestMethod]
    public async Task GetDataAsync_ForAudioModels_ReturnsNonEmptyCollection()
    {
        foreach (var context in InvocationContext)
        {
            var handler = new SpeechCreationModelDataSourceHandler(context);
            var data = await handler.GetDataAsync(new DataSourceContext(), CancellationToken.None);

            PrintResult(data);
            Assert.AreNotEqual(data.Count(), 0);
        }
    }

    [TestMethod]
    public async Task GetDataAsync_ForAssistantModels_ReturnsNonEmptyCollection()
    {
        foreach (var context in InvocationContext)
        {
            var handler = new AssistantsDataSourceHandler(context);
            var data = await handler.GetDataAsync(new DataSourceContext(), CancellationToken.None);

            PrintResult(data);
            Assert.AreNotEqual(data.Count(), 0);
        }
    }

    [TestMethod]
    public async Task GetDataAsync_ForBatches_ReturnsNonEmptyCollection()
    {
        foreach (var context in InvocationContext)
        {
            // Arrange
            var handler = new BatchDataSourceHandler(context);

            // Act
            var data = await handler.GetDataAsync(new DataSourceContext(), CancellationToken.None);

            // Assert
            PrintResult(data);
            Assert.AreNotEqual(data.Count(), 0);
        }
    }

    [TestMethod]
    public async Task Locales()
    {
        foreach (var context in InvocationContext)
        {
            var handler = new LocaleDataSourceHandler();
            var data = handler.GetData();

            PrintResult(data);
            Assert.AreNotEqual(data.Count(), 0);
        }
    }

    private static void PrintResult(IEnumerable<DataSourceItem> data)
    {
        foreach (var item in data)
            Console.WriteLine($"{item.Value}: {item.DisplayName}");
    }
}
