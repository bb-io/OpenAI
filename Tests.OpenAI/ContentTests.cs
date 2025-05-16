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
public class ContentTests : TestBase
{
    [TestMethod]
    public async Task Translate_html()
    {
        var actions = new ContentActions(InvocationContext, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4o" };
        var translateRequest = new TranslateContentRequest
        {
            File = new FileReference { Name = "contentful.html" },
            SourceLanguage = "en-US",
            TargetLanguage = "nl",
        };
        string? systemMessage = null;
        var glossaryRequest = new GlossaryRequest();

        var result = await actions.TranslateContent(modelIdentifier, translateRequest, systemMessage, glossaryRequest);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Content.Name.Contains("contentful"));

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [TestMethod]
    public async Task Translate_xliff()
    {
        var actions = new ContentActions(InvocationContext, FileManagementClient);
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
        Assert.IsTrue(result.Content.Name.Contains("contentful"));

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [TestMethod]
    public async Task Edit_xliff()
    {
        var actions = new ContentActions(InvocationContext, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4o" };
        var editRequest = new EditContentRequest
        {
            File = new FileReference { Name = "contentful.html.xliff" },
        };
        string? systemMessage = null;
        var glossaryRequest = new GlossaryRequest();

        var result = await actions.EditContent(modelIdentifier, editRequest, systemMessage, glossaryRequest);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Content.Name.Contains("contentful"));

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [TestMethod]
    public async Task Taus_edit()
    {
        var actions = new ContentActions(InvocationContext, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4.1" };
        var editRequest = new EditContentRequest
        {
            File = new FileReference { Name = "taus.xliff" },
        };
        string? systemMessage = null;
        var glossaryRequest = new GlossaryRequest();

        var result = await actions.EditContent(modelIdentifier, editRequest, systemMessage, glossaryRequest);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Content.Name.Contains("taus"));

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }
}
