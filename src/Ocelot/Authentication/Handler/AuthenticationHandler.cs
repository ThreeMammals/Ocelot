namespace Ocelot.Authentication.Handler
{
    public class AuthenticationHandler
    {
        public AuthenticationHandler(string provider, IHandler handler)
        {
            Provider = provider;
            Handler = handler;
        }

        public string Provider { get; private set; }
        public IHandler Handler { get; private set; }
    }
}