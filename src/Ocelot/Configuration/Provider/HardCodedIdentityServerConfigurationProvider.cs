using System;
using System.Collections.Generic;
using IdentityServer4.AccessTokenValidation;
using IdentityServer4.Models;
using IdentityServer4.Test;

namespace Ocelot.Configuration.Provider
{
    public class HardCodedIdentityServerConfigurationProvider : IIdentityServerConfigurationProvider
    {
        public IdentityServerConfiguration Get()
        {
            var url = "";
            return new IdentityServerConfiguration(
                url,
                "admin",
                false,
                SupportedTokens.Both,
                "secret",
                new List<string> {"admin", "openid", "offline_access"},
                "Ocelot Administration",
                true,
                GrantTypes.ResourceOwnerPassword,
                AccessTokenType.Jwt,
                false,
                new List<TestUser> {
                        new TestUser
                        { 
                            Username = "admin",
                            Password = "admin",
                            SubjectId = "admin",
                        }
                    }
                );
        }
    }

    public interface IIdentityServerConfigurationProvider
    {
        IdentityServerConfiguration Get();
    }

    public class IdentityServerConfiguration
    {
        public IdentityServerConfiguration(
            string identityServerUrl, 
            string apiName, 
            bool requireHttps, 
            SupportedTokens supportedTokens, 
            string apiSecret,
            List<string> allowedScopes,
            string description,
            bool enabled,
            IEnumerable<string>  grantType,
            AccessTokenType accessTokenType,
            bool requireClientSecret,
            List<TestUser> users)
        {
            IdentityServerUrl = identityServerUrl;
            ApiName = apiName;
            RequireHttps = requireHttps;
            SupportedTokens = supportedTokens;
            ApiSecret = apiSecret;
            AllowedScopes = allowedScopes;
            Description = description;
            Enabled = enabled;
            AllowedGrantTypes = grantType;
            AccessTokenType = accessTokenType;
            RequireClientSecret = requireClientSecret;
            Users = users;
        }

        public string IdentityServerUrl { get; private set; }
        public string ApiName { get; private set; }
        public bool RequireHttps { get; private set; }
        public List<string> AllowedScopes { get; private set; }
        public SupportedTokens SupportedTokens { get; private set; }
        public string ApiSecret { get; private set; }
        public string Description {get;private set;}
        public bool Enabled {get;private set;}
        public IEnumerable<string>  AllowedGrantTypes {get;private set;}
        public AccessTokenType AccessTokenType {get;private set;}
        public bool RequireClientSecret = false;
        public List<TestUser> Users {get;private set;}
    }
}