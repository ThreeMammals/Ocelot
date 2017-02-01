using System;
using System.Collections.Generic;
using System.Linq;
using Ocelot.Errors;
using Ocelot.Responses;
using Ocelot.Values;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests
{
    public class LeastConnectionTests
    {
        private HostAndPort _hostAndPort;
        private Response<HostAndPort> _result;
        private LeastConnectionLoadBalancer _leastConnection;
        private List<Service> _services;

        public LeastConnectionTests()
        {
        }

        [Fact]
        public void should_get_next_url()
        {
            var serviceName = "products";

            var hostAndPort = new HostAndPort("localhost", 80);

            var availableServices = new List<Service>
            {
                new Service(serviceName, hostAndPort)
            };

            this.Given(x => x.GivenAHostAndPort(hostAndPort))
            .And(x => x.GivenTheLoadBalancerStarts(availableServices, serviceName))
            .When(x => x.WhenIGetTheNextHostAndPort())
            .Then(x => x.ThenTheNextHostAndPortIsReturned())
            .BDDfy();
        }

        [Fact]
        public void should_serve_from_service_with_least_connections()
        {
            var serviceName = "products";

            var availableServices = new List<Service>
            {
                new Service(serviceName, new HostAndPort("127.0.0.1", 80)),
                new Service(serviceName, new HostAndPort("127.0.0.2", 80)),
                new Service(serviceName, new HostAndPort("127.0.0.3", 80))
            };

            _services = availableServices;
            _leastConnection = new LeastConnectionLoadBalancer(() => _services, serviceName);

            var response = _leastConnection.Lease();

            response.Data.DownstreamHost.ShouldBe(availableServices[0].HostAndPort.DownstreamHost);

            response = _leastConnection.Lease();

            response.Data.DownstreamHost.ShouldBe(availableServices[1].HostAndPort.DownstreamHost);

            response = _leastConnection.Lease();

            response.Data.DownstreamHost.ShouldBe(availableServices[2].HostAndPort.DownstreamHost);
        }

        [Fact]
        public void should_build_connections_per_service()
        {
             var serviceName = "products";

            var availableServices = new List<Service>
            {
                new Service(serviceName, new HostAndPort("127.0.0.1", 80)),
                new Service(serviceName, new HostAndPort("127.0.0.2", 80)),
            };

            _services = availableServices;
            _leastConnection = new LeastConnectionLoadBalancer(() => _services, serviceName);

            var response = _leastConnection.Lease();

            response.Data.DownstreamHost.ShouldBe(availableServices[0].HostAndPort.DownstreamHost);

            response = _leastConnection.Lease();

            response.Data.DownstreamHost.ShouldBe(availableServices[1].HostAndPort.DownstreamHost);

            response = _leastConnection.Lease();

            response.Data.DownstreamHost.ShouldBe(availableServices[0].HostAndPort.DownstreamHost);

            response = _leastConnection.Lease();

            response.Data.DownstreamHost.ShouldBe(availableServices[1].HostAndPort.DownstreamHost);
        }

        [Fact]
        public void should_release_connection()
        {
             var serviceName = "products";

            var availableServices = new List<Service>
            {
                new Service(serviceName, new HostAndPort("127.0.0.1", 80)),
                new Service(serviceName, new HostAndPort("127.0.0.2", 80)),
            };

            _services = availableServices;
            _leastConnection = new LeastConnectionLoadBalancer(() => _services, serviceName);

            var response = _leastConnection.Lease();

            response.Data.DownstreamHost.ShouldBe(availableServices[0].HostAndPort.DownstreamHost);

            response = _leastConnection.Lease();

            response.Data.DownstreamHost.ShouldBe(availableServices[1].HostAndPort.DownstreamHost);

            response = _leastConnection.Lease();

            response.Data.DownstreamHost.ShouldBe(availableServices[0].HostAndPort.DownstreamHost);

            response = _leastConnection.Lease();

            response.Data.DownstreamHost.ShouldBe(availableServices[1].HostAndPort.DownstreamHost);

            //release this so 2 should have 1 connection and we should get 2 back as our next host and port
            _leastConnection.Release(availableServices[1].HostAndPort);

            response = _leastConnection.Lease();

            response.Data.DownstreamHost.ShouldBe(availableServices[1].HostAndPort.DownstreamHost);
        }

        [Fact]
        public void should_return_error_if_services_are_null()
        {
            var serviceName = "products";

            var hostAndPort = new HostAndPort("localhost", 80);
               this.Given(x => x.GivenAHostAndPort(hostAndPort))
                .And(x => x.GivenTheLoadBalancerStarts(null, serviceName))
                .When(x => x.WhenIGetTheNextHostAndPort())
                .Then(x => x.ThenServiceAreNullErrorIsReturned())
                .BDDfy();
        }

        [Fact]
        public void should_return_error_if_services_are_empty()
        {
            var serviceName = "products";

            var hostAndPort = new HostAndPort("localhost", 80);
               this.Given(x => x.GivenAHostAndPort(hostAndPort))
                .And(x => x.GivenTheLoadBalancerStarts(new List<Service>(), serviceName))
                .When(x => x.WhenIGetTheNextHostAndPort())
                .Then(x => x.ThenServiceAreEmptyErrorIsReturned())
                .BDDfy();
        }

        private void ThenServiceAreNullErrorIsReturned()
        {
            _result.IsError.ShouldBeTrue();
            _result.Errors[0].ShouldBeOfType<ServicesAreNullError>();
        }

        private void ThenServiceAreEmptyErrorIsReturned()
        {
            _result.IsError.ShouldBeTrue();
            _result.Errors[0].ShouldBeOfType<ServicesAreEmptyError>();
        }

        private void GivenTheLoadBalancerStarts(List<Service> services, string serviceName)
        {
            _services = services;
            _leastConnection = new LeastConnectionLoadBalancer(() => _services, serviceName);
        }

        private void WhenTheLoadBalancerStarts(List<Service> services, string serviceName)
        {
            GivenTheLoadBalancerStarts(services, serviceName);
        }

        private void GivenAHostAndPort(HostAndPort hostAndPort)
        {
            _hostAndPort = hostAndPort;
        }

        private void WhenIGetTheNextHostAndPort()
        {
            _result = _leastConnection.Lease();
        }

        private void ThenTheNextHostAndPortIsReturned()
        {
            _result.Data.DownstreamHost.ShouldBe(_hostAndPort.DownstreamHost);
            _result.Data.DownstreamPort.ShouldBe(_hostAndPort.DownstreamPort);
        }
    }

    public class LeastConnectionLoadBalancer : ILoadBalancer
    {
        private Func<List<Service>> _services;
        private List<Lease> _leases;
        private string _serviceName;

        public LeastConnectionLoadBalancer(Func<List<Service>> services, string serviceName)
        {
            _services = services;
            _serviceName = serviceName;
            _leases = new List<Lease>();
        }

        public Response<HostAndPort> Lease()
        {
            var services = _services();

            if(services == null)
            {
                return new ErrorResponse<HostAndPort>(new List<Error>(){ new ServicesAreNullError($"services were null for {_serviceName}")});
            }

            if(!services.Any())
            {
                return new ErrorResponse<HostAndPort>(new List<Error>(){ new ServicesAreEmptyError($"services were empty for {_serviceName}")});
            }

            //todo - maybe this should be moved somewhere else...? Maybe on a repeater on seperate thread? loop every second and update or something?
            UpdateServices(services);

            var leaseWithLeastConnections = GetLeaseWithLeastConnections();
            
            _leases.Remove(leaseWithLeastConnections);

            leaseWithLeastConnections = AddConnection(leaseWithLeastConnections);

            _leases.Add(leaseWithLeastConnections);

            return new OkResponse<HostAndPort>(new HostAndPort(leaseWithLeastConnections.HostAndPort.DownstreamHost, leaseWithLeastConnections.HostAndPort.DownstreamPort));
        }

        public Response Release(HostAndPort hostAndPort)
        {
            var matchingLease = _leases.FirstOrDefault(l => l.HostAndPort.DownstreamHost == hostAndPort.DownstreamHost 
                && l.HostAndPort.DownstreamPort == hostAndPort.DownstreamPort);

            if(matchingLease != null)
            {
                var replacementLease = new Lease(hostAndPort, matchingLease.Connections - 1);

                _leases.Remove(matchingLease);

                _leases.Add(replacementLease);
            }

            return new OkResponse();
        }

        private Lease AddConnection(Lease lease)
        {
            return new Lease(lease.HostAndPort, lease.Connections + 1);
        }

        private Lease GetLeaseWithLeastConnections()
        {
            //now get the service with the least connections?
            Lease leaseWithLeastConnections = null;

            for(var i = 0; i < _leases.Count; i++)
            {
                if(i == 0)
                {
                    leaseWithLeastConnections = _leases[i];
                }
                else
                {
                    if(_leases[i].Connections < leaseWithLeastConnections.Connections)
                    {
                        leaseWithLeastConnections = _leases[i];
                    }
                }
            }

            return leaseWithLeastConnections;
        }

        private Response UpdateServices(List<Service> services)
        { 
            if(_leases.Count > 0)
            {
                var leasesToRemove = new List<Lease>();

                foreach(var lease in _leases)
                {
                    var match = services.FirstOrDefault(s => s.HostAndPort.DownstreamHost == lease.HostAndPort.DownstreamHost
                        && s.HostAndPort.DownstreamPort == lease.HostAndPort.DownstreamPort);

                    if(match == null)
                    {
                        leasesToRemove.Add(lease);
                    }
                }

                foreach(var lease in leasesToRemove)
                {
                    _leases.Remove(lease);
                }

                foreach(var service in services)
                {
                    var exists = _leases.FirstOrDefault(l => l.HostAndPort.ToString() == service.HostAndPort.ToString());

                    if(exists == null)
                    {
                        _leases.Add(new Lease(service.HostAndPort, 0));
                    }
                }
            }
            else
            {
                foreach(var service in services)
                {
                    _leases.Add(new Lease(service.HostAndPort, 0));
                }
            }

            return new OkResponse();
        }
    }

    public class Lease
    {
        public Lease(HostAndPort hostAndPort, int connections)
        {
            HostAndPort = hostAndPort;
            Connections = connections;
        }
        public HostAndPort HostAndPort {get;private set;}
        public int Connections {get;private set;}
    }

    public class ServicesAreNullError : Error
    {
        public ServicesAreNullError(string message) 
        : base(message, OcelotErrorCode.ServicesAreNullError)
        {
        }
    }

    public class ServicesAreEmptyError : Error
    {
        public ServicesAreEmptyError(string message) 
        : base(message, OcelotErrorCode.ServicesAreEmptyError)
        {
        }
    }
}