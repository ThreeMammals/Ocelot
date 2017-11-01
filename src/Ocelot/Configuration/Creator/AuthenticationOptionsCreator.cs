using System.Collections.Generic;
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

        public AuthenticationOptions Create(FileReRoute reRoute, List<FileAuthenticationOptions> authOptions)
        {
            //todo - loop is crap..
            foreach(var authOption in authOptions)
            {
                if(reRoute.AuthenticationProviderKey == authOption.AuthenticationProviderKey)
                {
                    var authenticationConfig = _creator.Create(authOption);

                    return new AuthenticationOptionsBuilder()
                        .WithProvider(authOption.Provider)
                        .WithAllowedScopes(authOption.AllowedScopes)
                        .WithConfig(authenticationConfig)
                        .Build();
                }
            }

            //todo - should not return null?
            return null;
        } 
    }
}