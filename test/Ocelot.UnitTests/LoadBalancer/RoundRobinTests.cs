using Microsoft.AspNetCore.Http;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Responses;
using Ocelot.Values;
using System.Diagnostics;

namespace Ocelot.UnitTests.LoadBalancer;

public class RoundRobinTests : UnitTest
{
    private readonly HttpContext _httpContext;

    public RoundRobinTests()
    {
        _httpContext = new DefaultHttpContext();
    }

    [Fact]
    public void Should_get_next_address()
    {
        var services = GivenServices();
        var roundRobin = GivenLoadBalancer(services);
        WhenIGetTheNextAddress(roundRobin).Data.ShouldNotBeNull().ShouldBe(services[0].HostAndPort);
        WhenIGetTheNextAddress(roundRobin).Data.ShouldNotBeNull().ShouldBe(services[1].HostAndPort);
        WhenIGetTheNextAddress(roundRobin).Data.ShouldNotBeNull().ShouldBe(services[2].HostAndPort);
    }

    [Fact]
    [Trait("Feat", "336")]
    public void Should_go_back_to_first_address_after_finished_last()
    {
        var services = GivenServices();
        var roundRobin = GivenLoadBalancer(services);
        var stopWatch = Stopwatch.StartNew();
        while (stopWatch.ElapsedMilliseconds < 1000)
        {
            WhenIGetTheNextAddress(roundRobin).Data.ShouldNotBeNull().ShouldBe(services[0].HostAndPort);
            WhenIGetTheNextAddress(roundRobin).Data.ShouldNotBeNull().ShouldBe(services[1].HostAndPort);
            WhenIGetTheNextAddress(roundRobin).Data.ShouldNotBeNull().ShouldBe(services[2].HostAndPort);
        }
    }

    [Fact]
    [Trait("Bug", "2110")]
    public void Should_return_error_if_selected_service_is_null()
    {
        var invalidServices = new List<Service> { null };
        var roundRobin = GivenLoadBalancer(invalidServices);
        var response = WhenIGetTheNextAddress(roundRobin);
        ThenServicesAreNullErrorIsReturned(response);
    }

    [Fact]
    [Trait("Bug", "2110")]
    public void Should_return_error_if_host_and_port_is_null_in_the_selected_service()
    {
        var invalidService = new Service(string.Empty, null, string.Empty, string.Empty, new List<string>());
        var services = new List<Service> { invalidService };
        var roundRobin = GivenLoadBalancer(services);
        var response = WhenIGetTheNextAddress(roundRobin);
        ThenServicesAreNullErrorIsReturned(response);
    }

    private static List<Service> GivenServices() => new()
    {
        new("product", new ServiceHostAndPort("127.0.0.1", 5000), string.Empty, string.Empty, Array.Empty<string>()),
        new("product", new ServiceHostAndPort("127.0.0.1", 5001), string.Empty, string.Empty, Array.Empty<string>()),
        new("product", new ServiceHostAndPort("127.0.0.1", 5002), string.Empty, string.Empty, Array.Empty<string>()),
    };

    private static RoundRobin GivenLoadBalancer(List<Service> services = null)
        => new(() => Task.FromResult(services));

    private Response<ServiceHostAndPort> WhenIGetTheNextAddress(RoundRobin roundRobin)
        => roundRobin.Lease(_httpContext).Result;

    private static void ThenServicesAreNullErrorIsReturned(Response<ServiceHostAndPort> response)
    {
        response.ShouldNotBeNull().Data.ShouldBeNull();
        response.IsError.ShouldBeTrue();
        response.Errors[0].ShouldBeOfType<ServicesAreNullError>();
    }
}
