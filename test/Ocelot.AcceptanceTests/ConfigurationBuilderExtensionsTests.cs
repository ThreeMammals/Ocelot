using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

namespace Ocelot.AcceptanceTests
{
    public class ConfigurationBuilderExtensionsTests
    {        
        private IWebHostBuilder _webHostBuilder;
        private TestServer _ocelotServer;

        [Fact]
        public void Should_merge_routes_custom_properties()
        {
            this.Given(x => GivenOcelotIsRunningWithMultipleConfigs())
                .When(x => x.WhenICreateClient())
                .Then(x => ThenConfigContentShouldHaveThreeRoutes())
                .And(x => ShouldMergeWithCustomPropertyInXservices())
                .And(x => ShouldMergeWithCustomGlobalProperty())
                .And(x => ShouldMergeWithCustomPropertyInYservices())
                .BDDfy();
        }

        private void GivenOcelotIsRunningWithMultipleConfigs()
        {
            _webHostBuilder = new WebHostBuilder();

            _webHostBuilder
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
                    var env = hostingContext.HostingEnvironment;
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: false);
                    config.AddOcelot("MergeConfiguration", hostingContext.HostingEnvironment);
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices(s =>
                {
                    s.AddOcelot();
                })
                .Configure(app =>
                {
                    app.UseOcelot().Wait();
                });
        }

        private void WhenICreateClient()
        {
            _ocelotServer = new TestServer(_webHostBuilder);
            _ = _ocelotServer.CreateClient();
        }

        private void ThenConfigContentShouldHaveThreeRoutes()
        {
            var mergedConfigFileName = "ocelot.json";
            File.Exists(mergedConfigFileName).ShouldBeTrue();
            var lines = File.ReadAllText(mergedConfigFileName);
            _config = JObject.Parse(lines);

            _config[nameof(FileConfiguration.Routes)].ShouldNotBeNull()
                .Children().Count().ShouldBe(3);
        }

        private JObject _config;

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
            var customPropertyGlobal = PropertyShouldExist("SomethingMore");
            customPropertyGlobal.ShouldBe("something");
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
            var routeWithProperty = _config[nameof(FileConfiguration.Routes)].Children()
                .SingleOrDefault(route => route[propertyName] != null)
                .ShouldNotBeNull();
            return routeWithProperty[propertyName].ShouldNotBeNull();
        }
    }
}
