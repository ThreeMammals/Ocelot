using Ocelot.Configuration.File;
using System;

namespace Ocelot.Configuration.Creator;

public class AuthenticationOptionsCreator : IAuthenticationOptionsCreator
{
    public AuthenticationOptions Create(FileRoute route, FileGlobalConfiguration globalConfiguration)
    {
        var options = route?.AuthenticationOptions?.HasScheme == true
            ? route?.AuthenticationOptions
            : globalConfiguration?.AuthenticationOptions;
        return new(options ?? new());
    }

    // TODO Apply this version after removal of the AuthenticationProviderKey property
    private AuthenticationOptions Create2(FileRoute route, FileGlobalConfiguration globalConfiguration)
    {
        FileAuthenticationOptions opts = route.AuthenticationOptions ?? new(),
            global = globalConfiguration.AuthenticationOptions ?? new();

        // We must ignore the global option because it is purely designed for route-level use only
        //opts.AllowAnonymous ??= global.AllowAnonymous;
        opts.AllowAnonymous = opts.AllowAnonymous;

        MergeScopes(opts, global);
        MergeSchemes(opts, global);
        return new(opts);
    }

    // Merging must keep order of definition as it is stated in the docs
    protected virtual void MergeSchemes(FileAuthenticationOptions opts, FileAuthenticationOptions global)
    {
        //opts.AuthenticationProviderKey ??= global.AuthenticationProviderKey;
        if (!opts.HasScheme)
        {
            opts.AuthenticationProviderKeys = global.HasScheme ? global.AuthenticationProviderKeys : Array.Empty<string>();
        }
    }

    protected virtual void MergeScopes(FileAuthenticationOptions opts, FileAuthenticationOptions global)
    {
        if (!opts.HasScope)
        {
            opts.AllowedScopes = global.HasScope ? global.AllowedScopes : new();
        }
    }
}
