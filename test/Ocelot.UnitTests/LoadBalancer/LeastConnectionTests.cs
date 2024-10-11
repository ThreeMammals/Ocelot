using Microsoft.AspNetCore.Http;
using Ocelot.Errors;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Responses;
using Ocelot.Values;

namespace Ocelot.UnitTests.LoadBalancer
{
    public class LeastConnectionTests : UnitTest
    {
        private ServiceHostAndPort _hostAndPort;
        private Response<ServiceHostAndPort> _result;
        private LeastConnection _leastConnection;
        private List<Service> _services;
        private readonly Random _random;
        private readonly HttpContext _httpContext;

        public LeastConnectionTests()
        {
            _httpContext = new DefaultHttpContext();
            _random = new Random();
        }

        [Fact]
        public async Task Should_be_able_to_lease_and_release_concurrently()
        {
            var serviceName = "products";

            var availableServices = new List<Service>
            {
                new(serviceName, new ServiceHostAndPort("127.0.0.1", 80), string.Empty, string.Empty, Array.Empty<string>()),
                new(serviceName, new ServiceHostAndPort("127.0.0.2", 80), string.Empty, string.Empty, Array.Empty<string>()),
            };

            _services = availableServices;
            _leastConnection = new LeastConnection(() => Task.FromResult(_services), serviceName);

            var tasks = new Task[100];

            for (var i = 0; i < tasks.Length; i++)
            {
                tasks[i] = LeaseDelayAndRelease();
            }

            await Task.WhenAll(tasks);
        }

        [Fact]
        public async Task Should_handle_service_returning_to_available()
        {
            var serviceName = "products";

            var availableServices = new List<Service>
            {
                new(serviceName, new ServiceHostAndPort("127.0.0.1", 80), string.Empty, string.Empty, Array.Empty<string>()),
                new(serviceName, new ServiceHostAndPort("127.0.0.2", 80), string.Empty, string.Empty, Array.Empty<string>()),
            };

            _leastConnection = new LeastConnection(() => Task.FromResult(availableServices), serviceName);

            var hostAndPortOne = await _leastConnection.LeaseAsync(_httpContext);
            hostAndPortOne.Data.DownstreamHost.ShouldBe("127.0.0.1");
            var hostAndPortTwo = await _leastConnection.LeaseAsync(_httpContext);
            hostAndPortTwo.Data.DownstreamHost.ShouldBe("127.0.0.2");
            _leastConnection.Release(hostAndPortOne.Data);
            _leastConnection.Release(hostAndPortTwo.Data);

            availableServices = new List<Service>
            {
                new(serviceName, new ServiceHostAndPort("127.0.0.1", 80), string.Empty, string.Empty, Array.Empty<string>()),
            };

            hostAndPortOne = await _leastConnection.LeaseAsync(_httpContext);
            hostAndPortOne.Data.DownstreamHost.ShouldBe("127.0.0.1");
            hostAndPortTwo = await _leastConnection.LeaseAsync(_httpContext);
            hostAndPortTwo.Data.DownstreamHost.ShouldBe("127.0.0.1");
            _leastConnection.Release(hostAndPortOne.Data);
            _leastConnection.Release(hostAndPortTwo.Data);

            availableServices = new List<Service>
            {
                new(serviceName, new ServiceHostAndPort("127.0.0.1", 80), string.Empty, string.Empty, Array.Empty<string>()),
                new(serviceName, new ServiceHostAndPort("127.0.0.2", 80), string.Empty, string.Empty, Array.Empty<string>()),
            };

            hostAndPortOne = await _leastConnection.LeaseAsync(_httpContext);
            hostAndPortOne.Data.DownstreamHost.ShouldBe("127.0.0.1");
            hostAndPortTwo = await _leastConnection.LeaseAsync(_httpContext);
            hostAndPortTwo.Data.DownstreamHost.ShouldBe("127.0.0.2");
            _leastConnection.Release(hostAndPortOne.Data);
            _leastConnection.Release(hostAndPortTwo.Data);
        }

        private async Task LeaseDelayAndRelease()
        {
            var hostAndPort = await _leastConnection.LeaseAsync(_httpContext);
            await Task.Delay(_random.Next(1, 100));
            _leastConnection.Release(hostAndPort.Data);
        }

        [Fact]
        public void Should_get_next_url()
        {
            var serviceName = "products";

            var hostAndPort = new ServiceHostAndPort("localhost", 80);

            var availableServices = new List<Service>
            {
                new(serviceName, hostAndPort, string.Empty, string.Empty, Array.Empty<string>()),
            };

            this.Given(x => x.GivenAHostAndPort(hostAndPort))
            .And(x => x.GivenTheLoadBalancerStarts(availableServices, serviceName))
            .When(x => x.WhenIGetTheNextHostAndPort())
            .Then(x => x.ThenTheNextHostAndPortIsReturned())
            .BDDfy();
        }

        [Fact]
        public async Task Should_serve_from_service_with_least_connections()
        {
            var serviceName = "products";

            var availableServices = new List<Service>
            {
                new(serviceName, new ServiceHostAndPort("127.0.0.1", 80), string.Empty, string.Empty, Array.Empty<string>()),
                new(serviceName, new ServiceHostAndPort("127.0.0.2", 80), string.Empty, string.Empty, Array.Empty<string>()),
                new(serviceName, new ServiceHostAndPort("127.0.0.3", 80), string.Empty, string.Empty, Array.Empty<string>()),
            };

            _services = availableServices;
            _leastConnection = new LeastConnection(() => Task.FromResult(_services), serviceName);

            var response = await _leastConnection.LeaseAsync(_httpContext);

            response.Data.DownstreamHost.ShouldBe(availableServices[0].HostAndPort.DownstreamHost);

            response = await _leastConnection.LeaseAsync(_httpContext);

            response.Data.DownstreamHost.ShouldBe(availableServices[1].HostAndPort.DownstreamHost);

            response = await _leastConnection.LeaseAsync(_httpContext);

            response.Data.DownstreamHost.ShouldBe(availableServices[2].HostAndPort.DownstreamHost);
        }

        [Fact]
        public async Task Should_build_connections_per_service()
        {
            var serviceName = "products";

            var availableServices = new List<Service>
            {
                new(serviceName, new ServiceHostAndPort("127.0.0.1", 80), string.Empty, string.Empty, Array.Empty<string>()),
                new(serviceName, new ServiceHostAndPort("127.0.0.2", 80), string.Empty, string.Empty, Array.Empty<string>()),
            };

            _services = availableServices;
            _leastConnection = new LeastConnection(() => Task.FromResult(_services), serviceName);

            var response = await _leastConnection.LeaseAsync(_httpContext);

            response.Data.DownstreamHost.ShouldBe(availableServices[0].HostAndPort.DownstreamHost);

            response = await _leastConnection.LeaseAsync(_httpContext);

            response.Data.DownstreamHost.ShouldBe(availableServices[1].HostAndPort.DownstreamHost);

            response = await _leastConnection.LeaseAsync(_httpContext);

            response.Data.DownstreamHost.ShouldBe(availableServices[0].HostAndPort.DownstreamHost);

            response = await _leastConnection.LeaseAsync(_httpContext);

            response.Data.DownstreamHost.ShouldBe(availableServices[1].HostAndPort.DownstreamHost);
        }

        [Fact]
        public async Task Should_release_connection()
        {
            var serviceName = "products";

            var availableServices = new List<Service>
            {
                new(serviceName, new ServiceHostAndPort("127.0.0.1", 80), string.Empty, string.Empty, Array.Empty<string>()),
                new(serviceName, new ServiceHostAndPort("127.0.0.2", 80), string.Empty, string.Empty, Array.Empty<string>()),
            };

            _services = availableServices;
            _leastConnection = new LeastConnection(() => Task.FromResult(_services), serviceName);

            var response = await _leastConnection.LeaseAsync(_httpContext);

            response.Data.DownstreamHost.ShouldBe(availableServices[0].HostAndPort.DownstreamHost);

            response = await _leastConnection.LeaseAsync(_httpContext);

            response.Data.DownstreamHost.ShouldBe(availableServices[1].HostAndPort.DownstreamHost);

            response = await _leastConnection.LeaseAsync(_httpContext);

            response.Data.DownstreamHost.ShouldBe(availableServices[0].HostAndPort.DownstreamHost);

            response = await _leastConnection.LeaseAsync(_httpContext);

            response.Data.DownstreamHost.ShouldBe(availableServices[1].HostAndPort.DownstreamHost);

            //release this so 2 should have 1 connection and we should get 2 back as our next host and port
            _leastConnection.Release(availableServices[1].HostAndPort);

            response = await _leastConnection.LeaseAsync(_httpContext);

            response.Data.DownstreamHost.ShouldBe(availableServices[1].HostAndPort.DownstreamHost);
        }

        [Fact]
        public void Should_return_error_if_services_are_null()
        {
            var serviceName = "products";

            var hostAndPort = new ServiceHostAndPort("localhost", 80);
            this.Given(x => x.GivenAHostAndPort(hostAndPort))
             .And(x => x.GivenTheLoadBalancerStarts(null, serviceName))
             .When(x => x.WhenIGetTheNextHostAndPort())
             .Then(x => x.ThenErrorIsReturned<ServicesAreNullError>())
             .BDDfy();
        }

        [Fact]
        public void Should_return_error_if_services_are_empty()
        {
            var serviceName = "products";

            var hostAndPort = new ServiceHostAndPort("localhost", 80);
            this.Given(x => x.GivenAHostAndPort(hostAndPort))
             .And(x => x.GivenTheLoadBalancerStarts(new List<Service>(), serviceName))
             .When(x => x.WhenIGetTheNextHostAndPort())
             .Then(x => x.ThenErrorIsReturned<ServicesAreNullError>())
             .BDDfy();
        }

        private void ThenErrorIsReturned<TError>()
            where TError : Error
        {
            _result.IsError.ShouldBeTrue();
            _result.Errors[0].ShouldBeOfType<TError>();
        }

        private void GivenTheLoadBalancerStarts(List<Service> services, string serviceName)
        {
            _services = services;
            _leastConnection = new LeastConnection(() => Task.FromResult(_services), serviceName);
        }

        private void GivenAHostAndPort(ServiceHostAndPort hostAndPort)
        {
            _hostAndPort = hostAndPort;
        }

        private async Task WhenIGetTheNextHostAndPort()
        {
            _result = await _leastConnection.LeaseAsync(_httpContext);
        }

        private void ThenTheNextHostAndPortIsReturned()
        {
            _result.Data.DownstreamHost.ShouldBe(_hostAndPort.DownstreamHost);
            _result.Data.DownstreamPort.ShouldBe(_hostAndPort.DownstreamPort);
        }
    }
}
