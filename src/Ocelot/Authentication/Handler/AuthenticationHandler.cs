using Microsoft.AspNetCore.Http;

namespace Ocelot.Authentication.Handler
{
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