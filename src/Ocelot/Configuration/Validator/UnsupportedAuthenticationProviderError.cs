using Ocelot.Errors;

namespace Ocelot.Configuration.Validator
{
    public class UnsupportedAuthenticationProviderError : Error
    {
        public UnsupportedAuthenticationProviderError(string message) 
            : base(message, OcelotErrorCode.UnsupportedAuthenticationProviderError)
        {
        }
    }
}
