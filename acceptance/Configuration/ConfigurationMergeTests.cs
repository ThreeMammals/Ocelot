using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.DependencyInjection;
using Ocelot.Testing.Steps;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Ocelot.Acceptance.Configuration;

public sealed class ConfigurationMergeTests : DiscoverySteps
{
    private readonly FileConfiguration _initialGlobalConfig;
    private string _folder;

    public ConfigurationMergeTests() : base()
    {
        _initialGlobalConfig = new();
        _folder = Path.Combine(nameof(Configuration), TestID);
        Folders.Add(Directory.CreateDirectory(_folder).FullName);
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
        var globalConfigFileName = $"{TestID}-{ConfigurationBuilderExtensions.GlobalConfigFile}";
        Files.Add(globalConfigFileName);
        Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate = (context, config) => config
            .SetBasePath(context.HostingEnvironment.ContentRootPath)
            .AddOcelot(_initialGlobalConfig, context.HostingEnvironment, where, ocelotConfigFileName, globalConfigFileName, null, false, false);
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
        var globalPath = Path.Combine(_folder, ConfigurationBuilderExtensions.GlobalConfigFile);
        var routeAPath = Path.Combine(_folder, string.Format(ConfigurationBuilderExtensions.EnvironmentConfigFile, "A"));
        var routeBPath = Path.Combine(_folder, string.Format(ConfigurationBuilderExtensions.EnvironmentConfigFile, "B"));
        var environmentPath = Path.Combine(_folder, string.Format(ConfigurationBuilderExtensions.EnvironmentConfigFile, "Env"));
        Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate = (context, config) => config
                .SetBasePath(context.HostingEnvironment.ContentRootPath)
                .AddOcelot(_folder, context.HostingEnvironment, where) // overloaded version from the user's scenario
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
            .And(x => ThenPrimaryConfigFileExistsInTheFolder(_folder, fileExist))
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
        var globalPath = Path.Combine(_folder, ConfigurationBuilderExtensions.GlobalConfigFile);
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
        var xServicesPath = Path.Combine(_folder, string.Format(ConfigurationBuilderExtensions.EnvironmentConfigFile, "xservices"));
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
        var yServicesPath = Path.Combine(_folder, string.Format(ConfigurationBuilderExtensions.EnvironmentConfigFile, "yservices"));
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
            .When(x => GivenOcelotIsRunning(WithConfigurationFolder))
            .Then(x => ThenConfigContentShouldHaveANumberOfRoutes(_folder, 3, 0))
            .And(x => ShouldMergeWithCustomPropertyInXservices("/gw/serviceMethodA/byNumber/{number}"))
            .And(x => ShouldMergeWithCustomPropertyInYservices("/b/{action}"))
            .And(x => ShouldMergeWithCustomGlobalProperty("/{global}"))
        .BDDfy();
    }

    [Fact]
    [Trait("Feat", "651")] // https://github.com/ThreeMammals/Ocelot/issues/651
    [Trait("PR", "1183")] // https://github.com/ThreeMammals/Ocelot/pull/1183
    [Trait("Release", "25.0.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0
    public async Task ShouldMergeDynamicRoutesCustomPropertiesViaNewtonsoftJtoken()
    {
        var globalPath = Path.Combine(_folder, ConfigurationBuilderExtensions.GlobalConfigFile);
        var ocelotGlobalJson = @"
{
  ""Routes"": [], // must be empty to enable dynamic routing!
  ""DynamicRoutes"": [
    // overriding goes here, but in another ocelot.X.json files
  ],
  ""GlobalConfiguration"": {
    ""BaseUrl"": ""https://api.ocelot.net"",
    ""ServiceDiscoveryProvider"": {
      ""Host"": ""my-consul.ocelot.net"",
      ""Port"": 8555,
      ""Type"": ""Consul"",
      ""Namespace"": ""ShouldMergeDynamicRoutesCustomPropertiesViaNewtonsoftJtoken"",
      ""MyDiscovery"": ""I use Consul to manage services and have switched Ocelot to dynamic routing mode."" // custom property
    },
    ""CacheOptions"": {
      ""TtlSeconds"": 300,
      ""TtlMinutes"": 777 // custom property: I'd like to specify TTL in minutes 😄
    },
    ""RateLimitOptions"": {
      ""ClientIdHeader"": ""Bla-bla-client"",
      ""QuotaMessage"": ""No Way!"", // 😆
      ""MyOpinionMessage"": ""Why is the rate-limiting feature so boring in Ocelot? 😏"" // custom property, :)
    },
    // custom options
    ""MyOptions"": {
      ""Country"": ""United Kingdom"",
      ""Town"": ""Liskeard"",
      ""HighTemp"": 37 // record-breaking peak of 37.3°C in June 26th!!! 😲
    },
  }
}";
        Action ShouldMergeWithCustomPropertyInGlobalConfiguration = () =>
        {
            var global = _configJObject.OcelotJSection(nameof(FileConfiguration.GlobalConfiguration));
            JsonNode sdpOptions = global["ServiceDiscoveryProvider"];
            sdpOptions["Host"].ToString().ShouldBe("my-consul.ocelot.net");
            sdpOptions["Namespace"].GetValue<string>().ShouldBe(nameof(ShouldMergeDynamicRoutesCustomPropertiesViaNewtonsoftJtoken));
            sdpOptions["MyDiscovery"].GetValue<string>().ShouldBe("I use Consul to manage services and have switched Ocelot to dynamic routing mode.");
            global["CacheOptions"]["TtlMinutes"].GetValue<int>().ShouldBe(777);
            global["RateLimitOptions"]["MyOpinionMessage"].GetValue<string>().ShouldBe("Why is the rate-limiting feature so boring in Ocelot? 😏");
            var myOptions = global["MyOptions"].ShouldNotBeNull();
            myOptions["Country"].ToString().ShouldBe("United Kingdom");
            myOptions["Town"].ToString().ShouldBe("Liskeard");
            myOptions["HighTemp"].GetValue<int>().ShouldBe(37);
        };

        var xServicesPath = Path.Combine(_folder, string.Format(ConfigurationBuilderExtensions.EnvironmentConfigFile, "X"));
        var ocelotXServicesJson = @"
{
  ""DynamicRoutes"": [
    {
      ""ServiceName"": ""product"",
      ""ServiceNamespace"": ""MyNamespace"",
      ""RateLimitOptions"": {
        ""Limit"": 5,
        ""Period"": ""1s"",
        ""Wait"": ""1.5s"" // hybrid fixed window
      }
    },
    {
      ""ServiceName"": ""notification"",
      ""LoadBalancerOptions"": {
        ""Type"": ""MyAlgorithm"",
        ""HighLoad"": 1000 // per second
      },
      ""MyCustomDynamicProperty"": ""https://my-cluster.ocelot.net"",
      ""MyCustomOptions"": {
        ""Enabled"": false
      }
    }
  ],
}";
        Action ShouldMergeWithCustomPropertyInNotificationService = () =>
        {
            var routes = _configJObject.OcelotJSection(nameof(FileConfiguration.DynamicRoutes)) as JsonArray;
            var route = routes.SingleOrDefault(r => r["ServiceName"].GetValue<string>() == "notification");
            var lbOptions = route["LoadBalancerOptions"];
            var lbo = lbOptions.Deserialize<FileLoadBalancerOptions>().ShouldNotBeNull();
            lbo.Type.ShouldBe("MyAlgorithm");
            lbOptions["Type"].GetValue<string>().ShouldBe("MyAlgorithm");
            lbOptions["HighLoad"].GetValue<int>().ShouldBe(1000);
            route["MyCustomDynamicProperty"].GetValue<string>().ShouldBe("https://my-cluster.ocelot.net");
            var myCustomOptions = route["MyCustomOptions"].Deserialize<MyCustomOptions>().ShouldNotBeNull();
            myCustomOptions.Enabled.ShouldBeFalse();
        };
        this.Given(x => GivenIWriteAConfiguration(globalPath, ocelotGlobalJson))
            .And(x => GivenIWriteAConfiguration(xServicesPath, ocelotXServicesJson))
            .When(x => GivenOcelotIsRunning(WithConfigurationFolder, WithDiscovery)) // We need to configure discovery to suppress Ocelot's startup validation errors
            .Then(x => ThenConfigContentShouldHaveANumberOfRoutes(_folder, 0, 2))
            .And(x => ShouldMergeWithCustomPropertyInGlobalConfiguration.Invoke())
            .And(x => ShouldMergeWithCustomPropertyInNotificationService.Invoke())
        .BDDfy();
    }
    class MyCustomOptions
    {
        public bool Enabled { get; set; }
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
    private /*JObject*/ JsonObject _configJObject;
    private void WithConfigurationFolder(WebHostBuilderContext context, IConfigurationBuilder builder) => builder
        .AddOcelot(_folder, context.HostingEnvironment);
    public override void WithDiscovery(IServiceCollection services) => services
        .AddSingleton(DynamicRoutingDiscoveryFinder)
        .AddOcelot();

    private async Task ThenConfigContentShouldHaveANumberOfRoutes(string folder, int staticRoutesCount, int dynamicRoutesCount)
    {
        var mergedConfigFile = Path.Combine(folder, ConfigurationBuilderExtensions.PrimaryConfigFile);
        File.Exists(mergedConfigFile).ShouldBeTrue();
        var lines = await File.ReadAllTextAsync(mergedConfigFile);
        _configJObject = JsonNode.Parse(lines).AsObject().ShouldNotBeNull();
        var routes = _configJObject.OcelotJSection(nameof(FileConfiguration.Routes)) as JsonArray;
        routes.ShouldNotBeNull().Count().ShouldBe(staticRoutesCount);
        var dynamicRoutes = _configJObject.OcelotJSection(nameof(FileConfiguration.DynamicRoutes)) as JsonArray;
        dynamicRoutes.ShouldNotBeNull().Count().ShouldBe(dynamicRoutesCount);
    }

    private void ShouldMergeWithCustomPropertyInXservices(string upstreamPath)
    {
        var routes = _configJObject.OcelotJSection(nameof(FileConfiguration.Routes)) as JsonArray;
        var route = routes.SingleOrDefault(r => r["UpstreamPathTemplate"].GetValue<string>() == upstreamPath);
        var customPropertyX = route["CustomStrategyProperty"];
        var xGET = customPropertyX["GET"] as JsonArray;
        xGET.ShouldNotBeNull();
        xGET.Count.ShouldBe(1);
        xGET[0].GetValue<string>().ShouldBe("SomeCustomStrategyMethodA");
        var xPOST = customPropertyX["POST"] as JsonArray;
        xPOST.ShouldNotBeNull();
        xPOST.Count.ShouldBe(1);
        xPOST[0].GetValue<string>().ShouldBe("SomeCustomStrategyMethodB");
    }

    private void ShouldMergeWithCustomGlobalProperty(string upstreamPath)
    {
        // in Routes
        var routes = _configJObject.OcelotJSection(nameof(FileConfiguration.Routes)) as JsonArray;
        var route = routes.SingleOrDefault(r => r["UpstreamPathTemplate"].GetValue<string>() == upstreamPath);
        JsonNode customPropertyGlobal = route["SomethingMore"];
        customPropertyGlobal.GetValue<string>().ShouldBe("something");
        // in GlobalConfiguration via IConfiguration
        IConfiguration config = OcelotServices.GetService<IConfiguration>().ShouldNotBeNull();
        config.OcelotGlobalProperty<string>("BaseUrl").ShouldBe("https://ocelot.net");
        config.OcelotGlobalProperty<int>("GlobalCustomProperty").ShouldBe(1183);
        // in GlobalConfiguration via Newtonsoft's JObject
        JsonNode jtGlobalConfig = _configJObject["GlobalConfiguration"].ShouldNotBeNull();
        JsonNode jtGlobalCustomProperty = jtGlobalConfig["GlobalCustomProperty"].ShouldNotBeNull();
        jtGlobalCustomProperty.ToString().ShouldBe("1183");
        jtGlobalCustomProperty.GetValue<int>().ShouldBe(1183);
    }

    private void ShouldMergeWithCustomPropertyInYservices(string upstreamPath)
    {
        var routes = _configJObject.OcelotJSection(nameof(FileConfiguration.Routes)) as JsonArray;
        var route = routes.SingleOrDefault(r => r["UpstreamPathTemplate"].GetValue<string>() == upstreamPath);
        JsonNode myCustomProperty = route["MyCustomProperty"];
        myCustomProperty.ShouldBeAssignableTo<JsonArray>();
        var customPropertyY = myCustomProperty as JsonArray;
        customPropertyY.ShouldNotBeNull().Count.ShouldBe(1);
        customPropertyY[0].GetValue<string>().ShouldBe("myValue");
    }
    #endregion PR 1183
}
