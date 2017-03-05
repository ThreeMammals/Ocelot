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
                                        .WithScopeName(fileReRoute.AuthenticationOptions?.ScopeName)
                                        .WithRequireHttps(fileReRoute.AuthenticationOptions.RequireHttps)
                                        .WithAdditionalScopes(fileReRoute.AuthenticationOptions?.AdditionalScopes)
                                        .WithScopeSecret(fileReRoute.AuthenticationOptions?.ScopeSecret)
                                        .Build();
        }
    }
}