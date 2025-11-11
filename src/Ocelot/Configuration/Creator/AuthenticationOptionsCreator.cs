using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public class AuthenticationOptionsCreator : IAuthenticationOptionsCreator
{
    public AuthenticationOptions Create(FileRoute route, FileGlobalConfiguration globalConfiguration)
    {
        route ??= new();
        route.AuthenticationOptions ??= new();
        globalConfiguration ??= new();
        globalConfiguration.AuthenticationOptions ??= new();
        var options = route.AuthenticationOptions.HasScheme
            ? route.AuthenticationOptions
            : globalConfiguration.AuthenticationOptions;
        return new(options);
    }

    // TODO Apply this version after removal of the AuthenticationProviderKey property
    private AuthenticationOptions Create2(FileRoute route, FileGlobalConfiguration globalConfiguration)
    {
        route ??= new();
        route.AuthenticationOptions ??= new();
        globalConfiguration ??= new();
        globalConfiguration.AuthenticationOptions ??= new();
        FileAuthenticationOptions
            opts = route.AuthenticationOptions,
            global = globalConfiguration.AuthenticationOptions;

        opts.AllowAnonymous ??= global.AllowAnonymous ?? false;
        MergeScopes(opts, global);
        MergeSchemes(opts, global);
        return new(opts);
    }

    // Merging must keep order of definition as it is stated in the docs
    protected virtual void MergeSchemes(FileAuthenticationOptions opts, FileAuthenticationOptions global)
    {
        //opts.AuthenticationProviderKey ??= global.AuthenticationProviderKey;
        if (!opts.HasScheme && global.HasScheme)
        {
            opts.AuthenticationProviderKeys = global.AuthenticationProviderKeys;
        }
    }

    protected virtual void MergeScopes(FileAuthenticationOptions opts, FileAuthenticationOptions global)
    {
        if (!opts.HasScope && global.HasScope)
        {
            opts.AllowedScopes = global.AllowedScopes;
        }
    }
}
