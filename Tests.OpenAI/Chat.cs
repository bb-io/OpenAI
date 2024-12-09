using Apps.OpenAI.Actions;
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

namespace Tests.OpenAI
{
    [TestClass]
    public class Chat : TestBase
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
    }
}
