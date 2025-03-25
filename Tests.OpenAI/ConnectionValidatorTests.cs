using Apps.OpenAI.Connections;
using Blackbird.Applications.Sdk.Common.Authentication;
using Tests.OpenAI.Base;

namespace Tests.OpenAI;

[TestClass]
public class ConnectionValidatorTests : TestBase
{
    [TestMethod]
    public async Task ValidateConnection_WithCorrectCredentials_ReturnsValidResult()
    {
        var validator = new ConnectionValidator();

        var result = await validator.ValidateConnection(Creds, CancellationToken.None);
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public async Task ValidateConnection_WithIncorrectCredentials_ReturnsInvalidResult()
    {
        var validator = new ConnectionValidator();

        var newCreds = Creds.Select(x => new AuthenticationCredentialsProvider(x.KeyName, x.Value + "_incorrect"));
        var result = await validator.ValidateConnection(newCreds, CancellationToken.None);
        Assert.IsFalse(result.IsValid);
    }
}