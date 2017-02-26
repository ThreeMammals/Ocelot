using System.Collections.Generic;
using IdentityServer4.AccessTokenValidation;
using IdentityServer4.Models;

namespace Ocelot.Configuration.Provider
{
    public class IdentityServerConfiguration : IIdentityServerConfiguration
    {
        public IdentityServerConfiguration(
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
            List<User> users)
        {
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

        public string ApiName { get; private set; }
        public bool RequireHttps { get; private set; }
        public List<string> AllowedScopes { get; private set; }
        public SupportedTokens SupportedTokens { get; private set; }
        public string ApiSecret { get; private set; }
        public string Description {get;private set;}
        public bool Enabled {get;private set;}
        public IEnumerable<string>  AllowedGrantTypes {get;private set;}
        public AccessTokenType AccessTokenType {get;private set;}
        public bool RequireClientSecret {get;private set;}
        public List<User> Users {get;private set;}
    }
}