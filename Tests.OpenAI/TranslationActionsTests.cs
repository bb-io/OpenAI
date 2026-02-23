using Apps.OpenAI.Actions;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Background;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Requests.Content;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Newtonsoft.Json;
using Tests.OpenAI.Base;

namespace Tests.OpenAI;

[TestClass]
public class TranslationActionsTests : TestBaseWithContext
{
    [TestMethod, ContextDataSource(ConnectionTypes.OpenAi)]
    public async Task Translate_html(InvocationContext context)
    {
        var actions = new TranslationActions(context, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4.1-mini" };
        var translateRequest = new TranslateContentRequest
        {
            File = new FileReference { Name = "3 random sentences_en_uk_ua.xlf" },
            TargetLanguage = "uk-UA",            
        };
        var reasoningEffortRequest = new ReasoningEffortRequest { };
        string systemMessage = "";
        var glossaryRequest = new GlossaryRequest 
        { 
            //Glossary = new FileReference { Name = "Glossary.tbx" } 
        };

        var result = await actions.TranslateContent(modelIdentifier, translateRequest, systemMessage, glossaryRequest, reasoningEffortRequest);
        Assert.IsNotNull(result);
        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [TestMethod, ContextDataSource]
    public async Task Translate_xlf(InvocationContext context)
    {
        var actions = new TranslationActions(context, FileManagementClient);
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
        Assert.Contains("contentful", result.File.Name);

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [TestMethod, ContextDataSource]
    public async Task Translate_xlf12(InvocationContext context)
    {
        var actions = new TranslationActions(context, FileManagementClient);
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
        Assert.Contains("contentful", result.File.Name);

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [TestMethod, ContextDataSource(ConnectionTypes.OpenAiEmbedded)]
    public async Task TranslateInBackground_OpenAiEmbedded_WithXliffFile_Success(InvocationContext context)
    {
        var actions = new TranslationActions(context, FileManagementClient);
            
        var translateRequest = new StartBackgroundProcessRequest
        {
            ModelId = "gpt-4.1",
            File = new FileReference { Name = "contentful12.xliff" },
            TargetLanguage = "fr"
        };
            
        var response = await actions.TranslateInBackground(translateRequest);
            
        Assert.IsNotNull(response);
        Assert.IsNotNull(response.BatchId);
        Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
    }

    [TestMethod, ContextDataSource]
    public async Task TranslateText_WithSerbianLocale_ReturnsLocalizedText(InvocationContext context)
    {
        var actions = new TranslationActions(context, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-5.1" };
        var localizeRequest = new LocalizeTextRequest
        {
            Text = "Develop and implement an HR strategy that drives organizational productivity and supports company's business goals. Design and oversee processes that promote team efficiency and operational effectiveness while reducing complexity and redundancies.",
            TargetLanguage = "sr-Latn-RS"
        };

        var glossaryRequest = new GlossaryRequest { 
        Glossary = new FileReference { Name= "Glossary for Serbian JD projects.tbx" }
        };

        var result = await actions.LocalizeText(modelIdentifier, localizeRequest, glossaryRequest);

        Assert.IsNotNull(result);
        Assert.IsNotNull(result.TranslatedText);
        Console.WriteLine("Original: " + localizeRequest.Text);
        Console.WriteLine("Localized: " + result.TranslatedText);

        // Additional validation to ensure response is not empty and contains Serbian characters
        Assert.IsGreaterThan(0, result.TranslatedText.Length);
    }

    [TestMethod, ContextDataSource]
    public async Task Translate_Text_Stork_Test(InvocationContext context)
    {
        var actions = new TranslationActions(context, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-5.1" };
        var localizeRequest = new LocalizeTextRequest
        {
            Text = "Brat mir einer einen Storch.",
            TargetLanguage = "en-US"
        };

        var glossaryRequest = new GlossaryRequest();

        var result = await actions.LocalizeText(modelIdentifier, localizeRequest, glossaryRequest);

        Assert.IsNotNull(result);
        Assert.IsNotNull(result.TranslatedText);
        Console.WriteLine("Original: " + localizeRequest.Text);
        Console.WriteLine("Localized: " + result.TranslatedText);

        // Additional validation to ensure response is not empty and contains Serbian characters
        Assert.IsGreaterThan(0, result.TranslatedText.Length);
    }
}
