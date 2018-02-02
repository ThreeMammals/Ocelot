using System.Collections.Generic;
using IdentityServer4.AccessTokenValidation;
using IdentityServer4.Models;

namespace Ocelot.Configuration.Provider
{
    public interface IIdentityServerConfiguration
    {
        string ApiName { get;  }
        string ApiSecret { get;  }
        bool RequireHttps { get;  }
        List<string> AllowedScopes { get;  }
        string CredentialsSigningCertificateLocation { get; }
        string CredentialsSigningCertificatePassword { get; }
    }
}