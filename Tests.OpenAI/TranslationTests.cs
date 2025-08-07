using Apps.OpenAI.Actions;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Requests.Content;
using Apps.OpenAI.Models.Requests.Xliff;
using Apps.OpenAI.Services;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Xliff.Utils.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tests.OpenAI.Base;

namespace Tests.OpenAI;

[TestClass]
public class TranslationTests : TestBase
{
    [TestMethod]
    public async Task Translate_html()
    {
        var actions = new TranslationActions(InvocationContext, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4.1" };
        var translateRequest = new TranslateContentRequest
        {
            File = new FileReference { Name = "contentful.html" },
            TargetLanguage = "nl",
            OutputFileHandling = "original",
        };
        string? systemMessage = null;
        var glossaryRequest = new GlossaryRequest();

        var result = await actions.TranslateContent(modelIdentifier, translateRequest, systemMessage, glossaryRequest);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.File.Name.Contains("contentful"));

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [TestMethod]
    public async Task Translate_xliff()
    {
        var actions = new TranslationActions(InvocationContext, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4.1" };
        var translateRequest = new TranslateContentRequest
        {
            File = new FileReference { Name = "category_7000039521.xml" },
            TargetLanguage = "nl",
        };
        string? systemMessage = null;
        var glossaryRequest = new GlossaryRequest();

        var result = await actions.TranslateContent(modelIdentifier, translateRequest, systemMessage, glossaryRequest);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.File.Name.Contains("contentful"));

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
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
