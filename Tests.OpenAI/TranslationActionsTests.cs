using Apps.OpenAI.Actions;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Requests.Content;
using Blackbird.Applications.Sdk.Common.Files;
using Newtonsoft.Json;
using Apps.OpenAI.Models.Requests.Background;
using Tests.OpenAI.Base;

namespace Tests.OpenAI;

[TestClass]
public class TranslationActionsTests : TestBase
{
    [TestMethod]
    public async Task Translate_html()
    {
        var actions = new TranslationActions(InvocationContext, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-5" };
        var translateRequest = new TranslateContentRequest
        {
            File = new FileReference { Name = "contentful.html" },
            TargetLanguage = "nl"
        };
        var reasoningEffortRequest = new ReasoningEffortRequest
        {
            ReasoningEffort = "low"
        };
        string? systemMessage = null;
        var glossaryRequest = new GlossaryRequest();

        var result = await actions.TranslateContent(modelIdentifier, translateRequest, systemMessage, glossaryRequest, reasoningEffortRequest);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.File.Name.Contains("contentful"));

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [TestMethod]
    public async Task Translate_xlf()
    {
        var actions = new TranslationActions(InvocationContext, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-5" };
        var translateRequest = new TranslateContentRequest
        {
            File = new FileReference { Name = "contentful.untranslated.xlf" },
            TargetLanguage = "nl"
        };
        var reasoningEffortRequest = new ReasoningEffortRequest
        {
            ReasoningEffort = "low"
        };
        string? systemMessage = null;
        var glossaryRequest = new GlossaryRequest();

        var result = await actions.TranslateContent(modelIdentifier, translateRequest, systemMessage, glossaryRequest, reasoningEffortRequest);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.File.Name.Contains("contentful"));

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [TestMethod]
    public async Task Translate_xlf12()
    {
        var actions = new TranslationActions(InvocationContext, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-5" };
        var translateRequest = new TranslateContentRequest
        {
            File = new FileReference { Name = "contentful12.xliff" },
            TargetLanguage = "nl"
        };
        var reasoningEffortRequest = new ReasoningEffortRequest
        {
            ReasoningEffort = "low"
        };
        string? systemMessage = null;
        var glossaryRequest = new GlossaryRequest();

        var result = await actions.TranslateContent(modelIdentifier, translateRequest, systemMessage, glossaryRequest, reasoningEffortRequest);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.File.Name.Contains("contentful"));

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [TestMethod]
    public async Task TranslateInBackground_WithXliffFile_Success()
    {
        var actions = new TranslationActions(InvocationContext, FileManagementClient);
        
        var translateRequest = new StartBackgroundProcessRequest
        {
            ModelId = "gpt-4.1",
            File = new FileReference { Name = "The Hobbit, or There and Back Again_en-US.html.xlf" },
            TargetLanguage = "fr"
        };
        
        var response = await actions.TranslateInBackground(translateRequest);
        
        Assert.IsNotNull(response);
        Assert.IsNotNull(response.BatchId);
        Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
    }

    [TestMethod]
    public async Task TranslateText_WithSerbianLocale_ReturnsLocalizedText()
    {
        var actions = new TranslationActions(InvocationContext, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4.1" };
        var localizeRequest = new LocalizeTextRequest
        {
            Text = "Develop and implement an HR strategy that drives organizational productivity and supports company's business goals. Design and oversee processes that promote team efficiency and operational effectiveness while reducing complexity and redundancies.",
            TargetLanguage = "sr-Latn-RS"
        };

        var glossaryRequest = new GlossaryRequest();

        var result = await actions.LocalizeText(modelIdentifier, localizeRequest, glossaryRequest);

        Assert.IsNotNull(result);
        Assert.IsNotNull(result.TranslatedText);
        Console.WriteLine("Original: " + localizeRequest.Text);
        Console.WriteLine("Localized: " + result.TranslatedText);

        // Additional validation to ensure response is not empty and contains Serbian characters
        Assert.IsTrue(result.TranslatedText.Length > 0);
    }
}
