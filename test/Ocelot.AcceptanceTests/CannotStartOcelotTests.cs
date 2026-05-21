namespace Ocelot.AcceptanceTests;

/// <summary>
/// TODO Move to Configuration feat namespace since these tests are part of Configuration Validator feature.
/// </summary>
public class CannotStartOcelotTests : Steps
{
    private static readonly string NL = Environment.NewLine;
    private Exception _ex;

    [BddfyFact]
    [Trait("Bug", "580")] // https://github.com/ThreeMammals/Ocelot/issues/580
    [Trait("Feat", "584")] // https://github.com/ThreeMammals/Ocelot/pull/584
    public void Should_throw_exception_if_cannot_start_because_service_discovery_provider_specified_in_config_but_no_service_discovery_provider_registered_with_dynamic_re_routes()
    {
        var invalidConfig = GivenConfiguration();
        invalidConfig.GlobalConfiguration.ServiceDiscoveryProvider = new()
        {
            Scheme = "https",
            Host = "localhost",
            Type = "Consul",
            Port = 8500,
        };
        this
            .Given(x => GivenThereIsAConfiguration(invalidConfig))
            .When(x => WhenIAmTryingToStartOcelot())
            .Then(x => ThenExceptionMessageShouldBe($"Unable to start Ocelot, errors are:{NL}FileValidationFailedError: Unable to start Ocelot, errors are: Unable to start Ocelot because either a Route or GlobalConfiguration are using ServiceDiscoveryOptions but no ServiceDiscoveryFinderDelegate has been registered in dependency injection container. Are you missing a package like Ocelot.Discovery.Consul and services.AddConsul() or Ocelot.Discovery.Eureka and services.AddEureka()?{NL}"))
        .BDDfy();
    }

    [BddfyFact]
    [Trait("Bug", "580")] // https://github.com/ThreeMammals/Ocelot/issues/580
    [Trait("Feat", "584")] // https://github.com/ThreeMammals/Ocelot/pull/584
    public void Should_throw_exception_if_cannot_start_because_service_discovery_provider_specified_in_config_but_no_service_discovery_provider_registered()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/laura", "/");
        var invalidConfig = GivenConfiguration(route);
        invalidConfig.GlobalConfiguration.ServiceDiscoveryProvider = new()
        {
            Scheme = "https",
            Host = "localhost",
            Type = "Consul",
            Port = 8500,
        };
        this
            .Given(x => GivenThereIsAConfiguration(invalidConfig))
            .When(x => WhenIAmTryingToStartOcelot())
            .Then(x => ThenExceptionMessageShouldBe($"Unable to start Ocelot, errors are:{NL}FileValidationFailedError: Unable to start Ocelot, errors are: Unable to start Ocelot because either a Route or GlobalConfiguration are using ServiceDiscoveryOptions but no ServiceDiscoveryFinderDelegate has been registered in dependency injection container. Are you missing a package like Ocelot.Discovery.Consul and services.AddConsul() or Ocelot.Discovery.Eureka and services.AddEureka()?{NL}"))
        .BDDfy();
    }

    [BddfyFact]
    [Trait("Bug", "568")] // https://github.com/ThreeMammals/Ocelot/issues/568
    [Trait("Feat", "578")] // https://github.com/ThreeMammals/Ocelot/pull/578
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
        this
            .Given(x => GivenThereIsAConfiguration(invalidConfig))
            .When(x => WhenIAmTryingToStartOcelot())
            .Then(x => ThenExceptionMessageShouldBe($"Unable to start Ocelot, errors are:{NL}FileValidationFailedError: Unable to start Ocelot because either a Route or GlobalConfiguration is using QoSOptions, but no QosDelegatingHandlerDelegate has been registered in the dependency injection container. Are you missing an external package like Ocelot.QualityOfService.Polly (and calling AddPolly()), or the built-in QoS support (via AddQualityOfService())?{NL}"))
        .BDDfy();
    }

    [BddfyFact]
    [Trait("Bug", "568")] // https://github.com/ThreeMammals/Ocelot/issues/568
    [Trait("Feat", "578")] // https://github.com/ThreeMammals/Ocelot/pull/578
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
        this
            .Given(x => GivenThereIsAConfiguration(invalidConfig))
            .When(x => WhenIAmTryingToStartOcelot())
            .Then(x => ThenExceptionMessageShouldBe($"Unable to start Ocelot, errors are:{NL}FileValidationFailedError: Unable to start Ocelot because either a Route or GlobalConfiguration is using QoSOptions, but no QosDelegatingHandlerDelegate has been registered in the dependency injection container. Are you missing an external package like Ocelot.QualityOfService.Polly (and calling AddPolly()), or the built-in QoS support (via AddQualityOfService())?{NL}"))
        .BDDfy();
    }

    [BddfyFact]
    [Trait("Bug", "156")] // https://github.com/ThreeMammals/Ocelot/issues/156
    [Trait("Feat", "160")] // https://github.com/ThreeMammals/Ocelot/pull/160
    public void Should_throw_exception_if_cannot_start()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "api", "test");
        var invalidConfig = GivenConfiguration(route);
        this
            .Given(x => GivenThereIsAConfiguration(invalidConfig))
            .When(x => WhenIAmTryingToStartOcelot())
            .Then(x => ThenExceptionMessageShouldBe($"Unable to start Ocelot, errors are:{NL}FileValidationFailedError: Downstream Path Template test doesnt start with forward slash{NL}FileValidationFailedError: Upstream Path Template api doesnt start with forward slash{NL}"))
        .BDDfy();
    }

    private async Task WhenIAmTryingToStartOcelot()
        => _ex = await Task.Run(GivenOcelotIsRunning).ShouldThrowAsync<Exception>();

    private void ThenExceptionMessageShouldBe(string expected)
    {
        _ex.ShouldNotBeNull();
        _ex.Message.ShouldBe(expected);
    }
}
