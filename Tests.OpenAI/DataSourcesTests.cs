using Apps.OpenAI.DataSourceHandlers;
using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using Tests.OpenAI.Base;

namespace Tests.OpenAI;

[TestClass]
public class DataSourceHandlerTests : TestBaseWithContext
{
    [TestMethod, ContextDataSource]
    public async Task GetDataAsync_ForTextChatModels_ReturnsNonEmptyCollection(InvocationContext context)
    {
        var handler = new TextChatModelDataSourceHandler(context);
        var data = await handler.GetDataAsync(new DataSourceContext(), CancellationToken.None);

        PrintDataHandlerResult(data);
        Assert.AreNotEqual(0, data.Count());
        Assert.AreEqual(data.Select(x => x.Value).Count(), data.Select(x => x.Value).Distinct().Count());
    }

    [TestMethod, ContextDataSource]
    public async Task GetDataAsync_ForGenerateImagesModels_ReturnsNonEmptyCollection(InvocationContext context)
    {
        var handler = new ImageGenerationModelDataSourceHandler(context);
        var data = await handler.GetDataAsync(new DataSourceContext(), CancellationToken.None);

        PrintDataHandlerResult(data);
        Assert.AreNotEqual(0, data.Count());
    }

    [TestMethod, ContextDataSource]
    public async Task GetDataAsync_ForAudioModels_ReturnsNonEmptyCollection(InvocationContext context)
    {
        var handler = new SpeechCreationModelDataSourceHandler(context);
        var data = await handler.GetDataAsync(new DataSourceContext(), CancellationToken.None);

        PrintDataHandlerResult(data);
        Assert.AreNotEqual(0, data.Count());
    }

    [TestMethod, ContextDataSource]
    public async Task GetDataAsync_ForBatches_ReturnsNonEmptyCollection(InvocationContext context)
    {
        // Arrange
        var handler = new BatchDataSourceHandler(context);

        // Act
        var data = await handler.GetDataAsync(new DataSourceContext(), CancellationToken.None);

        // Assert
        PrintDataHandlerResult(data);
        Assert.AreNotEqual(0, data.Count());
    }

    [TestMethod, ContextDataSource]
    public async Task Locales(InvocationContext context)
    {
        var handler = new LocaleDataSourceHandler();
        var data = handler.GetData();

        PrintResult(data);
        Assert.AreNotEqual(0, data.Count());
    }
}
