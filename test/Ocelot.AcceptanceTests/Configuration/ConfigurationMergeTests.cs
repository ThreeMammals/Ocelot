using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
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

    [BddfyTheory]
    [Trait("Bug", "1216")] // https://github.com/ThreeMammals/Ocelot/issues/1216
    [Trait("Feat", "1227")] // https://github.com/ThreeMammals/Ocelot/pull/1227
    [InlineData(MergeOcelotJson.ToFile, true)]
    [InlineData(MergeOcelotJson.ToMemory, false)]
    public void ShouldRunWithGlobalConfigMerged_WithExplicitGlobalConfigFileParameter(MergeOcelotJson where, bool fileExist)
    {
        Arrange();
        Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate = (context, config) => config
            .SetBasePath(context.HostingEnvironment.ContentRootPath)
            .AddOcelot(_initialGlobalConfig, context.HostingEnvironment, where, ocelotConfigFileName, _globalConfigFileName, null, false, false);
        var testName = TestName();
        this
            .Given(x => GivenOcelotIsRunning(configureDelegate))
            .Then(x => TheOcelotPrimaryConfigFileExists(fileExist))
            .And(x => ThenGlobalConfigurationHasBeenMerged(testName))
        .BDDfy();
    }

    [BddfyTheory]
    [Trait("Bug", "2084")] // https://github.com/ThreeMammals/Ocelot/issues/2084
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
        Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate = (context, config) => config
                .SetBasePath(context.HostingEnvironment.ContentRootPath)
                .AddOcelot(folder, context.HostingEnvironment, where) // overloaded version from the user's scenario
                .AddJsonFile(environmentPath);
        var testName = TestName();
        this
            .Given(x => GivenThereIsAConfiguration(globalConfig, globalPath))
            .And(x => GivenThereIsAConfiguration(routeAConfig, routeAPath))
            .And(x => GivenThereIsAConfiguration(routeBConfig, routeBPath))
            .And(x => GivenThereIsAConfiguration(environmentConfig, environmentPath))
            .When(x => GivenOcelotIsRunning(configureDelegate, null, null, null, host => host.UseEnvironment("Env"), null, null)) // Act
            .Then(x => TheOcelotPrimaryConfigFileExists(false))
            .And(x => ThenGlobalConfigurationHasBeenMerged(testName))
            .And(x => ThenPrimaryConfigFileExistsInTheFolder(folder, fileExist))
            .And(x => ThenConfigurationExistsInTheInternalConfigurationRepository())
            .And(x => ThenInternalConfigurationHasBeenCreatedFromTheGlobalOne(testName))
        .BDDfy();
    }
    private static void ThenPrimaryConfigFileExistsInTheFolder(string folder, bool fileExist)
    {
        var actualLocation = Path.Combine(folder, ConfigurationBuilderExtensions.PrimaryConfigFile);
        File.Exists(actualLocation).ShouldBe(fileExist);
    }
    private IInternalConfiguration _internalConfig;
    private void ThenConfigurationExistsInTheInternalConfigurationRepository()
    {
        var repository = OcelotServices.GetService<IInternalConfigurationRepository>().ShouldNotBeNull();
        _internalConfig = repository.Get().ShouldNotBeNull();
    }
    private void ThenInternalConfigurationHasBeenCreatedFromTheGlobalOne(string testName)
    {
        _internalConfig.RequestId.ShouldBe(testName);
        _internalConfig.ServiceProviderConfiguration.ConfigurationKey.ShouldBe(testName);
    }

    private void Arrange([CallerMemberName] string testName = null)
    {
        _initialGlobalConfig.GlobalConfiguration.RequestIdKey = testName;
        _initialGlobalConfig.GlobalConfiguration.ServiceDiscoveryProvider.ConfigurationKey = testName;
    }

    private void TheOcelotPrimaryConfigFileExists(bool expected)
        => File.Exists(ocelotConfigFileName).ShouldBe(expected);

    private void ThenGlobalConfigurationHasBeenMerged([CallerMemberName] string testName = null)
    {
        var config = OcelotServices.GetService<IConfiguration>().ShouldNotBeNull();
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
        UpstreamHttpMethod = [ nameof(FileRoute.UpstreamHttpMethod) + suffix ],
        DownstreamHostAndPorts = new()
        {
            new(nameof(FileHostAndPort.Host) + suffix, 80),
        },
    };
}
