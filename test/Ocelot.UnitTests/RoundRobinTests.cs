using System.Collections.Generic;
using System.Diagnostics;
using Ocelot.Responses;
using Ocelot.Values;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests
{
    public class RoundRobinTests
    {
        private readonly RoundRobinLoadBalancer _roundRobin;
        private readonly List<Service> _services;
        private Response<HostAndPort> _hostAndPort;

        public RoundRobinTests()
        {
            _services = new List<Service>
            {
                new Service("product", new HostAndPort("127.0.0.1", 5000)),
                new Service("product", new HostAndPort("127.0.0.1", 5001)),
                new Service("product", new HostAndPort("127.0.0.1", 5001))
            };

            _roundRobin = new RoundRobinLoadBalancer(_services);
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
        public void should_go_back_to_first_address_after_finished_last()
        {
            var stopWatch = Stopwatch.StartNew();

            while (stopWatch.ElapsedMilliseconds < 1000)
            {
                var address = _roundRobin.Lease();
                address.Data.ShouldBe(_services[0].HostAndPort);
                address = _roundRobin.Lease();
                address.Data.ShouldBe(_services[1].HostAndPort);
                address = _roundRobin.Lease();
                address.Data.ShouldBe(_services[2].HostAndPort);
            }
        }

        private void GivenIGetTheNextAddress()
        {
            _hostAndPort = _roundRobin.Lease();
        }

        private void ThenTheNextAddressIndexIs(int index)
        {
            _hostAndPort.Data.ShouldBe(_services[index].HostAndPort);
        }
    }

    public interface ILoadBalancer
    {
        Response<HostAndPort> Lease();
        Response Release(HostAndPort hostAndPort);
    }

    public class RoundRobinLoadBalancer : ILoadBalancer
    {
        private readonly List<Service> _services;
        private int _last;

        public RoundRobinLoadBalancer(List<Service> services)
        {
            _services = services;
        }

        public Response<HostAndPort> Lease()
        {
            if (_last >= _services.Count)
            {
                _last = 0;
            }

            var next = _services[_last];
            _last++;
            return new OkResponse<HostAndPort>(next.HostAndPort);
        }

        public Response Release(HostAndPort hostAndPort)
        {
            return new OkResponse();
        }
    }
}
