using System.Collections.Generic;
using IdentityServer4.AccessTokenValidation;
using IdentityServer4.Models;

namespace Ocelot.Configuration.Provider
{
    public interface IIdentityServerConfiguration
    {
        string ApiName { get;  }
        bool RequireHttps { get;  }
        List<string> AllowedScopes { get;  }
        SupportedTokens SupportedTokens { get;  }
        string ApiSecret { get;  }
        string Description {get;}
        bool Enabled {get;}
        IEnumerable<string>  AllowedGrantTypes {get;}
        AccessTokenType AccessTokenType {get;}
        bool RequireClientSecret {get;}
        List<User> Users {get;}
        string CredentialsSigningCertificateLocation { get; }
        string CredentialsSigningCertificatePassword { get; }
    }
}