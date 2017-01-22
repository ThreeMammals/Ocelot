using System.Collections.Generic;
using System.Diagnostics;
using Ocelot.Values;
using Shouldly;
using Xunit;

namespace Ocelot.UnitTests
{
    public class RoundRobinTests
    {
        private readonly RoundRobin _roundRobin;
        private readonly List<HostAndPort> _hostAndPorts;

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
            var address = _roundRobin.Next();
            address.ShouldBe(_hostAndPorts[0]);
            address = _roundRobin.Next();
            address.ShouldBe(_hostAndPorts[1]);
            address = _roundRobin.Next();
            address.ShouldBe(_hostAndPorts[2]);
        }

        [Fact]
        public void should_go_back_to_first_address_after_finished_last()
        {
            var stopWatch = Stopwatch.StartNew();

            while (stopWatch.ElapsedMilliseconds < 1000)
            {
                var address = _roundRobin.Next();
                address.ShouldBe(_hostAndPorts[0]);
                address = _roundRobin.Next();
                address.ShouldBe(_hostAndPorts[1]);
                address = _roundRobin.Next();
                address.ShouldBe(_hostAndPorts[2]);
            }
        }
    }

    public interface ILoadBalancer
    {
        HostAndPort Next();
    }

    public class RoundRobin : ILoadBalancer
    {
        private readonly List<HostAndPort> _hostAndPorts;
        private int _last;

        public RoundRobin(List<HostAndPort> hostAndPorts)
        {
            _hostAndPorts = hostAndPorts;
        }

        public HostAndPort Next()
        {
            if (_last >= _hostAndPorts.Count)
            {
                _last = 0;
            }

            var next = _hostAndPorts[_last];
            _last++;
            return next;
        }
    }
}
