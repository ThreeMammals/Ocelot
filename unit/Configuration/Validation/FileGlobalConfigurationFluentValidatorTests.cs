using Microsoft.AspNetCore.Authentication;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Validator;

namespace Ocelot.UnitTests.Configuration.Validation;

public class FileGlobalConfigurationFluentValidatorTests : UnitTest
{
    private readonly FileGlobalConfigurationFluentValidator _validator;
    private readonly Mock<IAuthenticationSchemeProvider> _authProvider;
    private readonly Mock<IServiceProvider> _serviceProvider;

    public FileGlobalConfigurationFluentValidatorTests()
    {
        _authProvider = new Mock<IAuthenticationSchemeProvider>();
        _serviceProvider = new Mock<IServiceProvider>();

        _validator = new FileGlobalConfigurationFluentValidator(
            new FileQoSOptionsFluentValidator(_serviceProvider.Object),
            new FileAuthenticationOptionsValidator(_authProvider.Object));
    }

    [Theory]
    [InlineData(null, true, null)]
    [InlineData(-1, false, "GlobalConfiguration.WebSocket.BufferSize is negative or zero")]
    [InlineData(0, false, "GlobalConfiguration.WebSocket.BufferSize is negative or zero")]
    [InlineData(65536, true, null)]
    public async Task ShouldValidate_WebSocketOptions_BufferSize(int? bufferSize, bool valid, string errorMessage)
    {
        // Arrange
        var configuration = new FileGlobalConfiguration
        {
            WebSocket = new() { BufferSize = bufferSize },
        };

        // Act
        var result = await _validator.ValidateAsync(configuration, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.ShouldBe(valid);
        if (!result.IsValid)
            result.ThenSingleErrorIs(errorMessage);
    }
}
