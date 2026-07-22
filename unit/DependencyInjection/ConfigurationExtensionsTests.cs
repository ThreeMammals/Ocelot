using Microsoft.Extensions.Configuration;
using Ocelot.DependencyInjection;

namespace Ocelot.UnitTests.DependencyInjection;

[Trait("Feat", "651")] // https://github.com/ThreeMammals/Ocelot/issues/651
[Trait("PR", "1183")] // https://github.com/ThreeMammals/Ocelot/pull/1183
[Trait("Release", "25.0.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0
public class ConfigurationExtensionsTests : UnitTest
{
    private readonly IConfigurationRoot _configRoot;

    public ConfigurationExtensionsTests()
    {
        _configRoot = BuildConfiguration(new Dictionary<string, string>
        {
            // Routes
            {"Routes:0:Key", "route1"},
            {"Routes:0:UpstreamPathTemplate", "/api/test"},
            {"Routes:1:Key", "route2"},
            {"Routes:1:UpstreamPathTemplate", "/api/other"},

            // Dynamic Routes
            {"DynamicRoutes:0:ServiceName", "svc1"},
            {"DynamicRoutes:0:ServiceNamespace", "ns1"},
            {"DynamicRoutes:0:Key", "dyn1"},
            {"DynamicRoutes:1:ServiceName", "svc2"},
            {"DynamicRoutes:1:Key", "dyn2"},

            // Aggregates
            {"Aggregates:0:UpstreamPathTemplate", "/agg1"},
            {"Aggregates:1:UpstreamPathTemplate", "/agg2"},

            // Global
            {"GlobalConfiguration:RequestIdKey", "global-req-id"},
            {"GlobalConfiguration:BaseUrl", "https://example.com"}
        });
    }

    private static IConfigurationRoot BuildConfiguration(Dictionary<string, string> data)
        => new ConfigurationBuilder().AddInMemoryCollection(data).Build();

    [Fact]
    public void OcelotRoot_Should_Return_Same_ConfigurationRoot()
    {
        // Act
        var result = _configRoot.OcelotRoot();

        // Assert
        Assert.Same(_configRoot, result);
    }

    [Fact]
    public void OcelotRoutes_Should_Return_Routes_Section()
    {
        var section = _configRoot.OcelotRoutes();
        Assert.NotNull(section);
        Assert.Equal("Routes", section.Key);
    }

    [Fact]
    public void OcelotDynamicRoutes_Should_Return_DynamicRoutes_Section()
    {
        var section = _configRoot.OcelotDynamicRoutes();
        Assert.NotNull(section);
        Assert.Equal("DynamicRoutes", section.Key);
    }

    [Fact]
    public void OcelotAggregates_Should_Return_Aggregates_Section()
    {
        var section = _configRoot.OcelotAggregates();
        Assert.NotNull(section);
        Assert.Equal("Aggregates", section.Key);
    }

    [Fact]
    public void OcelotGlobalConfiguration_Should_Return_GlobalConfiguration_Section()
    {
        var section = _configRoot.OcelotGlobalConfiguration();
        Assert.NotNull(section);
        Assert.Equal("GlobalConfiguration", section.Key);
    }

    [Trait("Feat", "651")] // https://github.com/ThreeMammals/Ocelot/issues/651
    [Trait("PR", "1183")] // https://github.com/ThreeMammals/Ocelot/pull/1183
    [Trait("Release", "25.0.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0
    public class OcelotRoute : UnitTest
    {
        [Fact]
        public void Should_Return_Matching_Route_By_UpstreamPathTemplate()
        {
            var config = BuildConfigWithRoutes();

            var result = config.OcelotRoute("/api/test");

            Assert.NotNull(result);
            Assert.Equal("route1", result["Key"]);
        }

        [Fact]
        public void Should_Return_Matching_Route_By_Key()
        {
            var config = BuildConfigWithRoutes();

            var result = config.OcelotRoute(key: "route2");

            Assert.NotNull(result);
            Assert.Equal("/api/other", result["UpstreamPathTemplate"]);
        }

        [Fact]
        public void Should_Return_Null_When_No_Match()
        {
            var config = BuildConfigWithRoutes();

            var result = config.OcelotRoute("/nonexistent");

            Assert.Null(result);
        }

        [Theory]
        [InlineData(StringComparison.OrdinalIgnoreCase, true)]
        [InlineData(StringComparison.Ordinal, false)]
        public void Should_Respect_StringComparison(StringComparison comparison, bool shouldMatch)
        {
            var config = BuildConfigWithRoutes();

            var result = config.OcelotRoute(key: "ROUTE1", comparison: comparison);

            if (shouldMatch)
                Assert.NotNull(result);
            else
                Assert.Null(result);
        }

        private static IConfigurationRoot BuildConfigWithRoutes()
        {
            var data = new Dictionary<string, string>
        {
            {"Routes:0:Key", "route1"},
            {"Routes:0:UpstreamPathTemplate", "/api/test"},
            {"Routes:1:Key", "route2"},
            {"Routes:1:UpstreamPathTemplate", "/api/other"}
        };
            return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        }
    }

    [Trait("Feat", "651")] // https://github.com/ThreeMammals/Ocelot/issues/651
    [Trait("PR", "1183")] // https://github.com/ThreeMammals/Ocelot/pull/1183
    [Trait("Release", "25.0.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0
    public class OcelotDynamicRoute : UnitTest
    {
        [Fact]
        public void Should_Return_Matching_By_ServiceName_Only()
        {
            var config = BuildConfigWithDynamicRoutes();

            var result = config.OcelotDynamicRoute("svc2");

            Assert.NotNull(result);
            Assert.Equal("dyn2", result["Key"]);
        }

        [Fact]
        public void Should_Return_Matching_By_ServiceName_And_Namespace()
        {
            var config = BuildConfigWithDynamicRoutes();

            var result = config.OcelotDynamicRoute("svc1", "ns1");

            Assert.NotNull(result);
            Assert.Equal("dyn1", result["Key"]);
        }

        [Fact]
        public void Should_Return_Matching_By_Key()
        {
            var config = BuildConfigWithDynamicRoutes();

            var result = config.OcelotDynamicRoute(key: "dyn1");

            Assert.NotNull(result);
            Assert.Equal("svc1", result["ServiceName"]);
        }

        private static IConfigurationRoot BuildConfigWithDynamicRoutes()
        {
            var data = new Dictionary<string, string>
        {
            {"DynamicRoutes:0:ServiceName", "svc1"},
            {"DynamicRoutes:0:ServiceNamespace", "ns1"},
            {"DynamicRoutes:0:Key", "dyn1"},
            {"DynamicRoutes:1:ServiceName", "svc2"},
            {"DynamicRoutes:1:Key", "dyn2"}
        };
            return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        }
    }

    [Trait("Feat", "651")] // https://github.com/ThreeMammals/Ocelot/issues/651
    [Trait("PR", "1183")] // https://github.com/ThreeMammals/Ocelot/pull/1183
    [Trait("Release", "25.0.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0
    public class OcelotAggregate : UnitTest
    {
        [Fact]
        public void Should_Return_Null_When_ArgIsNull()
        {
            var config = BuildConfigWithAggregates();

            var result = config.OcelotAggregate(null);

            Assert.Null(result);
        }

        [Fact]
        public void Should_Return_Matching_Aggregate_By_UpstreamPathTemplate()
        {
            var config = BuildConfigWithAggregates();

            var result = config.OcelotAggregate("/agg2");

            Assert.NotNull(result);
            Assert.Equal("/agg2", result["UpstreamPathTemplate"]);
        }

        [Fact]
        public void Should_Return_Null_When_No_Match()
        {
            var config = BuildConfigWithAggregates();

            var result = config.OcelotAggregate("/nonexistent");

            Assert.Null(result);
        }

        private static IConfigurationRoot BuildConfigWithAggregates()
        {
            var data = new Dictionary<string, string>
        {
            {"Aggregates:0:UpstreamPathTemplate", "/agg1"},
            {"Aggregates:1:UpstreamPathTemplate", "/agg2"}
        };
            return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        }
    }

    [Trait("Feat", "651")] // https://github.com/ThreeMammals/Ocelot/issues/651
    [Trait("PR", "1183")] // https://github.com/ThreeMammals/Ocelot/pull/1183
    [Trait("Release", "25.0.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0
    public class OcelotSection : UnitTest
    {
        [Fact]
        public void Should_Return_From_Routes_Section_When_Found()
        {
            var config = BuildConfig();

            var result = config.OcelotSection("0"); // first route child key

            Assert.NotNull(result);
            Assert.Equal("route1", result["Key"]);
        }

        [Fact]
        public void Should_Fallback_To_Global_Section()
        {
            var config = BuildConfig();

            var result = config.OcelotSection("RequestIdKey");

            Assert.NotNull(result);
            Assert.Equal("global-req-id", result.Value);
        }

        private static IConfigurationRoot BuildConfig()
        {
            var data = new Dictionary<string, string>
        {
            {"Routes:0:Key", "route1"},
            {"GlobalConfiguration:RequestIdKey", "global-req-id"}
        };
            return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        }
    }

    [Trait("Feat", "651")] // https://github.com/ThreeMammals/Ocelot/issues/651
    [Trait("PR", "1183")] // https://github.com/ThreeMammals/Ocelot/pull/1183
    [Trait("Release", "25.0.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0
    public class OcelotProperty : UnitTest
    {
        [Fact]
        public void Should_DefaultsToOcelotRoutes_WhenCurrentIsNull()
        {
            var config = BuildConfig();

            var result = config.OcelotProperty<string>("Key", null);

            Assert.Null(result);
        }

        [Fact]
        public void Should_Get_Typed_Property()
        {
            var config = BuildConfig();

            var result = config.OcelotProperty<string>("Key", config.OcelotRoutes().GetSection("0"));

            Assert.Equal("route1", result);
        }

        private static IConfigurationRoot BuildConfig()
        {
            var data = new Dictionary<string, string> { { "Routes:0:Key", "route1" } };
            return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        }
    }

    [Trait("Feat", "651")] // https://github.com/ThreeMammals/Ocelot/issues/651
    [Trait("PR", "1183")] // https://github.com/ThreeMammals/Ocelot/pull/1183
    [Trait("Release", "25.0.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0
    public class OcelotGlobalSectionTests : UnitTest
    {
        [Fact]
        public void Should_Return_Global_Child_By_Key()
        {
            var config = BuildConfig();

            var result = config.OcelotGlobalSection("BaseUrl");

            Assert.NotNull(result);
            Assert.Equal("https://example.com", result.Value);
        }

        private static IConfigurationRoot BuildConfig()
        {
            var data = new Dictionary<string, string> { { "GlobalConfiguration:BaseUrl", "https://example.com" } };
            return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        }
    }

    [Trait("Feat", "651")] // https://github.com/ThreeMammals/Ocelot/issues/651
    [Trait("PR", "1183")] // https://github.com/ThreeMammals/Ocelot/pull/1183
    [Trait("Release", "25.0.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0
    public class OcelotGlobalProperty : UnitTest
    {
        [Fact]
        public void Should_Get_Typed_Global_Property()
        {
            var config = BuildConfig();

            var result = config.OcelotGlobalProperty<string>("RequestIdKey");

            Assert.Equal("global-req-id", result);
        }

        private static IConfigurationRoot BuildConfig()
        {
            var data = new Dictionary<string, string> { { "GlobalConfiguration:RequestIdKey", "global-req-id" } };
            return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        }
    }
}
