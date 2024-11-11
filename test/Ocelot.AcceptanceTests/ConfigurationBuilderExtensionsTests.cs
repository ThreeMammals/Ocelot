using Newtonsoft.Json.Linq;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;

namespace Ocelot.AcceptanceTests
{
    public class ConfigurationBuilderExtensionsTests : Steps
    {
        private JObject _config;

        [Fact]
        public void Should_merge_routes_custom_properties()
        {
            var folder = "MergeConfiguration"; // TODO Convert to dynamic temp test folder instead of static one
            this.Given(x => GivenOcelotIsRunningWithMultipleConfigs(folder))
                .Then(x => ThenConfigContentShouldHaveThreeRoutes(folder))
                .And(x => ShouldMergeWithCustomPropertyInXservices())
                .And(x => ShouldMergeWithCustomGlobalProperty())
                .And(x => ShouldMergeWithCustomPropertyInYservices())
                .BDDfy();
        }

        private void GivenOcelotIsRunningWithMultipleConfigs(string folder) => StartOcelot(
            (context, config) => config.AddOcelot(folder, context.HostingEnvironment),
            "Env");

        private async Task ThenConfigContentShouldHaveThreeRoutes(string folder)
        {
            const int three = 3;
            var mergedConfigFile = Path.Combine(folder, ConfigurationBuilderExtensions.PrimaryConfigFile);
            File.Exists(mergedConfigFile).ShouldBeTrue();
            var lines = await File.ReadAllTextAsync(mergedConfigFile);
            _config = JObject.Parse(lines).ShouldNotBeNull();
            var routes = _config[nameof(FileConfiguration.Routes)].ShouldNotBeNull();
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
