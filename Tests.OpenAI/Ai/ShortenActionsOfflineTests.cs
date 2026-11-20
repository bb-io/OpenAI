using System.Xml.Linq;
using Apps.OpenAI.Actions;
using Apps.OpenAI.Ai;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Content;
using Blackbird.Ai.Llm.Abstract;
using Blackbird.Ai.Llm.Models;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Newtonsoft.Json.Linq;

namespace Tests.OpenAI.Ai;

[TestClass]
public class ShortenActionsOfflineTests
{
    private const string RestrictedFile = """
        <xliff version="2.0" srcLang="en" trgLang="de" xmlns="urn:oasis:names:tc:xliff:document:2.0"
               xmlns:slr="urn:oasis:names:tc:xliff:sizerestriction:2.0">
          <file id="f1" original="restricted.xlf">
            <unit id="u1" slr:sizeRestriction="5">
              <segment id="s1" state="translated"><source>Brief</source><target>Sehr kurz</target></segment>
            </unit>
          </file>
        </xliff>
        """;

    private readonly InMemoryFileManagementClient _files = new();
    private readonly TextChatModelIdentifier _model = new() { ModelId = "gpt-4o" };

    [TestMethod]
    public async Task ShortenContent_FileRestrictionOverridesFallbackAndUploadsShortenedTarget()
    {
        var file = _files.Add("restricted.xlf", RestrictedFile);
        var client = new ScriptedClient("Kurz");

        var result = await new ScriptedShortenActions(_files, client).ShortenContent(
            _model, new ShortenContentRequest { File = file, MaximumLength = 15 });

        Assert.AreEqual(5, client.Limits.Single());
        Assert.AreEqual(1, result.UnitsWithLimit);
        Assert.AreEqual(1, result.UnitsOverLimit);
        Assert.AreEqual(1, result.UnitsShortened);
        Assert.AreEqual(0, result.UnitsStillOverLimit);
        var xliff = XDocument.Parse(_files.Read(result.File));
        var segment = xliff.Descendants(xliff.Root!.Name.Namespace + "segment").Single();
        Assert.AreEqual("Kurz", segment.Element(segment.Name.Namespace + "target")?.Value);
        Assert.AreEqual("translated", (string?)segment.Attribute("state"));
    }

    [TestMethod]
    public async Task ShortenContent_TargetAlreadyFits_CompletesWithoutResolvingModel()
    {
        var file = _files.Add("fits.xlf", TestXliff.Translated("en", "de", ("Brief", "Kurz")));
        var context = new InvocationContext
        {
            AuthenticationCredentialsProviders =
            [
                new AuthenticationCredentialsProvider(CredNames.ConnectionType, ConnectionTypes.OpenAiEmbedded),
                new AuthenticationCredentialsProvider(CredNames.ApiKey, "unused")
            ]
        };

        var result = await new ShortenActions(context, _files).ShortenContent(
            new TextChatModelIdentifier(), new ShortenContentRequest { File = file, MaximumLength = 4 });

        Assert.AreEqual(1, result.UnitsWithLimit);
        Assert.AreEqual(0, result.UnitsOverLimit);
        Assert.AreEqual(0, result.Usage.TotalTokens);
    }

    [TestMethod]
    public async Task ShortenText_FirstAnswerTooLong_ReturnsRetriedAnswerAndUsage()
    {
        var client = new ScriptedClient("Still too long", "Brief");

        var result = await new ScriptedShortenActions(_files, client).ShortenText(
            _model, new ShortenTextRequest { Text = "Make this much shorter", MaximumLength = 6, Retries = 1 });

        Assert.AreEqual("Brief", result.ShortenedText);
        CollectionAssert.AreEqual(new[] { 6, 5 }, client.Limits);
        Assert.AreEqual(20, result.Usage.PromptTokens);
        Assert.AreEqual(10, result.Usage.CompletionTokens);
    }

    [TestMethod]
    public async Task ShortenText_NoRetriesAndAnswerTooLong_Throws()
    {
        var client = new ScriptedClient("Still too long");
        var actions = new ScriptedShortenActions(_files, client);

        await Assert.ThrowsExactlyAsync<PluginApplicationException>(() => actions.ShortenText(
            _model, new ShortenTextRequest { Text = "Make this much shorter", MaximumLength = 5, Retries = 0 }));

        Assert.AreEqual(5, client.Limits.Single());
    }

    private sealed class ScriptedShortenActions(InMemoryFileManagementClient files, ILlmClient client)
        : ShortenActions(new InvocationContext
        {
            AuthenticationCredentialsProviders =
            [
                new AuthenticationCredentialsProvider(CredNames.ConnectionType, ConnectionTypes.OpenAi),
                new AuthenticationCredentialsProvider(CredNames.ApiKey, "unused")
            ]
        }, files)
    {
        protected override ILlmClient CreateLlmClient(string? model, OpenAiLlmSettings settings) => client;
    }

    private sealed class ScriptedClient(params string[] answers) : ILlmClient
    {
        private readonly Queue<string> _answers = new(answers);

        public List<int> Limits { get; } = [];

        public LlmCapabilities Capabilities { get; } = new(
            SupportsStructuredOutput: true, ContextTokens: 128_000, MaxOutputTokens: 16_384);

        public Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken cancellationToken)
        {
            var start = request.UserPrompt.IndexOf('[');
            var end = request.UserPrompt.LastIndexOf(']');
            var item = JArray.Parse(request.UserPrompt[start..(end + 1)]).Single();
            Limits.Add(item.Value<int>("max_length"));

            var response = new JObject
            {
                ["translations"] = new JArray(new JObject
                {
                    ["translation_id"] = item.Value<string>("id"),
                    ["translated_text"] = _answers.Dequeue()
                })
            };
            return Task.FromResult(LlmResponse.Completed(response.ToString(), new LlmUsage(10, 5)));
        }
    }
}
