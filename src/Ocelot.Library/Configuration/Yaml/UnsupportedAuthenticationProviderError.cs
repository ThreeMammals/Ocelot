namespace Ocelot.Library.Configuration.Yaml
{
    using Errors;

    public class UnsupportedAuthenticationProviderError : Error
    {
        public UnsupportedAuthenticationProviderError(string message) 
            : base(message, OcelotErrorCode.UnsupportedAuthenticationProviderError)
        {
        }
    }
}
