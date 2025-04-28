using Apps.OpenAI.Actions;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Image;
using Tests.OpenAI.Base;

namespace Tests.OpenAI
{
    [TestClass]
    public class ImageGenerateServiceTests : TestBase
    {
        [TestMethod]
        public async Task GenerateImageAsync()
        {
            var handler = new ImageActions(InvocationContext, FileManagementClient);
            var data = await handler.GenerateImage(
                new ImageGenerationModelIdentifier { ModelId = "gpt-image-1" },
                new ImageRequest { Prompt = "Generate photo of cat with donuts" });

            Assert.IsNotNull(data);
        }
    }
}
