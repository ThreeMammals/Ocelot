using System.Collections.Generic;
using System.Diagnostics;
using Ocelot.Responses;
using Ocelot.Values;
using Shouldly;
using TestStack.BDDfy;
using Xunit;
using System;
using System.Linq;

namespace Ocelot.UnitTests
{
    public class RoundRobinTests
    {
        private readonly RoundRobin _roundRobin;
        private readonly List<Service> _hostAndPorts;
        private Response<HostAndPort> _hostAndPort;

        public RoundRobinTests()
        {
            _hostAndPorts = new List<Service>
            {
                new Service(Guid.NewGuid().ToString(), "one", "1.0.0", null,new HostAndPort("127.0.0.1", 5000)),
                new Service(Guid.NewGuid().ToString(), "one", "1.0.0", null,new HostAndPort("127.0.0.1", 5001)),
                new Service(Guid.NewGuid().ToString(), "one", "1.0.0", null,new HostAndPort("127.0.0.1", 5002))
            };

            _roundRobin = new RoundRobin();
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
                var address = _roundRobin.Lease(_hostAndPorts);
                address.Data.ShouldBe(_hostAndPorts[0].HostAndPort);
                address = _roundRobin.Lease(_hostAndPorts);
                address.Data.ShouldBe(_hostAndPorts[1].HostAndPort);
                address = _roundRobin.Lease(_hostAndPorts);
                address.Data.ShouldBe(_hostAndPorts[2].HostAndPort);
            }
        }

        private void GivenIGetTheNextAddress()
        {
            _hostAndPort = _roundRobin.Lease(_hostAndPorts);
        }

        private void ThenTheNextAddressIndexIs(int index)
        {
            _hostAndPort.Data.ShouldBe(_hostAndPorts[index].HostAndPort);
        }
    }

    public interface ILoadBalancer
    {
        Response<HostAndPort> Lease(IList<Service> instances);

        Response Release(HostAndPort hostAndPort);
    }

    public class RoundRobin : ILoadBalancer
    {

        public RoundRobin()
        {
            _discriminator = (x, y) => x?.HostAndPort == y?.HostAndPort;
        }

        private int _last;
        private readonly Func<Service, Service, bool> _discriminator;
        private Service Previous;

        /// <summary>
        /// select next instance
        /// </summary>
        /// <param name="instances"></param>
        /// <returns></returns>
        public Response<HostAndPort> Lease(IList<Service> instances)
        {
            var next = Choose(Previous, instances);
            Previous = next;
            return new OkResponse<HostAndPort>(next.HostAndPort); 
        }

        public Service Choose(Service previous, IList<Service> instances)
        {
            int previousIndex = instances.IndexOf(previous);

            var next = instances.Skip(previousIndex < 0 ? 0 : previousIndex + 1)
                .FirstOrDefault(x => _discriminator(x, previous) == false) ?? instances.FirstOrDefault();

            Previous = next;

            return next;
        }


        //public Response<HostAndPort> Lease()
        //{
        //    if (_last >= _hostAndPorts.Count)
        //    {
        //        _last = 0;
        //    }

        //    var next = _hostAndPorts[_last];
        //    _last++;
        //    return new OkResponse<HostAndPort>(next);
        //}

        public Response Release(HostAndPort hostAndPort)
        {
            return new OkResponse();
        }
    }
}
