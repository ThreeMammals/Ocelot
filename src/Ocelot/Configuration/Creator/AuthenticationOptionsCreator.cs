using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public class AuthenticationOptionsCreator : IAuthenticationOptionsCreator
{
    public AuthenticationOptions Create(FileRoute route, FileGlobalConfiguration global)
    {
        var finalOptions = route?.AuthenticationOptions?.HasScheme != true
            ? global?.AuthenticationOptions
            : route.AuthenticationOptions;
        return new(finalOptions ?? new());
    }
}
