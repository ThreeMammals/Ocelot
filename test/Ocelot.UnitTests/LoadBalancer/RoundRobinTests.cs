using Microsoft.AspNetCore.Http;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Responses;
using Ocelot.Values;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ocelot.UnitTests.LoadBalancer;

public class RoundRobinTests : UnitTest
{
    private readonly HttpContext _httpContext;

    public RoundRobinTests()
    {
        _httpContext = new DefaultHttpContext();
    }

    [Fact]
    public async Task Lease_LoopThroughIndexRangeOnce_ShouldGetNextAddress()
    {
        var services = GivenServices();
        var roundRobin = GivenLoadBalancer(services);

        var response0 = await WhenIGetTheNextAddressAsync(roundRobin);
        var response1 = await WhenIGetTheNextAddressAsync(roundRobin);
        var response2 = await WhenIGetTheNextAddressAsync(roundRobin);

        response0.Data.ShouldNotBeNull().ShouldBe(services[0].HostAndPort);
        response1.Data.ShouldNotBeNull().ShouldBe(services[1].HostAndPort);
        response2.Data.ShouldNotBeNull().ShouldBe(services[2].HostAndPort);
    }

    [Fact]
    [Trait("Feat", "336")]
    public async Task Lease_LoopThroughIndexRangeIndefinitelyButOneSecond_ShouldGoBackToFirstAddressAfterFinishedLast()
    {
        var services = GivenServices();
        var roundRobin = GivenLoadBalancer(services);
        var stopWatch = Stopwatch.StartNew();
        while (stopWatch.ElapsedMilliseconds < 1000)
        {
            var response0 = await WhenIGetTheNextAddressAsync(roundRobin);
            var response1 = await WhenIGetTheNextAddressAsync(roundRobin);
            var response2 = await WhenIGetTheNextAddressAsync(roundRobin);

            response0.Data.ShouldNotBeNull().ShouldBe(services[0].HostAndPort);
            response1.Data.ShouldNotBeNull().ShouldBe(services[1].HostAndPort);
            response2.Data.ShouldNotBeNull().ShouldBe(services[2].HostAndPort);
        }
    }

    [Fact]
    [Trait("Bug", "2110")]
    public async Task Lease_SelectedServiceIsNull_ShouldReturnError()
    {
        var invalidServices = new List<Service> { null };
        var roundRobin = GivenLoadBalancer(invalidServices);
        var response = await WhenIGetTheNextAddressAsync(roundRobin);
        ThenServicesAreNullErrorIsReturned(response);
    }

    //[InlineData(1, 10)]
    //[InlineData(2, 50)]
    //[InlineData(3, 50)]
    //[InlineData(4, 50)]
    //[InlineData(5, 50)]
    //[InlineData(3, 100)]
    //[InlineData(4, 100)]
    //[InlineData(7, 100)]
    [InlineData(3, 100)]
    [Theory]
    [Trait("Feat", "2110")]
    public void Lease_LoopThroughIndexRangeIndefinitelyUnderHighLoad_ShouldDistributeIndexValuesUniformly(int totalServices, int totalThreads)
    {
        // Arrange
        const bool ReturnServicesNotImmediately = false;
        var services = GivenServices(totalServices);
        var roundRobin = GivenLoadBalancer(services, ReturnServicesNotImmediately);
        int bottom = totalThreads / totalServices,
            top = totalThreads - (bottom * totalServices) + bottom;

        // Act
        var responses = WhenICallLeaseFromMultipleThreads(roundRobin, totalThreads);
        var counters = CountServices(services, responses);

        // Assert
        responses.ShouldNotBeNull();
        responses.Length.ShouldBe(totalThreads);

        var message = $"All values are [{string.Join(',', counters)}]";
        counters.Sum().ShouldBe(totalThreads, message);

        message = $"{nameof(bottom)}: {bottom}\n\t{nameof(top)}: {top}\n\tAll values are [{string.Join(',', counters)}]";
        counters.ShouldAllBe(counter => bottom <= counter && counter <= top, message);
    }

    private static int[] CountServices(List<Service> services, Response<ServiceHostAndPort>[] responses)
    {
        var counters = new int[services.Count];
        var firstPort = services[0].HostAndPort.DownstreamPort;
        foreach (var response in responses)
        {
            var idx = response.Data.DownstreamPort - firstPort;
            counters[idx]++;
        }

        return counters;
    }

    private Response<ServiceHostAndPort>[] WhenICallLeaseFromMultipleThreads(RoundRobin roundRobin, int times)
    {
        var tasks = new Task[times]; // allocate N-times threads as Task
        var parallelResponses = new Response<ServiceHostAndPort>[times];
        for (var i = 0; i < times; i++)
        {
            tasks[i] = GetParallelResponse(parallelResponses, roundRobin, i);
        }

        Task.WaitAll(tasks); // load by N-times threads
        return parallelResponses;
    }

    private async Task GetParallelResponse(Response<ServiceHostAndPort>[] responses, RoundRobin roundRobin, int threadIndex)
    {
        responses[threadIndex] = await WhenIGetTheNextAddressAsync(roundRobin);
    }

    private static List<Service> GivenServices(int total = 3, [CallerMemberName] string serviceName = null)
    {
        var list = new List<Service>(total);
        for (int i = 1; i <= total; i++)
        {
            list.Add(new(serviceName, new ServiceHostAndPort("127.0.0." + i, 5000 + i), string.Empty, string.Empty, Array.Empty<string>()));
        }

        return list;
    }

    private static RoundRobin GivenLoadBalancer(List<Service> services, bool immediately = true, [CallerMemberName] string serviceName = null)
    {
        return new(
            () =>
            {
                int leasingDelay = immediately ? 0 : Random.Shared.Next(5, 15);
                Thread.Sleep(leasingDelay);
                return Task.FromResult(services);
            },
            serviceName);
    }

    private Task<Response<ServiceHostAndPort>> WhenIGetTheNextAddressAsync(RoundRobin roundRobin)
        => roundRobin.LeaseAsync(_httpContext);

    private static void ThenServicesAreNullErrorIsReturned(Response<ServiceHostAndPort> response)
    {
        response.ShouldNotBeNull().Data.ShouldBeNull();
        response.IsError.ShouldBeTrue();
        response.Errors[0].ShouldBeOfType<ServicesAreNullError>();
    }
}
