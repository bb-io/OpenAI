﻿using Apps.OpenAI.Actions;
using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Tests.OpenAI.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apps.OpenAI.Models.Requests.Xliff;

namespace Tests.OpenAI
{
    [TestClass]
    public class ChatTests : TestBase
    {

        [TestMethod]
        public async Task ChatReturnsAResponse()
        {
            var actions = new ChatActions(InvocationContext, FileManager);
            var result = await actions.ChatMessageRequest(
                new TextChatModelIdentifier { ModelId = "gpt-4o" },
                new ChatRequest { Message = "Hello!" },
                new GlossaryRequest { });

            Console.WriteLine(result.Message);

            Assert.IsNotNull(result.Message);
        }
        [TestMethod]
        public async Task ChatFromAudioReturnsAResponse()
        {
            var actions = new ChatActions(InvocationContext, FileManager);

            var result = await actions.ChatMessageRequest(
                new TextChatModelIdentifier { ModelId = "gpt-4o" },
                new ChatRequest { Message = "Answer to the audio file!",
                File = new Blackbird.Applications.Sdk.Common.Files.FileReference() { Name = "tts delorean.mp3", ContentType = "audio/mp3" }},
                new GlossaryRequest { });

            Console.WriteLine(result.Message);

            Assert.IsNotNull(result.Message);
        }

        [TestMethod]
       public async Task PostEditXliffResponse()
        {
            var actions = new ChatActions(InvocationContext, FileManager);
            var input1 = new TextChatModelIdentifier { ModelId= "gpt-4o-mini" };
            var input2 = new PostEditXliffRequest {File = new Blackbird.Applications.Sdk.Common.Files.FileReference { Name= "test.xlf" } };
            string? input3 = null;

            var input4 = new GlossaryRequest { };
            var result = await actions.PostEditXLIFF(input1, input2, input3, input4);
        }

        [TestMethod]
        public async Task ProcessXliffResponse()
        {
            var actions = new ChatActions(InvocationContext, FileManager);
            var input1 = new TextChatModelIdentifier { ModelId = "gpt-4o-mini", };
            var input2 = new TranslateXliffRequest { File = new Blackbird.Applications.Sdk.Common.Files.FileReference { Name = "test.tmx" } };
            //string? input3 = " If the content includes any URLs that start with \"https://remote.com/\" please insert the locale domain \"en-ph\" before the rest of the URL. For example, if you get \"https://remote.com/global-hr/hris-software\", change it to \"https://remote.com/en-ph/global-hr/hris-software\" ";
            string? input3 = null;
            var input4 = new GlossaryRequest { };
            var result = await actions.TranslateXliff(input1, input2, input3, input4,30);
        }
    }
}
