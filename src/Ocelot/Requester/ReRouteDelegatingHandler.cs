using System.Net.Http;

namespace Ocelot.Requester
{
    public class ReRouteDelegatingHandler<T> 
        where T : DelegatingHandler
    {
        public T DelegatingHandler { get; private set; }
    }
}
