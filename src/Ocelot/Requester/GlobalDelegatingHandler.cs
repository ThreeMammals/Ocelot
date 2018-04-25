using System.Net.Http;

namespace Ocelot.Requester
{
    public class GlobalDelegatingHandler
    {
        public GlobalDelegatingHandler(DelegatingHandler delegatingHandler)
        {
            DelegatingHandler = delegatingHandler;
        }

        public DelegatingHandler DelegatingHandler { get; private set; }
    }
}
