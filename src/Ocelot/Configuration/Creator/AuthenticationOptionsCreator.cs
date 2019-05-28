using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public class AuthenticationOptionsCreator : IAuthenticationOptionsCreator
    {
        public AuthenticationOptions Create(FileReRoute reRoute)
        {
            return new AuthenticationOptions(reRoute.AuthenticationOptions.AllowedScopes, reRoute.AuthenticationOptions.AuthenticationProviderKey);
        }
    }
}
