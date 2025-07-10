using Apps.OpenAI.Actions;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Tests.OpenAI.Base;
using Apps.OpenAI.Models.Requests.Xliff;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Newtonsoft.Json;

namespace Tests.OpenAI;

[TestClass]
public class XliffActionTests : TestBase
{   

    [TestMethod]
    public async Task PostEditXLIFF_WithValidXlfFile_ProcessesSuccessfully()
    {
        var actions = new DeprecatedXliffActions(InvocationContext, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4.1" };
        var prompt = "You are a Swiss machine translation post-editor. You edit texts from German into Italian. Your task is to post-edit a translation. You need to take the source segment into account when post-editing the translation. Check each target segment and perform the following tasks:\r\n- Replace the character \"'\" with the character \"’\". Example: write \"l’indice\" instead of \"l'indice\".\r\n- Make sure that the quotation marks \"«»\" are used in the target text. Example: write «I prezzi aumentano» instead of \"I prezzi aumentano\".\r\n- Make sure percentages are expressed with the symbol \"%\". Make sure there aren't spaces between the number and the symbol. Example: write \"10%\" instead of “10 per cento”.\r\n- Perform a grammar and punctuation check. Focus on spelling, gender and number. Ensure internal consistency and fluency. Try to use impersonal formulations when possible.\r\n- Perform a terminology check. The attached glossary contains the terms that need to be strictly applied to the translation, ensuring grammatical correctness, gender, inflections and plurals.\r\nReturn the new .xliff file. ";
        
        var editRequest = new PostEditXliffRequest 
        {
            DisableTagChecks=true,
            File = new Blackbird.Applications.Sdk.Common.Files.FileReference { Name = "670470817.xliff" } ,
            SourceLanguage = "German",
            TargetLanguage = "Italian",
            
        };
        string? systemMessage = prompt;
        var glossaryRequest = new GlossaryRequest { Glossary=new Blackbird.Applications.Sdk.Common.Files.FileReference { Name="CAS LTAI.tbx" } };
        
        var result = await actions.PostEditXLIFF(modelIdentifier, editRequest, systemMessage, glossaryRequest);
        Assert.IsNotNull(result);
        //Assert.IsTrue(result.File.Name.Contains("test"));

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [TestMethod]
    public async Task TranslateXliff_WithValidTmxFile_ThrowMisconfigurationError()
    {
        var actions = new DeprecatedXliffActions(InvocationContext, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4o-mini" };
        var translateRequest = new TranslateXliffRequest 
        { 
            File = new Blackbird.Applications.Sdk.Common.Files.FileReference { Name = "test.tmx" } 
        };
        string? systemMessage = null;
        var glossaryRequest = new GlossaryRequest();
        
        await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(async () =>
        {
            await actions.TranslateXliff(modelIdentifier, translateRequest, systemMessage, glossaryRequest);
        });
    }

    [TestMethod]
    public async Task TranslateXliff_WithTxlfFile_ProcessesSuccessfully()
    {
        var actions = new DeprecatedXliffActions(InvocationContext, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4o-mini" };
        var translateRequest = new TranslateXliffRequest 
        { 
            File = new Blackbird.Applications.Sdk.Common.Files.FileReference { Name = "Markdown entry #1_en-US-Default_HTML-nl-NL#TR_FQTF#.html.txlf" } 
        };
        string? systemMessage = null;
        var glossaryRequest = new GlossaryRequest();
        
        var result = await actions.TranslateXliff(modelIdentifier, translateRequest, systemMessage, glossaryRequest);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.File.Name.Contains("Markdown entry"));

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    
    [TestMethod]
    public async Task PostEditXLIFF_WithTxlfFile_ProcessesSuccessfully()
    {
        var actions = new DeprecatedXliffActions(InvocationContext, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4o-mini" };
        var editRequest = new PostEditXliffRequest 
        { 
            File = new Blackbird.Applications.Sdk.Common.Files.FileReference { Name = "test.xlf" } 
        };
        string? systemMessage = null;
        var glossaryRequest = new GlossaryRequest()
        {
            Glossary = new Blackbird.Applications.Sdk.Common.Files.FileReference { Name = "glossary.tbx" } 
        };
        
        var result = await actions.PostEditXLIFF(modelIdentifier, editRequest, systemMessage, glossaryRequest);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.File.Name.Contains("test.xlf"));

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }
    
    [TestMethod]
    public async Task ScoreXLIFF_WithValidTxlfFile_ProcessesSuccessfully()
    {
        var actions = new DeprecatedXliffActions(InvocationContext, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4o-mini" };
        var scoreRequest = new ScoreXliffRequest 
        { 
            File = new Blackbird.Applications.Sdk.Common.Files.FileReference { Name = "test.xlf" },
            Threshold = new []{ 8.0},
            Condition = new []{">="},
            State = new []{"needs-adaptation"}
        };
        
        var result = await actions.ScoreXLIFF(modelIdentifier, scoreRequest, null, 1500);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.File.Name.Contains("test"));

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }
}
