using Ocelot.Library.Errors;

namespace Ocelot.Library.Configuration.Validator
{
    public class UnsupportedAuthenticationProviderError : Error
    {
        public UnsupportedAuthenticationProviderError(string message) 
            : base(message, OcelotErrorCode.UnsupportedAuthenticationProviderError)
        {
        }
    }
}
