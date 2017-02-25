using System;
using System.Collections.Generic;
using IdentityServer4.AccessTokenValidation;
using IdentityServer4.Models;
using Ocelot.Configuration.Provider;

namespace Ocelot.Configuration.Creator
{
    public static class IdentityServerConfigurationCreator
    {
        public static IdentityServerConfiguration GetIdentityServerConfiguration()
        {
            var username = Environment.GetEnvironmentVariable("OCELOT_USERNAME");
            var hash = Environment.GetEnvironmentVariable("OCELOT_HASH");
            var salt = Environment.GetEnvironmentVariable("OCELOT_SALT");

            return new IdentityServerConfiguration(
                "admin",
                false,
                SupportedTokens.Both,
                "secret",
                new List<string> { "admin", "openid", "offline_access" },
                "Ocelot Administration",
                true,
                GrantTypes.ResourceOwnerPassword,
                AccessTokenType.Jwt,
                false,
                new List<User>
                {
                    new User("admin", username, hash, salt)
                }
            );
        }
    }
}
