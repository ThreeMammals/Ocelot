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
    private readonly FileGlobalConfiguration _global;
    private HeaderTransformations _result;
    private readonly Mock<IPlaceholders> _placeholders;
    private readonly Mock<IOcelotLoggerFactory> _factory;
    private readonly Mock<IOcelotLogger> _logger;
    private readonly List<Func<string>> _messages = new();

    public HeaderFindAndReplaceCreatorTests()
    {
        _logger = new Mock<IOcelotLogger>();
        _logger.Setup(x => x.LogWarning(It.IsAny<Func<string>>()))
            .Callback<Func<string>>(_messages.Add);

        _factory = new Mock<IOcelotLoggerFactory>();
        _factory.Setup(x => x.CreateLogger<HeaderFindAndReplaceCreator>()).Returns(_logger.Object);
        _placeholders = new Mock<IPlaceholders>();

        _global = new FileGlobalConfiguration();
        _global.UpstreamHeaderTransform.Add("TestGlobal", "Test, Chicken");
        _global.UpstreamHeaderTransform.Add("MoopGlobal", "o, a");
        _global.DownstreamHeaderTransform.Add("PopGlobal", "West, East");
        _global.DownstreamHeaderTransform.Add("BopGlobal", "e, r");

        var options = new Mock<IOptions<FileGlobalConfiguration>>();
        options.Setup(x => x.Value).Returns(_global);
        _creator = new HeaderFindAndReplaceCreator(_placeholders.Object, _factory.Object, options.Object);
    }

    [Fact]
    [Trait("Feat", "204")] // https://github.com/ThreeMammals/Ocelot/pull/204
    [Trait("Feat", "1658")] // https://github.com/ThreeMammals/Ocelot/issues/1658
    [Trait("PR", "1659")] // https://github.com/ThreeMammals/Ocelot/pull/1659
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
            new(_global.DownstreamHeaderTransform.First()),
            new(_global.DownstreamHeaderTransform.Last()),
        };

        // Act
        _result = _creator.Create(route);

        // Assert
        ThenTheFollowingUpstreamIsReturned(upstream);
        ThenTheFollowingDownstreamIsReturned(downstream);
    }

    [Fact]
    [Trait("Feat", "1658")]
    public void Create_WithRouteAndWithoutGlobalConfigurationParam_GlobalConfigurationInjectionIsReused()
    {
        // Arrange
        var route = new FileRoute(); // no data
        var upstream = new List<HeaderFindAndReplace>
        {
            new(_global.UpstreamHeaderTransform.First()),
            new(_global.UpstreamHeaderTransform.Last()),
        };
        var downstream = new List<HeaderFindAndReplace>
        {
            new(_global.DownstreamHeaderTransform.First()),
            new(_global.DownstreamHeaderTransform.Last()),
        };

        // Act
        _result = _creator.Create(route, null);

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

        // Act
        _result = _creator.Create(route);

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
            new(_global.DownstreamHeaderTransform.First()),
            new(_global.DownstreamHeaderTransform.Last()),
        };
        GivenThePlaceholderIs("http://ocelot.com/");

        // Act
        _result = _creator.Create(route);

        // Assert
        ThenTheFollowingDownstreamIsReturned(downstream);
    }

    [Fact]
    [Trait("Feat", "204")]
    [Trait("Feat", "1658")]
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
            new(_global.DownstreamHeaderTransform.First()),
            new(_global.DownstreamHeaderTransform.Last()),
        };
        var expectedUpstream = new List<HeaderFindAndReplace>
        {
            new("TestGlobal", "Test", "Chicken", 0),
            new("MoopGlobal", "o", "a", 0),
        };
        GivenTheBaseUrlErrors();

        // Act
        _result = _creator.Create(route);

        // Assert
        ThenTheFollowingDownstreamIsReturned(expectedDownstream);
        ThenTheFollowingUpstreamIsReturned(expectedUpstream);
        ThenTheLoggerIsCalledCorrectly(4,
            "HeaderFindAndReplace was not mapped from [Location, http://www.bbc.co.uk/, {BaseUrl}] due to UnknownError: blahh",
            "Unable to add UpstreamHeaderTransform [Location, http://www.bbc.co.uk/, {BaseUrl}]",
            "HeaderFindAndReplace was not mapped from [Location, http://www.bbc.co.uk/, {BaseUrl}] due to UnknownError: blahh",
            "Unable to add DownstreamHeaderTransform [Location, http://www.bbc.co.uk/, {BaseUrl}]");
    }

    private void ThenTheLoggerIsCalledCorrectly(int times, params string[] messages)
    {
        _logger.Verify(x => x.LogWarning(It.IsAny<Func<string>>()), Times.Exactly(times));
        _messages.ShouldNotBeEmpty();
        var actual = _messages.Select(f => f.Invoke()).ToList();
        foreach (var expected in messages)
        {
            actual.ShouldContain(expected);
        }
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
            new(_global.DownstreamHeaderTransform.First()),
            new(_global.DownstreamHeaderTransform.Last()),
        };
        GivenThePlaceholderIs("http://ocelot.com/");

        // Act
        _result = _creator.Create(route);

        // Assert
        ThenTheFollowingDownstreamIsReturned(downstream);
    }

    [Fact]
    [Trait("Feat", "204")]
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
            new(_global.DownstreamHeaderTransform.First()),
            new(_global.DownstreamHeaderTransform.Last()),
        };
        GivenThePlaceholderIs("ocelot.next");

        // Act
        _result = _creator.Create(route);

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
        GivenThePlaceholderIs("http://ocelot.com/");

        // Act
        _result = _creator.Create(route);

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

        // Act
        _result = _creator.Create(route);

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

        // Act
        _result = _creator.Create(route);

        // Assert
        ThenTheFollowingAddHeaderToUpstreamIsReturned(expected);
    }

    [Fact]
    [Trait("PR", "1659")]
    [Trait("Feat", "1658")]
    public void Merge_ShouldMergeGlobalIntoRouteOpts()
    {
        // Arrange
        var routeTransforms = new Dictionary<string, string>()
        {
            { "B", "routeB" },
            { "C", "routeC" },
        };
        var globalTransforms = new Dictionary<string, string>()
        {
            { "A", "globalA" },
            { "B", "globalB" },
        };

        // Act
        var actual = HeaderFindAndReplaceCreator.Merge(routeTransforms, globalTransforms);

        // Assert
        actual.ShouldNotBeNull();
        var dictionary = actual.ToDictionary(x => x.Key, x => x.Value);
        dictionary.Count.ShouldBe(3);
        dictionary.ContainsKey("A").ShouldBeTrue();
        dictionary["A"].ShouldBe("globalA");
        dictionary.ContainsKey("B").ShouldBeTrue();
        dictionary["B"].ShouldBe("routeB"); // local value wins over global one
        dictionary.ContainsKey("C").ShouldBeTrue();
        dictionary["C"].ShouldBe("routeC");
    }

    [Fact]
    [Trait("PR", "1659")]
    [Trait("Feat", "1658")]
    public void Merge_NullParams_NullChecksHaveBeenPerformed()
    {
        // Arrange, Act
        var actual = HeaderFindAndReplaceCreator.Merge(null, null);

        // Assert
        actual.ShouldNotBeNull().ShouldBeEmpty();
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
