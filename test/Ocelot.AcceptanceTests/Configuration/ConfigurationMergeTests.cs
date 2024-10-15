using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.DependencyInjection;
using System.Runtime.CompilerServices;

namespace Ocelot.AcceptanceTests.Configuration;

public sealed class ConfigurationMergeTests : Steps
{
    private readonly FileConfiguration _initialGlobalConfig;
    private readonly string _globalConfigFileName;

    public ConfigurationMergeTests() : base()
    {
        _initialGlobalConfig = new();
        _globalConfigFileName = $"{TestID}-{ConfigurationBuilderExtensions.GlobalConfigFile}";
        Files.Add(_globalConfigFileName);
    }

    [Theory]
    [Trait("Bug", "1216")]
    [Trait("Feat", "1227")]
    [InlineData(MergeOcelotJson.ToFile, true)]
    [InlineData(MergeOcelotJson.ToMemory, false)]
    public void ShouldRunWithGlobalConfigMerged_WithExplicitGlobalConfigFileParameter(MergeOcelotJson where, bool fileExist)
    {
        Arrange();

        // Act
        StartOcelot((context, config) => config
            .AddOcelot(_initialGlobalConfig, context.HostingEnvironment, where, _ocelotConfigFileName, _globalConfigFileName, null, false, false));

        // Assert
        TheOcelotPrimaryConfigFileExists(fileExist);
        ThenGlobalConfigurationHasBeenMerged();
    }

    [Theory]
    [Trait("Bug", "2084")]
    [InlineData(MergeOcelotJson.ToFile, true)]
    [InlineData(MergeOcelotJson.ToMemory, false)]
    public void ShouldRunWithGlobalConfigMerged_WithImplicitGlobalConfigFileParameter(MergeOcelotJson where, bool fileExist)
    {
        Arrange();
        var globalConfig = _initialGlobalConfig;
        globalConfig.Routes.Clear();
        var routeAConfig = GivenConfiguration(GetRoute("A"));
        var routeBConfig = GivenConfiguration(GetRoute("B"));
        var environmentConfig = GivenConfiguration(GetRoute("Env"));
        environmentConfig.GlobalConfiguration = null;
        var folder = "GatewayConfiguration-" + TestID;
        Folders.Add(Directory.CreateDirectory(folder).FullName);
        var globalPath = Path.Combine(folder, ConfigurationBuilderExtensions.GlobalConfigFile);
        var routeAPath = Path.Combine(folder, string.Format(ConfigurationBuilderExtensions.EnvironmentConfigFile, "A"));
        var routeBPath = Path.Combine(folder, string.Format(ConfigurationBuilderExtensions.EnvironmentConfigFile, "B"));
        var environmentPath = Path.Combine(folder, string.Format(ConfigurationBuilderExtensions.EnvironmentConfigFile, "Env"));
        GivenThereIsAConfiguration(globalConfig, globalPath);
        GivenThereIsAConfiguration(routeAConfig, routeAPath);
        GivenThereIsAConfiguration(routeBConfig, routeBPath);
        GivenThereIsAConfiguration(environmentConfig, environmentPath);

        // Act
        StartOcelot((context, config) => config
            .AddOcelot(folder, context.HostingEnvironment, where) // overloaded version from the user's scenario
            .AddJsonFile(environmentPath),
            "Env");

        // Assert
        TheOcelotPrimaryConfigFileExists(false);
        ThenGlobalConfigurationHasBeenMerged();

        var actualLocation = Path.Combine(folder, ConfigurationBuilderExtensions.PrimaryConfigFile);
        File.Exists(actualLocation).ShouldBe(fileExist);

        var repository = _ocelotServer.Services.GetService<IInternalConfigurationRepository>().ShouldNotBeNull();
        var response = repository.Get().ShouldNotBeNull();
        response.IsError.ShouldBeFalse();
        var internalConfig = response.Data.ShouldNotBeNull();

        // Assert Arrange() setup
        internalConfig.RequestId.ShouldBe(nameof(ShouldRunWithGlobalConfigMerged_WithImplicitGlobalConfigFileParameter));
        internalConfig.ServiceProviderConfiguration.ConfigurationKey.ShouldBe(nameof(ShouldRunWithGlobalConfigMerged_WithImplicitGlobalConfigFileParameter));
    }

    private void Arrange([CallerMemberName] string testName = null)
    {
        _initialGlobalConfig.GlobalConfiguration.RequestIdKey = testName;
        _initialGlobalConfig.GlobalConfiguration.ServiceDiscoveryProvider.ConfigurationKey = testName;
    }

    private void TheOcelotPrimaryConfigFileExists(bool expected)
        => File.Exists(_ocelotConfigFileName).ShouldBe(expected);

    private void ThenGlobalConfigurationHasBeenMerged([CallerMemberName] string testName = null)
    {
        var config = _ocelotServer.Services.GetService<IConfiguration>().ShouldNotBeNull();
        var actual = config["GlobalConfiguration:RequestIdKey"];
        actual.ShouldNotBeNull().ShouldBe(testName);
        actual = config["GlobalConfiguration:ServiceDiscoveryProvider:ConfigurationKey"];
        actual.ShouldNotBeNull().ShouldBe(testName);
    }

    private static FileRoute GetRoute(string suffix, [CallerMemberName] string testName = null) => new()
    {
        DownstreamScheme = nameof(FileRoute.DownstreamScheme) + suffix,
        DownstreamPathTemplate = "/" + suffix,
        Key = testName + suffix,
        UpstreamPathTemplate = "/" + suffix,
        UpstreamHttpMethod = new() { nameof(FileRoute.UpstreamHttpMethod) + suffix },
        DownstreamHostAndPorts = new()
        {
            new(nameof(FileHostAndPort.Host) + suffix, 80),
        },
    };
}
