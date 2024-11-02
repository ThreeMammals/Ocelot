using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Validator;

public class FileAuthenticationOptionsValidator : AbstractValidator<FileAuthenticationOptions>
{
    private readonly IAuthenticationSchemeProvider _authenticationSchemeProvider;

    public FileAuthenticationOptionsValidator(IAuthenticationSchemeProvider authenticationSchemeProvider)
    {
        _authenticationSchemeProvider = authenticationSchemeProvider;

        RuleFor(authOptions => authOptions)
            .MustAsync(IsSupportedAuthenticationProviders)
            .WithMessage("AuthenticationOptions: {PropertyValue} is unsupported authentication provider");
    }

    private async Task<bool> IsSupportedAuthenticationProviders(FileAuthenticationOptions options, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(options.AuthenticationProviderKey) && options.AuthenticationProviderKeys.Length == 0)
        {
            return true;
        }

        var schemes = await _authenticationSchemeProvider.GetAllSchemesAsync();
        var supportedSchemes = schemes.Select(scheme => scheme.Name);
        var primary = options.AuthenticationProviderKey;
        return !string.IsNullOrEmpty(primary) && supportedSchemes.Contains(primary)
            || (string.IsNullOrEmpty(primary) && options.AuthenticationProviderKeys.All(supportedSchemes.Contains));
    }
}
