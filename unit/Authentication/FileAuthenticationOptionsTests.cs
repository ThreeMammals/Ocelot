using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Authentication;

public class FileAuthenticationOptionsTests
{
    [Fact]
    public void ToString_Serialized()
    {
        // Arrange
        FileAuthenticationOptions opts = new()
        {
            AllowAnonymous = true,
            AllowedScopes = ["2"],
            AuthenticationProviderKey = "3",
            AuthenticationProviderKeys = ["4", "5"],
        };

        // Act
        var actual = opts.ToString();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(actual));
        Assert.Equal("AllowAnonymous:True,AllowedScopes:['2'],AuthenticationProviderKey:'3',AuthenticationProviderKeys:['4','5']", actual);
    }
}
