using Apps.OpenAI.Connections;
using Apps.OpenAI.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Newtonsoft.Json.Linq;
using OpenAITests.Base;

namespace OpenAITests
{
    [TestClass]
    public class Validator : TestBase
    {
        [TestMethod]
        public async Task ValidatesCorrectConnection()
        {
            var validator = new ConnectionValidator();

            var result = await validator.ValidateConnection(Creds, CancellationToken.None);
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public async Task DoesNotValidateIncorrectConnection()
        {
            var validator = new ConnectionValidator();

            var newCreds = Creds.Select(x => new AuthenticationCredentialsProvider(x.KeyName, x.Value + "_incorrect"));
            var result = await validator.ValidateConnection(newCreds, CancellationToken.None);
            Assert.IsFalse(result.IsValid);
        }
    }
}