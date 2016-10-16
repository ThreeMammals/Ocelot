using Ocelot.Library.Infrastructure.Errors;

namespace Ocelot.Library.Infrastructure.Configuration.Yaml
{
    public class UnsupportedAuthenticationProviderError : Error
    {
        public UnsupportedAuthenticationProviderError(string message) 
            : base(message, OcelotErrorCode.UnsupportedAuthenticationProviderError)
        {
        }
    }
}
