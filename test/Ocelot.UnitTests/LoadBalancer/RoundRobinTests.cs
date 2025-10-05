using Microsoft.AspNetCore.Http;
using Ocelot.LoadBalancer;
using Ocelot.LoadBalancer.Balancers;
using Ocelot.LoadBalancer.Errors;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Ocelot.UnitTests.LoadBalancer;

public class RoundRobinTests : UnitTest
{
    private readonly DefaultHttpContext _httpContext = new();

    [Fact]
    public async Task Lease_LoopThroughIndexRangeOnce_ShouldGetNextAddress()
    {
        // Arrange
        var services = GivenServices();
        var roundRobin = GivenLoadBalancer(services);

        // Act
        var response0 = await roundRobin.LeaseAsync(_httpContext);
        var response1 = await roundRobin.LeaseAsync(_httpContext);
        var response2 = await roundRobin.LeaseAsync(_httpContext);

        // Assert
        response0.Data.ShouldNotBeNull().ShouldBe(services[0].HostAndPort);
        response1.Data.ShouldNotBeNull().ShouldBe(services[1].HostAndPort);
        response2.Data.ShouldNotBeNull().ShouldBe(services[2].HostAndPort);
    }

    [Fact]
    [Trait("Feat", "336")]
    public async Task Lease_LoopThroughIndexRangeIndefinitelyButOneSecond_ShouldGoBackToFirstAddressAfterFinishedLast()
    {
        // Arrange
        var services = GivenServices();
        var roundRobin = GivenLoadBalancer(services);
        var stopWatch = Stopwatch.StartNew();
        while (stopWatch.ElapsedMilliseconds < 1000)
        {
            // Act
            var response0 = await roundRobin.LeaseAsync(_httpContext);
            var response1 = await roundRobin.LeaseAsync(_httpContext);
            var response2 = await roundRobin.LeaseAsync(_httpContext);

            // Assert
            response0.Data.ShouldNotBeNull().ShouldBe(services[0].HostAndPort);
            response1.Data.ShouldNotBeNull().ShouldBe(services[1].HostAndPort);
            response2.Data.ShouldNotBeNull().ShouldBe(services[2].HostAndPort);
        }
    }

    [Fact]
    [Trait("Bug", "2110")]
    public async Task Lease_SelectedServiceIsNull_ShouldReturnError()
    {
        // Arrange
        var invalidServices = new List<Service> { null };
        var roundRobin = GivenLoadBalancer(invalidServices);

        // Act
        var response = await roundRobin.LeaseAsync(_httpContext);

        // Assert: Then ServicesAreNullError Is Returned
        response.ShouldNotBeNull().Data.ShouldBeNull();
        response.IsError.ShouldBeTrue();
        response.Errors[0].ShouldBeOfType<ServicesAreNullError>();
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

    [Fact]
    public async Task OnLeased()
    {
        // Arrange
        const string ServiceName = "products";
        var availableServices = new List<Service>
        {
            new(ServiceName, new ServiceHostAndPort("127.0.0.1", 80), string.Empty, string.Empty, Array.Empty<string>()),
        };
        var roundRobin = new TestRoundRobin(() => Task.FromResult(availableServices), ServiceName);

        // Act
        var result = await roundRobin.LeaseAsync(_httpContext);

        // Assert
        Assert.NotEmpty(roundRobin.Events);
        var args = roundRobin.Events[0];
        Assert.NotNull(args);
        Assert.Equal(ServiceName, args.Service.Name);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LeaseAsync_ServicesAreEmpty_ServicesAreEmptyError(bool isNull)
    {
        // Arrange
        List<Service> services = isNull ? null : GivenServices(0);
        var roundRobin = GivenLoadBalancer(services);

        // Act
        var actual = await roundRobin.LeaseAsync(_httpContext);

        // Assert
        Assert.True(actual.IsError);
        var error = actual.Errors[0];
        Assert.IsType<ServicesAreEmptyError>(error);
        Assert.Equal("There were no services in RoundRobin for 'LeaseAsync_ServicesAreEmpty_ServicesAreEmptyError' during LeaseAsync operation!", error.Message);
    }

    [Fact]
    public async Task Release()
    {
        // Arrange
        const string ServiceName = nameof(Release);
        var availableServices = new List<Service>
        {
            new(ServiceName, new ServiceHostAndPort("127.0.0.1", 80), string.Empty, string.Empty, Array.Empty<string>()),
        };
        var roundRobin = new RoundRobin(() => Task.FromResult(availableServices), ServiceName);
        var response = await roundRobin.LeaseAsync(_httpContext);

        // Act, Assert
        roundRobin.Release(response.Data);
    }

    [Fact]
    public void TryScanNext()
    {
        // Arrange
        const int lastIndex = 3;
        var method = typeof(RoundRobin).GetMethod(nameof(TryScanNext), BindingFlags.Instance | BindingFlags.NonPublic);
        var field = typeof(RoundRobin).GetField("LastIndices", BindingFlags.Static | BindingFlags.NonPublic);
        List<Service> services = GivenServices(lastIndex);
        var roundRobin = GivenLoadBalancer(services);
        var lastIndices = field.GetValue(roundRobin) as Dictionary<string, int>;
        lastIndices[nameof(TryScanNext)] = lastIndex;

        // Act
        // TryScanNext(Service[] readme, out Service next, out int index)
        var readme = services.ToArray();
        Service next = null;
        int index = -1;
        object[] parameters = [readme, next, index];
        bool success = (bool)method.Invoke(roundRobin, parameters);

        // Assert
        Assert.True(success);
        Assert.Equal(0, parameters[2]);
        Assert.Equal(readme[0], parameters[1]);
        Assert.Equal(1, lastIndices[nameof(TryScanNext)]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Update_CanIncreaseConnections(bool increase)
    {
        var method = typeof(RoundRobin).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
        var field = typeof(RoundRobin).GetField("_leasing", BindingFlags.Instance | BindingFlags.NonPublic);
        List<Service> services = GivenServices(1);
        var roundRobin = GivenLoadBalancer(services);
        Lease item = new(
            services[0].HostAndPort,
            increase ? 0 : 1);
        var leasing = field.GetValue(roundRobin) as List<Lease>;
        leasing.Add(item);

        // Act
        // int Update(ref Lease item, bool increase)
        object[] parameters = [item, increase];
        int index = (int)method.Invoke(roundRobin, parameters);

        Lease actual = (Lease)parameters[0];
        Assert.Equal(0, index);
        Assert.Equal(increase ? 1 : 0, actual.Connections);
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
        responses[threadIndex] = await roundRobin.LeaseAsync(_httpContext);
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
}

internal sealed class TestRoundRobin : RoundRobin, ILoadBalancer
{
    public readonly List<LeaseEventArgs> Events = new();
    public TestRoundRobin(Func<Task<List<Service>>> services, string serviceName)
        : base(services, serviceName) => Leased += Me_Leased;
    private void Me_Leased(object sender, LeaseEventArgs args) => Events.Add(args);
}
