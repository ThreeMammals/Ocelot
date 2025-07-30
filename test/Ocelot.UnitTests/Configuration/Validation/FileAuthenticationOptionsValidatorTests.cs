using FluentValidation.Results;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Validator;

namespace Ocelot.UnitTests.Configuration.Validation;

[Trait("PR", "2114")] // https://github.com/ThreeMammals/Ocelot/pull/2114
[Trait("Feat", "842")] // https://github.com/ThreeMammals/Ocelot/issues/842
public class FileAuthenticationOptionsValidatorTests : UnitTest
{
    private readonly FileAuthenticationOptionsValidator _validator;
    private readonly Mock<IAuthenticationSchemeProvider> _authProvider;
    private FileAuthenticationOptions _authenticationOptions;
    private ValidationResult _result;

    public FileAuthenticationOptionsValidatorTests()
    {
        _authProvider = new();
        _validator = new(_authProvider.Object);
    }

    [Fact]
    public async Task Should_be_valid_if_specified_authentication_provider_is_registered()
    {
        // Arrange
        const string key = "JwtLads";
        CreateAuthenticationOptions(key);
        GivenAnAuthProvider(key);

        // Act
        await ValidateAsync();

        // Assert
        _result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_not_be_valid_if_specified_authentication_provider_is_not_registered()
    {
        // Arrange
        const string key = "JwtLads";
        CreateAuthenticationOptions(key);

        // Act
        await ValidateAsync();

        // Assert
        _result.IsValid.ShouldBeFalse();
        _result.Errors[0].ErrorMessage.ShouldBe("AuthenticationOptions: AllowAnonymous:False,AllowedScopes:[],AuthenticationProviderKey:'',AuthenticationProviderKeys:['JwtLads'] is unsupported authentication provider");
    }

    private void GivenAnAuthProvider(string key)
    {
        var schemes = new List<AuthenticationScheme>
        {
            new(key, key, typeof(FakeAuthHandler)),
        };
        _authProvider.Setup(x => x.GetAllSchemesAsync())
            .ReturnsAsync(schemes);
    }

    private void CreateAuthenticationOptions(string key)
    {
        _authenticationOptions = new FileAuthenticationOptions
        {
            AuthenticationProviderKeys = [ key ],
        };
    }

    private async Task ValidateAsync()
    {
        _result = await _validator.ValidateAsync(_authenticationOptions);
    }

    private class FakeAuthHandler : IAuthenticationHandler
    {
        public Task<AuthenticateResult> AuthenticateAsync() => throw new NotImplementedException();
        public Task ChallengeAsync(AuthenticationProperties properties) => throw new NotImplementedException();
        public Task ForbidAsync(AuthenticationProperties properties) => throw new NotImplementedException();
        public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context) => throw new NotImplementedException();
    }
}
