using Microsoft.AspNetCore.Http;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Responses;
using Ocelot.Values;

namespace Ocelot.UnitTests.LoadBalancer;

public class NoLoadBalancerTests : UnitTest
{
    private readonly List<Service> _services;
    private NoLoadBalancer _loadBalancer;
    private Response<ServiceHostAndPort> _result;

    public NoLoadBalancerTests()
    {
        _services = new List<Service>();
        _loadBalancer = new NoLoadBalancer(() => Task.FromResult(_services));
    }

    [Fact]
    public async Task Should_return_host_and_port()
    {
        var hostAndPort = new ServiceHostAndPort("127.0.0.1", 80);
        var services = new List<Service>
        {
            new("product", hostAndPort, string.Empty, string.Empty, Array.Empty<string>()),
        };
        GivenServices(services);
        await WhenIGetTheNextHostAndPort();
        ThenTheHostAndPortIs(hostAndPort);
    }

    [Fact]
    public async Task Should_return_error_if_no_services()
    {
        await WhenIGetTheNextHostAndPort();
        ThenThereIsAnError();
    }

    [Fact]
    public async Task Should_return_error_if_no_services_then_when_services_available_return_host_and_port()
    {
        var hostAndPort = new ServiceHostAndPort("127.0.0.1", 80);

        var services = new List<Service>
        {
            new("product", hostAndPort, string.Empty, string.Empty, Array.Empty<string>()),
        };

        await WhenIGetTheNextHostAndPort();
        ThenThereIsAnError();
        GivenServices(services);
        await WhenIGetTheNextHostAndPort();
        ThenTheHostAndPortIs(hostAndPort);
    }

    [Fact]
    public async Task Should_return_error_if_null_services()
    {
        GivenServicesAreNull();
        await WhenIGetTheNextHostAndPort();
        ThenThereIsAnError();
    }

    private void GivenServicesAreNull()
    {
        _loadBalancer = new NoLoadBalancer(() => Task.FromResult((List<Service>)null));
    }

    private void ThenThereIsAnError()
    {
        _result.IsError.ShouldBeTrue();
    }

    private void GivenServices(List<Service> services)
    {
        _services.AddRange(services);
    }

    private async Task WhenIGetTheNextHostAndPort()
    {
        _result = await _loadBalancer.LeaseAsync(new DefaultHttpContext());
    }

    private void ThenTheHostAndPortIs(ServiceHostAndPort expected)
    {
        _result.Data.ShouldBe(expected);
    }
}
