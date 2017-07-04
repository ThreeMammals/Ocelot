using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;
using Ocelot.Creator.Configuration;

namespace Ocelot.Configuration.Creator
{
    public class AuthenticationOptionsCreator : IAuthenticationOptionsCreator
    {
        private readonly IAuthenticationProviderConfigCreator _creator;

        public AuthenticationOptionsCreator(IAuthenticationProviderConfigCreator creator)
        {
            _creator = creator;
        }

        public AuthenticationOptions Create(FileReRoute fileReRoute)
        {
            var authenticationConfig = _creator.Create(fileReRoute.AuthenticationOptions);

            return new AuthenticationOptionsBuilder()
                .WithProvider(fileReRoute.AuthenticationOptions?.Provider)
                .WithAllowedScopes(fileReRoute.AuthenticationOptions?.AllowedScopes)
                .WithConfig(authenticationConfig)
                .Build();
        } 
    }
}