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
        private List<Service> _services;
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

        [Fact]
        public void should_return_error_if_selected_service_is_null()
        {
            Service invalidService = null;

            this.Given(x => x.GivenServices(new List<Service> { invalidService }))
               .And(x => x.GivenIGetTheNextAddress())
               .Then(x => x.ThenServiceAreNullErrorIsReturned())
               .BDDfy();
        }

        [Fact]
        public void should_return_error_if_host_and_port_is_null_in_the_selected_service()
        {
            var invalidService = new Service(string.Empty, null, string.Empty, string.Empty, new List<string>());

            this.Given(x => x.GivenServices(new List<Service> { invalidService }))
               .And(x => x.GivenIGetTheNextAddress())
               .Then(x => x.ThenServiceAreNullErrorIsReturned())
               .BDDfy();
        }

        private void GivenIGetTheNextAddress()
        {
            _hostAndPort = _roundRobin.Lease(_httpContext).Result;
        }

        private void GivenServices(List<Service> services)
        {
            _services = services;
        }

        private void ThenTheNextAddressIndexIs(int index)
        {
            _hostAndPort.Data.ShouldBe(_services[index].HostAndPort);
        }

        private void ThenServiceAreNullErrorIsReturned()
        {
            _hostAndPort.IsError.ShouldBeTrue();
            _hostAndPort.Errors[0].ShouldBeOfType<ServicesAreNullError>();
        }
    }
}
