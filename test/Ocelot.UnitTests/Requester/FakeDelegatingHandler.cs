using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Ocelot.UnitTests.Requester
{
    public class FakeDelegatingHandler : DelegatingHandler
    {
        public FakeDelegatingHandler()
        {

        }

        public FakeDelegatingHandler(int order)
        {
            Order = order;
        }
        public int Order {get;private set;}
        public DateTime TimeCalled {get;private set;}

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            TimeCalled = DateTime.Now;
            return new HttpResponseMessage();
        }
    }
}