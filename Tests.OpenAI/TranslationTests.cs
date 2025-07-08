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
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4o" };
        var translateRequest = new TranslateContentRequest
        {
            File = new FileReference { Name = "contentful.html" },
            SourceLanguage = "en-US",
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
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4o" };
        var translateRequest = new TranslateContentRequest
        {
            File = new FileReference { Name = "contentful.html.xliff" },
            SourceLanguage = "en-US",
            TargetLanguage = "nl",
        };
        string? systemMessage = null;
        var glossaryRequest = new GlossaryRequest();

        var result = await actions.TranslateContent(modelIdentifier, translateRequest, systemMessage, glossaryRequest);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.File.Name.Contains("contentful"));

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }
}
