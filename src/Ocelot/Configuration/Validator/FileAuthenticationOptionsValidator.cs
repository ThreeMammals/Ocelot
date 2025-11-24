using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Ocelot.Configuration.File;
using Ocelot.Infrastructure.Extensions;

namespace Ocelot.Configuration.Validator;

public class FileAuthenticationOptionsValidator : AbstractValidator<FileAuthenticationOptions>
{
    private readonly IAuthenticationSchemeProvider _authenticationSchemeProvider;

    public FileAuthenticationOptionsValidator(IAuthenticationSchemeProvider authenticationSchemeProvider)
    {
        _authenticationSchemeProvider = authenticationSchemeProvider;

        RuleFor(authOptions => authOptions)
            .MustAsync(IsSupportedAuthenticationProviders)
            .WithMessage($"{nameof(FileRoute.AuthenticationOptions)}: {{PropertyValue}} is unsupported authentication provider");
    }

    private async Task<bool> IsSupportedAuthenticationProviders(FileAuthenticationOptions options, CancellationToken cancellationToken)
    {
        var keys = options.AuthenticationProviderKeys;
        if (options.AuthenticationProviderKey.IsEmpty() && (keys is null || keys.Length == 0))
        {
            return true;
        }

        var schemes = await _authenticationSchemeProvider.GetAllSchemesAsync();
        var supportedSchemes = schemes.Select(scheme => scheme.Name);
        var primary = options.AuthenticationProviderKey;
        return !string.IsNullOrWhiteSpace(primary) && supportedSchemes.Contains(primary)
            || (string.IsNullOrWhiteSpace(primary) && keys.All(supportedSchemes.Contains));
    }
}
