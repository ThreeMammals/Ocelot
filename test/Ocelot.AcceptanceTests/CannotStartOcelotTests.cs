using Ocelot.Configuration.File;

namespace Ocelot.AcceptanceTests;

public class CannotStartOcelotTests : Steps
{
    private static readonly string NL = Environment.NewLine;

    [Fact]
    public void Should_throw_exception_if_cannot_start_because_service_discovery_provider_specified_in_config_but_no_service_discovery_provider_registered_with_dynamic_re_routes()
    {
        var invalidConfig = new FileConfiguration
        {
            GlobalConfiguration = new()
            {
                ServiceDiscoveryProvider = new()
                {
                    Scheme = "https",
                    Host = "localhost",
                    Type = "consul",
                    Port = 8500,
                },
            },
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
        exception.Message.ShouldBe($"One or more errors occurred. (Unable to start Ocelot, errors are:{NL}FileValidationFailedError: Unable to start Ocelot, errors are: Unable to start Ocelot because either a Route or GlobalConfiguration are using ServiceDiscoveryOptions but no ServiceDiscoveryFinderDelegate has been registered in dependency injection container. Are you missing a package like Ocelot.Provider.Consul and services.AddConsul() or Ocelot.Provider.Eureka and services.AddEureka()?{NL})");
    }

    [Fact]
    public void Should_throw_exception_if_cannot_start_because_service_discovery_provider_specified_in_config_but_no_service_discovery_provider_registered()
    {
        var invalidConfig = new FileConfiguration
        {
            Routes = new()
            {
                new()
                {
                    DownstreamPathTemplate = "/",
                    DownstreamScheme = "http",
                    UpstreamPathTemplate = "/laura",
                    UpstreamHttpMethod = ["Get"],
                    ServiceName = "test",
                },
            },
            GlobalConfiguration = new()
            {
                ServiceDiscoveryProvider = new()
                {
                    Scheme = "https",
                    Host = "localhost",
                    Type = "consul",
                    Port = 8500,
                },
            },
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
        exception.Message.ShouldBe($"One or more errors occurred. (Unable to start Ocelot, errors are:{NL}FileValidationFailedError: Unable to start Ocelot, errors are: Unable to start Ocelot because either a Route or GlobalConfiguration are using ServiceDiscoveryOptions but no ServiceDiscoveryFinderDelegate has been registered in dependency injection container. Are you missing a package like Ocelot.Provider.Consul and services.AddConsul() or Ocelot.Provider.Eureka and services.AddEureka()?{NL}FileValidationFailedError: Unable to start Ocelot, errors are: Unable to start Ocelot because either a Route or GlobalConfiguration are using ServiceDiscoveryOptions but no ServiceDiscoveryFinderDelegate has been registered in dependency injection container. Are you missing a package like Ocelot.Provider.Consul and services.AddConsul() or Ocelot.Provider.Eureka and services.AddEureka()?{NL})");
    }

    [Fact]
    public void Should_throw_exception_if_cannot_start_because_no_qos_delegate_registered_globally()
    {
        var invalidConfig = new FileConfiguration
        {
            Routes = new()
            {
                new()
                {
                    DownstreamPathTemplate = "/",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new()
                    {
                        new("localhost", 51878),
                    },
                    UpstreamPathTemplate = "/laura",
                    UpstreamHttpMethod = ["Get"],
                    Key = "Laura",
                },
            },
            GlobalConfiguration = new()
            {
                QoSOptions = new()
                {
                    TimeoutValue = 1,
                    ExceptionsAllowedBeforeBreaking = 1,
                },
            },
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
        exception.Message.ShouldBe($"One or more errors occurred. (Unable to start Ocelot, errors are:{NL}FileValidationFailedError: Unable to start Ocelot because either a Route or GlobalConfiguration are using QoSOptions but no QosDelegatingHandlerDelegate has been registered in dependency injection container. Are you missing a package like Ocelot.Provider.Polly and services.AddPolly()?{NL})");
    }

    [Fact]
    public void Should_throw_exception_if_cannot_start_because_no_qos_delegate_registered_for_re_route()
    {
        var invalidConfig = new FileConfiguration
        {
            Routes = new()
            {
                new()
                {
                    DownstreamPathTemplate = "/",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new()
                    {
                        new("localhost", 51878),
                    },
                    UpstreamPathTemplate = "/laura",
                    UpstreamHttpMethod = ["Get"],
                    Key = "Laura",
                    QoSOptions = new()
                    {
                        TimeoutValue = 1,
                        ExceptionsAllowedBeforeBreaking = 1,
                    },
                },
            },
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
        exception.Message.ShouldBe($"One or more errors occurred. (Unable to start Ocelot, errors are:{NL}FileValidationFailedError: Unable to start Ocelot because either a Route or GlobalConfiguration are using QoSOptions but no QosDelegatingHandlerDelegate has been registered in dependency injection container. Are you missing a package like Ocelot.Provider.Polly and services.AddPolly()?{NL})");
    }

    [Fact]
    public void Should_throw_exception_if_cannot_start()
    {
        var invalidConfig = new FileConfiguration
        {
            Routes = new()
            {
                new()
                {
                    UpstreamPathTemplate = "api",
                    DownstreamPathTemplate = "test",
                },
            },
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
        exception.Message.ShouldBe($"One or more errors occurred. (Unable to start Ocelot, errors are:{NL}FileValidationFailedError: Downstream Path Template test doesnt start with forward slash{NL}FileValidationFailedError: Upstream Path Template api doesnt start with forward slash{NL}FileValidationFailedError: When not using service discovery DownstreamHostAndPorts must be set and not empty or Ocelot cannot find your service!{NL})");
    }
}
