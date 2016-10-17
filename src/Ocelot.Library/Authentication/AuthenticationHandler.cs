namespace Ocelot.Library.Authentication
{
    using Microsoft.AspNetCore.Http;

    public class AuthenticationHandler
    {
        public AuthenticationHandler(string provider, RequestDelegate handler)
        {
            Provider = provider;
            Handler = handler;
        }

        public string Provider { get; private set; }
        public RequestDelegate Handler { get; private set; }
    }
}