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
        private readonly RoundRobin _roundRobin;
        private readonly List<HostAndPort> _hostAndPorts;
        private Response<HostAndPort> _hostAndPort;

        public RoundRobinTests()
        {
            _hostAndPorts = new List<HostAndPort>
            {
                new HostAndPort("127.0.0.1", 5000),
                new HostAndPort("127.0.0.1", 5001),
                new HostAndPort("127.0.0.1", 5001)
            };

            _roundRobin = new RoundRobin(_hostAndPorts);
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
                address.Data.ShouldBe(_hostAndPorts[0]);
                address = _roundRobin.Lease();
                address.Data.ShouldBe(_hostAndPorts[1]);
                address = _roundRobin.Lease();
                address.Data.ShouldBe(_hostAndPorts[2]);
            }
        }

        private void GivenIGetTheNextAddress()
        {
            _hostAndPort = _roundRobin.Lease();
        }

        private void ThenTheNextAddressIndexIs(int index)
        {
            _hostAndPort.Data.ShouldBe(_hostAndPorts[index]);
        }
    }

    public interface ILoadBalancer
    {
        Response<HostAndPort> Lease();
        Response Release(HostAndPort hostAndPort);
    }

    public class RoundRobin : ILoadBalancer
    {
        private readonly List<HostAndPort> _hostAndPorts;
        private int _last;

        public RoundRobin(List<HostAndPort> hostAndPorts)
        {
            _hostAndPorts = hostAndPorts;
        }

        public Response<HostAndPort> Lease()
        {
            if (_last >= _hostAndPorts.Count)
            {
                _last = 0;
            }

            var next = _hostAndPorts[_last];
            _last++;
            return new OkResponse<HostAndPort>(next);
        }

        public Response Release(HostAndPort hostAndPort)
        {
            return new OkResponse();
        }
    }
}
