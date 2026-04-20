namespace Ocelot.AcceptanceTests;

public class CannotStartOcelotTests : Steps
{
    private static readonly string NL = Environment.NewLine;

    [Fact]
    public void Should_throw_exception_if_cannot_start_because_service_discovery_provider_specified_in_config_but_no_service_discovery_provider_registered_with_dynamic_re_routes()
    {
        var invalidConfig = GivenConfiguration();
        invalidConfig.GlobalConfiguration.ServiceDiscoveryProvider = new()
        {
            Scheme = "https",
            Host = "localhost",
            Type = nameof(Provider.Consul.Consul),
            Port = 8500,
        };

        Exception exception = null;
        GivenThereIsAConfiguration(invalidConfig);
        try
        {
            GivenOcelotIsRunning();
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        exception.ShouldNotBeNull();
        exception.Message.ShouldBe($"Unable to start Ocelot, errors are:{NL}FileValidationFailedError: Unable to start Ocelot, errors are: Unable to start Ocelot because either a Route or GlobalConfiguration are using ServiceDiscoveryOptions but no ServiceDiscoveryFinderDelegate has been registered in dependency injection container. Are you missing a package like Ocelot.Provider.Consul and services.AddConsul() or Ocelot.Provider.Eureka and services.AddEureka()?{NL}");
    }

    [Fact]
    public void Should_throw_exception_if_cannot_start_because_service_discovery_provider_specified_in_config_but_no_service_discovery_provider_registered()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/laura", "/");
        var invalidConfig = GivenConfiguration(route);
        invalidConfig.GlobalConfiguration.ServiceDiscoveryProvider = new()
        {
            Scheme = "https",
            Host = "localhost",
            Type = nameof(Provider.Consul.Consul),
            Port = 8500,
        };

        Exception exception = null;
        GivenThereIsAConfiguration(invalidConfig);
        try
        {
            GivenOcelotIsRunning();
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        exception.ShouldNotBeNull();
        exception.Message.ShouldBe($"Unable to start Ocelot, errors are:{NL}FileValidationFailedError: Unable to start Ocelot, errors are: Unable to start Ocelot because either a Route or GlobalConfiguration are using ServiceDiscoveryOptions but no ServiceDiscoveryFinderDelegate has been registered in dependency injection container. Are you missing a package like Ocelot.Provider.Consul and services.AddConsul() or Ocelot.Provider.Eureka and services.AddEureka()?{NL}");
    }

    [Fact]
    public void Should_throw_exception_if_cannot_start_because_no_qos_delegate_registered_globally()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/laura", "/");
        var invalidConfig = GivenConfiguration(route);
        invalidConfig.GlobalConfiguration.QoSOptions = new()
        {
            TimeoutValue = 1,
            ExceptionsAllowedBeforeBreaking = 1,
        };

        Exception exception = null;
        GivenThereIsAConfiguration(invalidConfig);
        try
        {
            GivenOcelotIsRunning();
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        exception.ShouldNotBeNull();
        exception.Message.ShouldBe($"Unable to start Ocelot, errors are:{NL}FileValidationFailedError: Unable to start Ocelot because either a Route or GlobalConfiguration are using QoSOptions but no QosDelegatingHandlerDelegate has been registered in dependency injection container. Are you missing a package like Ocelot.QualityOfService.Polly and services.AddPolly()?{NL}");
    }

    [Fact]
    public void Should_throw_exception_if_cannot_start_because_no_qos_delegate_registered_for_re_route()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/laura", "/");
        route.QoSOptions = new()
        {
            TimeoutValue = 1,
            ExceptionsAllowedBeforeBreaking = 1,
        };
        var invalidConfig = GivenConfiguration(route);

        Exception exception = null;
        GivenThereIsAConfiguration(invalidConfig);
        try
        {
            GivenOcelotIsRunning();
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        exception.ShouldNotBeNull();
        exception.Message.ShouldBe($"Unable to start Ocelot, errors are:{NL}FileValidationFailedError: Unable to start Ocelot because either a Route or GlobalConfiguration are using QoSOptions but no QosDelegatingHandlerDelegate has been registered in dependency injection container. Are you missing a package like Ocelot.QualityOfService.Polly and services.AddPolly()?{NL}");
    }

    [Fact]
    public void Should_throw_exception_if_cannot_start()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "api", "test");
        var invalidConfig = GivenConfiguration(route);
        Exception exception = null;
        GivenThereIsAConfiguration(invalidConfig);
        try
        {
            GivenOcelotIsRunning();
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        exception.ShouldNotBeNull();
        exception.Message.ShouldBe($"Unable to start Ocelot, errors are:{NL}FileValidationFailedError: Downstream Path Template test doesnt start with forward slash{NL}FileValidationFailedError: Upstream Path Template api doesnt start with forward slash{NL}");
    }
}
