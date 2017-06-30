using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public class AuthenticationOptionsCreator : IAuthenticationOptionsCreator
    {
        public AuthenticationOptions Create(FileReRoute fileReRoute)
        {
            var authenticationConfig = new ConfigCreator().Create(fileReRoute.AuthenticationOptions);

            return new AuthenticationOptionsBuilder()
                .WithProvider(fileReRoute.AuthenticationOptions?.Provider)
                .WithAllowedScopes(fileReRoute.AuthenticationOptions?.AllowedScopes)
                .WithConfig(authenticationConfig)
                .Build();
        } 
    }
}