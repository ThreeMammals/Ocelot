using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public class AuthenticationOptionsCreator : IAuthenticationOptionsCreator
    {
        public AuthenticationOptions Create(FileReRoute fileReRoute)
        {
            return new AuthenticationOptionsBuilder()
                                        .WithProvider(fileReRoute.AuthenticationOptions?.Provider)
                                        .WithProviderRootUrl(fileReRoute.AuthenticationOptions?.ProviderRootUrl)
                                        .WithApiName(fileReRoute.AuthenticationOptions?.ApiName)
                                        .WithRequireHttps(fileReRoute.AuthenticationOptions.RequireHttps)
                                        .WithAllowedScopes(fileReRoute.AuthenticationOptions?.AllowedScopes)
                                        .WithApiSecret(fileReRoute.AuthenticationOptions?.ApiSecret)
                                        .Build();
        }
    }
}