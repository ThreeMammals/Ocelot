using Microsoft.Extensions.Options;
using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Infrastructure;
using Ocelot.Logging;
using Ocelot.Responses;
using Ocelot.UnitTests.Responder;

namespace Ocelot.UnitTests.Configuration;

public class HeaderFindAndReplaceCreatorTests : UnitTest
{
    private readonly HeaderFindAndReplaceCreator _creator;
    private FileRoute _route;
    private HeaderTransformations _result;
    private readonly Mock<IPlaceholders> _placeholders;
    private readonly Mock<IOcelotLoggerFactory> _factory;
    private readonly Mock<IOcelotLogger> _logger;

    public HeaderFindAndReplaceCreatorTests()
    {
        _logger = new Mock<IOcelotLogger>();
        _factory = new Mock<IOcelotLoggerFactory>();
        _factory.Setup(x => x.CreateLogger<HeaderFindAndReplaceCreator>()).Returns(_logger.Object);
        _placeholders = new Mock<IPlaceholders>();
        var fileGlobalConfiguration = new FileGlobalConfiguration();
        fileGlobalConfiguration.UpstreamHeaderTransform.Add("TestGlobal", "Test, Chicken");
        fileGlobalConfiguration.UpstreamHeaderTransform.Add("MoopGlobal", "o, a");
        fileGlobalConfiguration.DownstreamHeaderTransform.Add("PopGlobal", "West, East");
        fileGlobalConfiguration.DownstreamHeaderTransform.Add("BopGlobal", "e, r");

        var options = new Mock<IOptions<FileConfiguration>>();
        options.Setup(x => x.Value).Returns(new FileConfiguration
        {
            GlobalConfiguration = fileGlobalConfiguration,
        });

        _creator = new HeaderFindAndReplaceCreator(options.Object, _placeholders.Object, _factory.Object);
    }

    [Fact]
    public void Should_create()
    {
        // Arrange
        var route = new FileRoute
        {
            UpstreamHeaderTransform = new Dictionary<string, string>
            {
                {"Test", "Test, Chicken"},
                {"Moop", "o, a"},
            },
            DownstreamHeaderTransform = new Dictionary<string, string>
            {
                {"Pop", "West, East"},
                {"Bop", "e, r"},
            },
        };
        var upstream = new List<HeaderFindAndReplace>
        {
            new("Test", "Test", "Chicken", 0),
            new("Moop", "o", "a", 0),
            new("TestGlobal", "Test", "Chicken", 0),
            new("MoopGlobal", "o", "a", 0),
        };
        var downstream = new List<HeaderFindAndReplace>
        {
            new("Pop", "West", "East", 0),
            new("Bop", "e", "r", 0),
            new("PopGlobal", "West", "East", 0),
            new("BopGlobal", "e", "r", 0),
        };
        GivenTheRoute(route);

        // Act
        WhenICreate();

        // Assert
        ThenTheFollowingUpstreamIsReturned(upstream);
        ThenTheFollowingDownstreamIsReturned(downstream);
    }

    [Fact]
    public void Should_create_with_add_headers_to_request()
    {
        // Arrange
        const string key = "X-Forwarded-For";
        const string value = "{RemoteIpAddress}";
        var route = new FileRoute
        {
            UpstreamHeaderTransform = new Dictionary<string, string>
            {
                {key, value},
            },
        };
        var expected = new AddHeader(key, value);
        GivenTheRoute(route);

        // Act
        WhenICreate();

        // Assert
        ThenTheFollowingAddHeaderToUpstreamIsReturned(expected);
    }

    [Fact]
    public void Should_use_base_url_placeholder()
    {
        // Arrange
        var route = new FileRoute
        {
            DownstreamHeaderTransform = new Dictionary<string, string>
            {
                {"Location", "http://www.bbc.co.uk/, {BaseUrl}"},
            },
        };
        var downstream = new List<HeaderFindAndReplace>
        {
            new("Location", "http://www.bbc.co.uk/", "http://ocelot.com/", 0),
            new("PopGlobal", "West", "East", 0),
            new("BopGlobal", "e", "r", 0),
        };
        GivenTheRoute(route);
        GivenThePlaceholderIs("http://ocelot.com/");

        // Act
        WhenICreate();

        // Assert
        ThenTheFollowingDownstreamIsReturned(downstream);
    }

    [Fact]
    public void Should_log_errors_and_not_add_headers()
    {
        // Arrange
        var route = new FileRoute
        {
            DownstreamHeaderTransform = new Dictionary<string, string>
            {
                {"Location", "http://www.bbc.co.uk/, {BaseUrl}"},
            },
            UpstreamHeaderTransform = new Dictionary<string, string>
            {
                {"Location", "http://www.bbc.co.uk/, {BaseUrl}"},
            },
        };
        var expectedDownstream = new List<HeaderFindAndReplace>
        {
            new("PopGlobal", "West", "East", 0),
            new("BopGlobal", "e", "r", 0),
        };
        var expectedUpstream = new List<HeaderFindAndReplace>
        {
            new("TestGlobal", "Test", "Chicken", 0),
            new("MoopGlobal", "o", "a", 0),
        };
        GivenTheRoute(route);
        GivenTheBaseUrlErrors();

        // Act
        WhenICreate();

        // Assert
        ThenTheFollowingDownstreamIsReturned(expectedDownstream);
        ThenTheFollowingUpstreamIsReturned(expectedUpstream);
        ThenTheLoggerIsCalledCorrectly("Unable to add DownstreamHeaderTransform Location: http://www.bbc.co.uk/, {BaseUrl}");
        ThenTheLoggerIsCalledCorrectly("Unable to add UpstreamHeaderTransform Location: http://www.bbc.co.uk/, {BaseUrl}");
    }

    private void ThenTheLoggerIsCalledCorrectly(string message)
    {
        _logger.Verify(x => x.LogWarning(It.Is<Func<string>>(y => y.Invoke() == message)), Times.Once);
    }

    [Fact]
    public void Should_use_base_url_partial_placeholder()
    {
        // Arrange
        var route = new FileRoute
        {
            DownstreamHeaderTransform = new Dictionary<string, string>
            {
                {"Location", "http://www.bbc.co.uk/pay, {BaseUrl}pay"},
            },
        };
        var downstream = new List<HeaderFindAndReplace>
        {
            new("Location", "http://www.bbc.co.uk/pay", "http://ocelot.com/pay", 0),
            new("PopGlobal", "West", "East", 0),
            new("BopGlobal", "e", "r", 0),
        };
        GivenTheRoute(route);
        GivenThePlaceholderIs("http://ocelot.com/");

        // Act
        WhenICreate();

        // Assert
        ThenTheFollowingDownstreamIsReturned(downstream);
    }

    [Fact]
    public void Should_map_with_partial_placeholder_in_the_middle()
    {
        // Arrange
        var route = new FileRoute
        {
            DownstreamHeaderTransform = new Dictionary<string, string>
            {
                {"Host-Next", "www.bbc.co.uk, subdomain.{Host}/path"},
            },
        };
        var expected = new List<HeaderFindAndReplace>
        {
            new("Host-Next", "www.bbc.co.uk", "subdomain.ocelot.next/path", 0),
        };
        GivenTheRoute(route);
        GivenThePlaceholderIs("ocelot.next");

        // Act
        WhenICreate();

        // Assert
        ThenTheFollowingDownstreamIsReturned(expected);
    }

    [Fact]
    public void Should_add_trace_id_header()
    {
        // Arrange
        var route = new FileRoute
        {
            DownstreamHeaderTransform = new Dictionary<string, string>
            {
                {"Trace-Id", "{TraceId}"},
            },
        };
        var expected = new AddHeader("Trace-Id", "{TraceId}");
        GivenTheRoute(route);
        GivenThePlaceholderIs("http://ocelot.com/");

        // Act
        WhenICreate();

        // Assert
        ThenTheFollowingAddHeaderToDownstreamIsReturned(expected);
    }

    [Fact]
    public void Should_add_downstream_header_as_is_when_no_replacement_is_given()
    {
        // Arrange
        var route = new FileRoute
        {
            DownstreamHeaderTransform = new Dictionary<string, string>
            {
                {"X-Custom-Header", "Value"},
            },
        };
        var expected = new AddHeader("X-Custom-Header", "Value");
        GivenTheRoute(route);

        // Act
        WhenICreate();

        // Assert
        ThenTheFollowingAddHeaderToDownstreamIsReturned(expected);
    }

    [Fact]
    public void Should_add_upstream_header_as_is_when_no_replacement_is_given()
    {
        // Arrange
        var route = new FileRoute
        {
            UpstreamHeaderTransform = new Dictionary<string, string>
            {
                {"X-Custom-Header", "Value"},
            },
        };
        var expected = new AddHeader("X-Custom-Header", "Value");
        GivenTheRoute(route);

        // Act
        WhenICreate();

        // Assert
        ThenTheFollowingAddHeaderToUpstreamIsReturned(expected);
    }

    [Fact]
    public void Should_merge()
    {
        // Arrange
        var local = new Dictionary<string, string>()
        {
            { "B", "localB" },
            { "C", "localC" },
        };
        var global = new Dictionary<string, string>()
        {
            { "A", "globalA" },
            { "B", "globalB" },
        };

        // Act
        var actual = HeaderFindAndReplaceCreator.Merge(local, global);

        // Assert
        actual.ShouldNotBeNull();
        var dictionary = actual.ToDictionary(x => x.Key, x => x.Value);
        dictionary.Count.ShouldBe(3);
        dictionary.ContainsKey("A").ShouldBeTrue();
        dictionary["A"].ShouldBe("globalA");
        dictionary.ContainsKey("B").ShouldBeTrue();
        dictionary["B"].ShouldBe("localB"); // local value wins over global one
        dictionary.ContainsKey("C").ShouldBeTrue();
        dictionary["C"].ShouldBe("localC");
    }

    private void GivenThePlaceholderIs(string placeholderValue)
    {
        _placeholders.Setup(x => x.Get(It.IsAny<string>())).Returns(new OkResponse<string>(placeholderValue));
    }

    private void GivenTheBaseUrlErrors()
    {
        _placeholders.Setup(x => x.Get(It.IsAny<string>())).Returns(new ErrorResponse<string>(new AnyError()));
    }

    private void ThenTheFollowingAddHeaderToDownstreamIsReturned(AddHeader addHeader)
    {
        _result.AddHeadersToDownstream[0].Key.ShouldBe(addHeader.Key);
        _result.AddHeadersToDownstream[0].Value.ShouldBe(addHeader.Value);
    }

    private void ThenTheFollowingAddHeaderToUpstreamIsReturned(AddHeader addHeader)
    {
        _result.AddHeadersToUpstream[0].Key.ShouldBe(addHeader.Key);
        _result.AddHeadersToUpstream[0].Value.ShouldBe(addHeader.Value);
    }

    private void ThenTheFollowingDownstreamIsReturned(List<HeaderFindAndReplace> downstream)
    {
        _result.Downstream.Count.ShouldBe(downstream.Count);

        for (var i = 0; i < _result.Downstream.Count; i++)
        {
            var result = _result.Downstream[i];
            var expected = downstream[i];
            result.Find.ShouldBe(expected.Find);
            result.Index.ShouldBe(expected.Index);
            result.Key.ShouldBe(expected.Key);
            result.Replace.ShouldBe(expected.Replace);
        }
    }

    private void GivenTheRoute(FileRoute route)
    {
        _route = route;
    }

    private void WhenICreate()
    {
        _result = _creator.Create(_route);
    }

    private void ThenTheFollowingUpstreamIsReturned(List<HeaderFindAndReplace> expecteds)
    {
        _result.Upstream.Count.ShouldBe(expecteds.Count);

        for (var i = 0; i < _result.Upstream.Count; i++)
        {
            var result = _result.Upstream[i];
            var expected = expecteds[i];
            result.Find.ShouldBe(expected.Find);
            result.Index.ShouldBe(expected.Index);
            result.Key.ShouldBe(expected.Key);
            result.Replace.ShouldBe(expected.Replace);
        }
    }
}
