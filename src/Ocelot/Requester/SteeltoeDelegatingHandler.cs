namespace Ocelot.Requester
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Pivotal.Discovery.Client;

    public class SteeltoeDelegatingHandler : DelegatingHandler
    {
        private readonly IDiscoveryClient _discoveryClient;

        public SteeltoeDelegatingHandler(IDiscoveryClient discoveryClient)
        {
            _discoveryClient = discoveryClient;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var handler = new DiscoveryHttpClientHandler(_discoveryClient);

            InnerHandler = handler;

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
