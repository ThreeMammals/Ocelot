namespace Ocelot.Configuration.Creator
{
    using Ocelot.Configuration.Builder;
    using Ocelot.Configuration.File;

    public class ConfigCreator
    {
        public IAuthenticationConfig Create(FileAuthenticationOptions authenticationOptions)
        {
            if (authenticationOptions.Provider == "Jwt")
            {
                return CreateJwt(authenticationOptions);
            }

            return CreateIdentityServer(authenticationOptions);
        }

        private JwtConfig CreateJwt(FileAuthenticationOptions authenticationOptions)
        {
            return new JwtConfigBuilder()
                .WithAudience(authenticationOptions.JwtConfig?.Audience)
                .WithAuthority(authenticationOptions.JwtConfig?.Authority)
                .Build();
        }

        private IdentityServerConfig CreateIdentityServer(FileAuthenticationOptions authenticationOptions)
        {
            return new IdentityServerConfigBuilder()
                .WithApiName(authenticationOptions.IdentityServerConfig?.ApiName)
                .WithApiSecret(authenticationOptions.IdentityServerConfig?.ApiSecret)
                .WithProviderRootUrl(authenticationOptions.IdentityServerConfig?.ProviderRootUrl)
                .WithRequireHttps(authenticationOptions.IdentityServerConfig.RequireHttps).Build();
        }
    }
}