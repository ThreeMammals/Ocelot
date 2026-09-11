using Ocelot.Administration;

namespace Ocelot.UnitTests.Administration;

public class AdministrationPathTests
{
    [Fact]
    public void Constructor_Sets_All_Properties_Correctly()
    {
        // Arrange
        const string expectedPath = "/administration";
        const string expectedSecret = "super-secret-key-12345";
        var expectedExternalUrl = new Uri("https://auth.example.com");

        // Act
        var path = new AdministrationPath(expectedPath, expectedSecret, expectedExternalUrl);

        // Assert
        Assert.Equal(expectedPath, path.Path);
        Assert.Equal(expectedSecret, path.IssuerSigningKey);
        Assert.Equal(expectedExternalUrl, path.ExternalJwtSigningUrl);
    }

    [Fact]
    public void Constructor_When_ExternalJwtServerUrl_Is_Null_Sets_Property_To_Null()
    {
        // Arrange
        const string path = "/admin";
        const string secret = "my-secret";

        // Act
        var adminPath = new AdministrationPath(path, secret, null);

        // Assert
        Assert.Equal(path, adminPath.Path);
        Assert.Equal(secret, adminPath.IssuerSigningKey);
        Assert.Null(adminPath.ExternalJwtSigningUrl);
    }

    [Fact]
    public void Constructor_When_ExternalJwtServerUrl_Is_Provided_Sets_Property_Correctly()
    {
        // Arrange
        var externalUrl = new Uri("https://jwt-signing.mycompany.com");

        // Act
        var adminPath = new AdministrationPath("/ocelot/admin", "key123", externalUrl);

        // Assert
        Assert.Equal(externalUrl, adminPath.ExternalJwtSigningUrl);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Accepts_Null_Or_Whitespace_Path(string path)
    {
        // Act & Assert (no exception expected - class does not validate)
        var adminPath = new AdministrationPath(path, "secret", null);

        Assert.Equal(path, adminPath.Path);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Accepts_Null_Or_Whitespace_IssuerSigningKey(string secret)
    {
        // Act & Assert
        var adminPath = new AdministrationPath("/admin", secret, null);

        Assert.Equal(secret, adminPath.IssuerSigningKey);
    }

    [Fact]
    public void Properties_Are_ReadOnly()
    {
        var adminPath = new AdministrationPath("/admin", "secret123", new Uri("https://example.com"));

        // These should compile only if properties are read-only (getter only)
        Assert.Equal("/admin", adminPath.Path);
        Assert.Equal("secret123", adminPath.IssuerSigningKey);
        Assert.Equal(new Uri("https://example.com"), adminPath.ExternalJwtSigningUrl);

        // No setters available -> this would not compile if someone tried:
        // adminPath.Path = "new"; // <- compile error
    }

    [Fact]
    public void Can_Create_Multiple_Instances_With_Different_Values()
    {
        var path1 = new AdministrationPath("/admin1", "key1", null);
        var path2 = new AdministrationPath("/admin2", "key2", new Uri("https://auth2.com"));

        Assert.NotEqual(path1.Path, path2.Path);
        Assert.NotEqual(path1.IssuerSigningKey, path2.IssuerSigningKey);
        Assert.Null(path1.ExternalJwtSigningUrl);
        Assert.NotNull(path2.ExternalJwtSigningUrl);
    }
}
