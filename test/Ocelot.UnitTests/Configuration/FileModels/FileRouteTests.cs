using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration.FileModels;

public class FileRouteTests : UnitTest
{
    [Fact]
    [Trait("PR", "1753")]
    public void Ctor_Copying_Copied()
    {
        // Arrange
        var expected = GivenFileRoute();

        // Act
        FileRoute actual = new(expected); // copying

        // Assert
        Assert.Equivalent(expected, actual);
        AssertEquality(actual, expected);
    }

    [Fact]
    [Trait("PR", "1753")]
    public void Clone_ShouldClone()
    {
        // Arrange
        var expected = GivenFileRoute();

        // Act
        var obj = expected.Clone();
        var actual = Assert.IsType<FileRoute>(obj);

        // Assert
        Assert.Equivalent(expected, actual);
        AssertEquality(actual, expected);
    }

    [Fact]
    [Trait("Feat", "1229")]
    [Trait("PR", "2294")]
    public void ToString_NoKeyNoDiscovery_ShouldBeUpstreamPathTemplate()
    {
        // Arrange
        var route = GivenFileRoute();
        route.Key = null;
        route.ServiceName = null;
        route.UpstreamPathTemplate = "/upstream";

        // Act
        var actual = route.ToString();

        // Assert
        Assert.Equal("/upstream", actual);
    }

    [Theory]
    [Trait("Feat", "1229")]
    [Trait("PR", "2294")]
    [InlineData(null, "?")]
    [InlineData("/downstream", "/downstream")]
    public void ToString_NoKeyNoUpstreamPathTemplate_ShouldBeDownstreamPathTemplate(string downstream, string expected)
    {
        // Arrange
        var route = GivenFileRoute();
        route.Key = null;
        route.ServiceName = null;
        route.UpstreamPathTemplate = null;
        route.DownstreamPathTemplate = downstream;

        // Act
        var actual = route.ToString();

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    [Trait("Feat", "1229")]
    [Trait("PR", "2294")]
    public void ToString_NoKeyWithServiceDiscovery_ShouldContainServiceName()
    {
        // Arrange
        var route = GivenFileRoute();
        route.Key = null;
        route.ServiceName = TestName();
        route.UpstreamPathTemplate = "/upstream";

        // Act
        var actual = route.ToString();

        // Assert
        Assert.True(actual.Contains(route.ServiceName));
        Assert.Equal("test-namespace:ToString_NoKeyWithServiceDiscovery_ShouldContainServiceName:/upstream", actual);
    }

    [Fact]
    [Trait("Feat", "1229")]
    [Trait("PR", "2294")]
    public void ToString_HasKey_ShouldBeKey()
    {
        // Arrange
        var route = GivenFileRoute();
        route.Key = TestName();

        // Act
        var actual = route.ToString();

        // Assert
        Assert.Equal(nameof(ToString_HasKey_ShouldBeKey), actual);
    }

    private static FileRoute GivenFileRoute()
    {
        FileRoute expected = new();
        expected.AddClaimsToRequest.Add("key1", "value1");
        expected.AddHeadersToRequest.Add("key2", "value2");
        expected.AddQueriesToRequest.Add("key3", "value3");
        expected.AuthenticationOptions.AuthenticationProviderKeys = ["value4"];
        expected.ChangeDownstreamPathTemplate.Add("key5", "value5");
        expected.DangerousAcceptAnyServerCertificateValidator = true;
        expected.DelegatingHandlers.Add("value6");
        expected.DownstreamHeaderTransform.Add("key7", "value7");
        expected.DownstreamHostAndPorts.Add(new("host8", 8));
        expected.DownstreamHttpMethod = "value9";
        expected.DownstreamHttpVersion = "value10";
        expected.DownstreamHttpVersionPolicy = "value11";
        expected.DownstreamPathTemplate = "value12";
        expected.DownstreamScheme = "value13";
        expected.FileCacheOptions.Header = "value14";
        expected.HttpHandlerOptions.MaxConnectionsPerServer = 15;
        expected.Key = "value16";
        expected.LoadBalancerOptions.Key = "value17";
        expected.Metadata.Add("key18", "value18");
        expected.Priority = 19;
        expected.QoSOptions.DurationOfBreak = 20;
        expected.RateLimitOptions ??= new() { Period = "value21" };
        expected.RequestIdKey = "value22";
        expected.RouteClaimsRequirement.Add("key23", "value23");
        expected.RouteIsCaseSensitive = true;
        expected.SecurityOptions.IPAllowedList.Add("value24");
        expected.ServiceName = "test-service";
        expected.ServiceNamespace = "test-namespace";
        expected.Timeout = 27;
        expected.UpstreamHeaderTemplates.Add("key28", "value28");
        expected.UpstreamHeaderTransform.Add("key29", "value29");
        expected.UpstreamHost = "value30";
        expected.UpstreamHttpMethod.Add("value31");
        expected.UpstreamPathTemplate = "value32";
        return expected;
    }

    private static void AssertEquality(FileRoute actual, FileRoute expected)
    {
        Assert.Equal(expected.AddClaimsToRequest, actual.AddClaimsToRequest);
        Assert.Equal(expected.AddHeadersToRequest, actual.AddHeadersToRequest);
        Assert.Equal(expected.AddQueriesToRequest, actual.AddQueriesToRequest);
        Assert.Equivalent(expected.AuthenticationOptions, actual.AuthenticationOptions); // FileAuthenticationOptions requires Equals overriding
        Assert.Equal(expected.ChangeDownstreamPathTemplate, actual.ChangeDownstreamPathTemplate);
        Assert.Equal(expected.DangerousAcceptAnyServerCertificateValidator, actual.DangerousAcceptAnyServerCertificateValidator);
        Assert.Equal(expected.DelegatingHandlers, actual.DelegatingHandlers);
        Assert.Equal(expected.DownstreamHeaderTransform, actual.DownstreamHeaderTransform);
        Assert.Equivalent(expected.DownstreamHostAndPorts, actual.DownstreamHostAndPorts); // FileHostAndPort requires Equals overriding
        Assert.Equal(expected.DownstreamHttpMethod, actual.DownstreamHttpMethod);
        Assert.Equal(expected.DownstreamHttpVersion, actual.DownstreamHttpVersion);
        Assert.Equal(expected.DownstreamHttpVersionPolicy, actual.DownstreamHttpVersionPolicy);
        Assert.Equal(expected.DownstreamPathTemplate, actual.DownstreamPathTemplate);
        Assert.Equal(expected.DownstreamScheme, actual.DownstreamScheme);
        Assert.Equivalent(expected.FileCacheOptions, actual.FileCacheOptions); // FileCacheOptions requires Equals overriding
        Assert.Equivalent(expected.HttpHandlerOptions, actual.HttpHandlerOptions); // FileHttpHandlerOptions requires Equals overriding
        Assert.Equal(expected.Key, actual.Key);
        Assert.Equivalent(expected.LoadBalancerOptions, actual.LoadBalancerOptions); // FileLoadBalancerOptions requires Equals overriding
        Assert.Equal(expected.Metadata, actual.Metadata);
        Assert.Equal(expected.Priority, actual.Priority);
        Assert.Equivalent(expected.QoSOptions, actual.QoSOptions); // FileQoSOptions requires Equals overriding
        Assert.Equivalent(expected.RateLimitOptions, actual.RateLimitOptions); // FileRateLimitByHeaderRule requires Equals overriding
        Assert.Equal(expected.RequestIdKey, actual.RequestIdKey);
        Assert.Equal(expected.RouteClaimsRequirement, actual.RouteClaimsRequirement);
        Assert.Equal(expected.RouteIsCaseSensitive, actual.RouteIsCaseSensitive);
        Assert.Equivalent(expected.SecurityOptions, actual.SecurityOptions); // FileSecurityOptions requires Equals overriding
        Assert.Equal(expected.ServiceName, actual.ServiceName);
        Assert.Equal(expected.ServiceNamespace, actual.ServiceNamespace);
        Assert.Equal(expected.Timeout, actual.Timeout);
        Assert.Equal(expected.UpstreamHeaderTemplates, actual.UpstreamHeaderTemplates);
        Assert.Equal(expected.UpstreamHeaderTransform, actual.UpstreamHeaderTransform);
        Assert.Equal(expected.UpstreamHost, actual.UpstreamHost);
        Assert.Equal(expected.UpstreamHttpMethod, actual.UpstreamHttpMethod);
        Assert.Equal(expected.UpstreamPathTemplate, actual.UpstreamPathTemplate);
    }
}
