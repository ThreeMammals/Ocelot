using Microsoft.AspNetCore.Http;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Responses;
using Ocelot.Values;
using System.Diagnostics;

namespace Ocelot.UnitTests.LoadBalancer
{
    public class RoundRobinTests : UnitTest
    {
        private readonly RoundRobin _roundRobin;
        private readonly List<Service> _services;
        private Response<ServiceHostAndPort> _hostAndPort;
        private readonly HttpContext _httpContext;

        public RoundRobinTests()
        {
            _httpContext = new DefaultHttpContext();
            _services = new List<Service>
            {
                new("product", new ServiceHostAndPort("127.0.0.1", 5000), string.Empty, string.Empty, Array.Empty<string>()),
                new("product", new ServiceHostAndPort("127.0.0.1", 5001), string.Empty, string.Empty, Array.Empty<string>()),
                new("product", new ServiceHostAndPort("127.0.0.1", 5001), string.Empty, string.Empty, Array.Empty<string>()),
            };

            _roundRobin = new RoundRobin(() => Task.FromResult(_services));
        }

        [Fact]
        public void should_get_next_address()
        {
            this.Given(x => x.GivenIGetTheNextAddress())
                .Then(x => x.ThenTheNextAddressIndexIs(0))
                .Given(x => x.GivenIGetTheNextAddress())
                .Then(x => x.ThenTheNextAddressIndexIs(1))
                .Given(x => x.GivenIGetTheNextAddress())
                .Then(x => x.ThenTheNextAddressIndexIs(2))
                .BDDfy();
        }

        [Fact]
        public async Task should_go_back_to_first_address_after_finished_last()
        {
            var stopWatch = Stopwatch.StartNew();

            while (stopWatch.ElapsedMilliseconds < 1000)
            {
                var address = await _roundRobin.Lease(_httpContext);
                address.Data.ShouldBe(_services[0].HostAndPort);
                address = await _roundRobin.Lease(_httpContext);
                address.Data.ShouldBe(_services[1].HostAndPort);
                address = await _roundRobin.Lease(_httpContext);
                address.Data.ShouldBe(_services[2].HostAndPort);
            }
        }

        private void GivenIGetTheNextAddress()
        {
            _hostAndPort = _roundRobin.Lease(_httpContext).Result;
        }

        private void ThenTheNextAddressIndexIs(int index)
        {
            _hostAndPort.Data.ShouldBe(_services[index].HostAndPort);
        }
    }
}
