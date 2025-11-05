using Tests.OpenAI.Base;
using Apps.OpenAI.Connections;
using Blackbird.Applications.Sdk.Common.Authentication;

namespace Tests.OpenAI;

[TestClass]
public class ConnectionValidatorTests : TestBase
{
    [TestMethod]
    public async Task ValidateConnection_WithCorrectCredentials_ReturnsValidResult()
    {
        var validator = new ConnectionValidator();

        var tasks = CredentialGroups.Select(x => validator.ValidateConnection(x, CancellationToken.None).AsTask());
        var results = await Task.WhenAll(tasks);
        Assert.IsTrue(results.All(x => x.IsValid));
    }

    [TestMethod]
    public async Task ValidateConnection_WithIncorrectCredentials_ReturnsInvalidResult()
    {
        // Arrange
        var validator = new ConnectionValidator();
        var newCreds = CredentialGroups.First().Select(x => new AuthenticationCredentialsProvider(x.KeyName, x.Value + "_incorrect"));

        // Act
        var ex = await Assert.ThrowsExactlyAsync<Exception>(async () =>
            await validator.ValidateConnection(newCreds, CancellationToken.None)
        );

        // Assert
        StringAssert.Contains(ex.Message, "Unsupported connection type");
    }
}