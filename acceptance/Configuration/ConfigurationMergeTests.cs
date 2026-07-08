using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
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

    [Theory]
    [Trait("Bug", "1216")] // https://github.com/ThreeMammals/Ocelot/issues/1216
    [Trait("Feat", "1227")] // https://github.com/ThreeMammals/Ocelot/pull/1227
    [Trait("Release", "23.2.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/23.2.0
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

    [Theory]
    [Trait("Bug", "2084")] // https://github.com/ThreeMammals/Ocelot/issues/2084
    [Trait("Release", "23.3.4")] // https://github.com/ThreeMammals/Ocelot/releases/tag/23.3.4
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
        var folder = Path.Combine(nameof(Ocelot.AcceptanceTests.Configuration), TestID);
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

    [Fact]
    [Trait("Feat", "651")] // https://github.com/ThreeMammals/Ocelot/issues/651
    [Trait("PR", "1183")] // https://github.com/ThreeMammals/Ocelot/pull/1183
    [Trait("Release", "25.0.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0
    public async Task ShouldMergeStaticRoutesCustomPropertiesViaNewtonsoftJtoken()
    {
        var folder = Path.Combine(nameof(Ocelot.AcceptanceTests.Configuration), TestID);
        Folders.Add(Directory.CreateDirectory(folder).FullName);
        var globalPath = Path.Combine(folder, ConfigurationBuilderExtensions.GlobalConfigFile);
        var ocelotGlobalJson = @"
{
  ""Routes"": [
    {
      ""DownstreamPathTemplate"": ""/{global}"",
      ""DownstreamScheme"": ""http"",
      ""DownstreamHostAndPorts"": [
        {
          ""Host"": ""localhost"",
          ""Port"": 24012
        }
      ],
      ""UpstreamPathTemplate"": ""/{global}"",
      ""UpstreamHttpMethod"": [ ""Get"" ],
      ""SomethingMore"": ""something"" // custom property
    }
  ],
  ""GlobalConfiguration"": {
    ""BaseUrl"": ""https://ocelot.net"",
    ""GlobalCustomProperty"": 1183 // number of pull request LoL :D
  }
}";
        var xServicesPath = Path.Combine(folder, string.Format(ConfigurationBuilderExtensions.EnvironmentConfigFile, "xservices"));
        var ocelotXServicesJson = @"
{
  ""Routes"": [
    {
      ""DownstreamPathTemplate"": ""/xservice.svc/serviceMethodA/byNumber/{number}"",
      ""UpstreamPathTemplate"": ""/gw/serviceMethodA/byNumber/{number}"",
      ""UpstreamHttpMethod"": [
        ""Get"",
        ""Post""
      ],
      ""DownstreamScheme"": ""http"",
      ""DownstreamHostAndPorts"": [
        {
          ""Host"": ""host"",
          ""Port"": 80
        }
      ],
      // custom properties below
      ""CustomStrategyProperty"": {
        ""GET"": [
          ""SomeCustomStrategyMethodA""
        ],
        ""POST"": [
          ""SomeCustomStrategyMethodB""
        ]
      }
    }
  ]
}";
        var yServicesPath = Path.Combine(folder, string.Format(ConfigurationBuilderExtensions.EnvironmentConfigFile, "yservices"));
        var ocelotYServicesJson = @"
{
  ""Routes"": [
    {
      ""DownstreamPathTemplate"": ""/b/{action}"",
      ""DownstreamScheme"": ""http"",
      ""DownstreamHostAndPorts"": [
        {
          ""Host"": ""localhost"",
          ""Port"": 34234
        }
      ],
      ""UpstreamPathTemplate"": ""/b/{action}"",
      ""UpstreamHttpMethod"": [ ""Get"" ],
      ""MyCustomProperty"": [ ""myValue"" ] // custom property
    }
  ]
}";
        this.Given(x => GivenIWriteAConfiguration(globalPath, ocelotGlobalJson))
            .And(x => GivenIWriteAConfiguration(xServicesPath, ocelotXServicesJson))
            .And(x => GivenIWriteAConfiguration(yServicesPath, ocelotYServicesJson))
            .When(x => GivenOcelotIsRunningWithMultipleConfigs(folder))
            .Then(x => ThenConfigContentShouldHaveThreeRoutes(folder))
            .And(x => ShouldMergeWithCustomPropertyInXservices())
            .And(x => ShouldMergeWithCustomPropertyInYservices())
            .And(x => ShouldMergeWithCustomGlobalProperty())
        .BDDfy();
    }
    private Task GivenIWriteAConfiguration(string fpath, string ocelotJson)
    {
        Files.Add(fpath);
        return File.WriteAllTextAsync(fpath, ocelotJson, CancelMe);
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
        DownstreamHostAndPorts = [ new(nameof(FileHostAndPort.Host) + suffix, 80) ],
    };

    #region PR 1183
    private JObject _configJObject;
    private void GivenOcelotIsRunningWithMultipleConfigs(string folder)
        => GivenOcelotIsRunning((context, config) => config.AddOcelot(folder, context.HostingEnvironment));

    private async Task ThenConfigContentShouldHaveThreeRoutes(string folder)
    {
        const int three = 3;
        var mergedConfigFile = Path.Combine(folder, ConfigurationBuilderExtensions.PrimaryConfigFile);
        File.Exists(mergedConfigFile).ShouldBeTrue();
        var lines = await File.ReadAllTextAsync(mergedConfigFile);
        _configJObject = JObject.Parse(lines).ShouldNotBeNull();
        var routes = _configJObject[nameof(FileConfiguration.Routes)].ShouldNotBeNull();
        routes.Children().Count().ShouldBe(three);
    }

    private void ShouldMergeWithCustomPropertyInXservices()
    {
        var customPropertyX = PropertyShouldExist("CustomStrategyProperty");
        customPropertyX["GET"].ShouldNotBeNull();
        customPropertyX["GET"].Children().Count().ShouldBe(1);
        customPropertyX["GET"].Children().FirstOrDefault().ShouldBe("SomeCustomStrategyMethodA");
        customPropertyX["POST"].ShouldNotBeNull();
        customPropertyX["POST"].Children().Count().ShouldBe(1);
        customPropertyX["POST"].Children().FirstOrDefault().ShouldBe("SomeCustomStrategyMethodB");
    }

    private void ShouldMergeWithCustomGlobalProperty()
    {
        // in Routes
        JToken customPropertyGlobal = PropertyShouldExist("SomethingMore");
        customPropertyGlobal.ShouldBe("something");
        // in GlobalConfiguration via IConfiguration
        IConfiguration config = OcelotServices.GetService<IConfiguration>().ShouldNotBeNull();
        string actual = config["GlobalConfiguration:BaseUrl"];
        actual.ShouldNotBeNull().ShouldBe("https://ocelot.net");
        actual = config["GlobalConfiguration:GlobalCustomProperty"];
        actual.ShouldNotBeNull().ShouldBe("1183");
        int prNo = config.GetValue<int>("GlobalConfiguration:GlobalCustomProperty");
        prNo.ShouldBe(1183);
        // in GlobalConfiguration via Newtonsoft's JObject
        JToken jtGlobalConfig = _configJObject["GlobalConfiguration"].ShouldNotBeNull();
        JToken jtGlobalCustomProperty = jtGlobalConfig["GlobalCustomProperty"].ShouldNotBeNull();
        jtGlobalCustomProperty.Value<string>().ShouldBe("1183");
        jtGlobalCustomProperty.Value<int>().ShouldBe(1183);
    }

    private void ShouldMergeWithCustomPropertyInYservices()
    {
        var customPropertyY = PropertyShouldExist("MyCustomProperty");
        customPropertyY.ShouldBeAssignableTo(typeof(JArray));
        customPropertyY.Count().ShouldBe(1);
        customPropertyY.First().ShouldBe("myValue");
    }

    private JToken PropertyShouldExist(string propertyName)
    {
        var routeWithProperty = _configJObject[nameof(FileConfiguration.Routes)].Children()
            .SingleOrDefault(route => route[propertyName] != null)
            .ShouldNotBeNull();
        return routeWithProperty[propertyName].ShouldNotBeNull();
    }
    #endregion PR 1183
}
