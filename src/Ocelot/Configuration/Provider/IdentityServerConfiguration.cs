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
            string apiSecret,
            List<string> allowedScopes,
            string credentialsSigningCertificateLocation, 
            string credentialsSigningCertificatePassword)
        {
            ApiName = apiName;
            RequireHttps = requireHttps;
            ApiSecret = apiSecret;
            AllowedScopes = allowedScopes;
            CredentialsSigningCertificateLocation = credentialsSigningCertificateLocation;
            CredentialsSigningCertificatePassword = credentialsSigningCertificatePassword;
        }

        public string ApiName { get; private set; }
        public bool RequireHttps { get; private set; }
        public List<string> AllowedScopes { get; private set; }
        public string ApiSecret { get; private set; }
        public string CredentialsSigningCertificateLocation { get; private set; }
        public string CredentialsSigningCertificatePassword { get; private set; }
    }
}