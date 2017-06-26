using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public class AuthenticationOptionsCreator : IAuthenticationOptionsCreator
    {
        public AuthenticationOptions Create(FileReRoute fileReRoute)
        {
            var authenticationConfig = new AuthenticationConfigCreator().Create(fileReRoute.AuthenticationOptions);

            return new AuthenticationOptionsBuilder()
                .WithProvider(fileReRoute.AuthenticationOptions?.Provider)
                .WithAllowedScopes(fileReRoute.AuthenticationOptions?.AllowedScopes)
                .WithConfiguration(authenticationConfig)
                .Build();
        } 
    }

    public class AuthenticationConfigCreator
    {
        public IAuthenticationConfig Create(FileAuthenticationOptions authenticationOptions)
        {
            return new IdentityServerConfigBuilder()
                .WithApiName(authenticationOptions.IdentityServerConfig?.ApiName)
                .WithApiSecret(authenticationOptions.IdentityServerConfig?.ApiSecret)
                .WithProviderRootUrl(authenticationOptions.IdentityServerConfig?.ProviderRootUrl)
                .WithRequireHttps(authenticationOptions.IdentityServerConfig.RequireHttps).Build();
        }
    }
}