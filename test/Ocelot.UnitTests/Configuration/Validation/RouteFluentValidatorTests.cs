using FluentValidation.Results;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Validator;
using System.Reflection;

namespace Ocelot.UnitTests.Configuration.Validation;

public class RouteFluentValidatorTests : UnitTest
{
    private readonly RouteFluentValidator _validator;
    private readonly Mock<IAuthenticationSchemeProvider> _authProvider;
    private readonly Mock<IServiceProvider> _serviceProvider;

    public RouteFluentValidatorTests()
    {
        _authProvider = new Mock<IAuthenticationSchemeProvider>();
        _serviceProvider = new Mock<IServiceProvider>();

        // TODO - replace with mocks
        _validator = new RouteFluentValidator(
            new HostAndPortValidator(),
            new FileQoSOptionsFluentValidator(_serviceProvider.Object),
            new FileAuthenticationOptionsValidator(_authProvider.Object));
    }

    [Fact]
    public async Task Downstream_path_template_should_not_be_empty()
    {
        // Arrange
        var route = new FileRoute();

        // Act
        var result = await _validator.ValidateAsync(route);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ThenTheErrorsContains("Downstream Path Template cannot be empty");
    }

    [Fact]
    public async Task Upstream_path_template_should_not_be_empty()
    {
        // Arrange
        var route = new FileRoute
        {
            DownstreamPathTemplate = "test",
        };

        // Act
        var result = await _validator.ValidateAsync(route);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ThenTheErrorsContains("Upstream Path Template cannot be empty");
    }

    [Fact]
    public async Task Downstream_path_template_should_start_with_forward_slash()
    {
        // Arrange
        var route = new FileRoute
        {
            DownstreamPathTemplate = "test",
        };

        // Act
        var result = await _validator.ValidateAsync(route);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ThenTheErrorsContains("Downstream Path Template test doesnt start with forward slash");
    }

    [Fact]
    public async Task Downstream_path_template_should_not_contain_double_forward_slash()
    {
        // Arrange
        var route = new FileRoute
        {
            DownstreamPathTemplate = "//test",
        };

        // Act
        var result = await _validator.ValidateAsync(route);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ThenTheErrorsContains("Downstream Path Template //test contains double forward slash, Ocelot does not support this at the moment. Please raise an issue in GitHib if you need this feature.");
    }

    [Theory]
    [InlineData("https://test")]
    [InlineData("http://test")]
    [InlineData("/test/http://")]
    [InlineData("/test/https://")]
    public async Task Downstream_path_template_should_not_contain_scheme(string downstreamPathTemplate)
    {
        // Arrange
        var route = new FileRoute
        {
            DownstreamPathTemplate = downstreamPathTemplate,
        };

        // Act
        var result = await _validator.ValidateAsync(route);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ThenTheErrorsContains($"Downstream Path Template {downstreamPathTemplate} contains double forward slash, Ocelot does not support this at the moment. Please raise an issue in GitHib if you need this feature.");
    }

    [Fact]
    public async Task Upstream_path_template_should_start_with_forward_slash()
    {
        // Arrange
        var route = new FileRoute
        {
            DownstreamPathTemplate = "/test",
            UpstreamPathTemplate = "test",
        };

        // Act
        var result = await _validator.ValidateAsync(route);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ThenTheErrorsContains("Upstream Path Template test doesnt start with forward slash");
    }

    [Fact]
    public async Task Upstream_path_template_should_not_contain_double_forward_slash()
    {
        // Arrange
        var route = new FileRoute
        {
            DownstreamPathTemplate = "/test",
            UpstreamPathTemplate = "//test",
        };

        // Act
        var result = await _validator.ValidateAsync(route);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ThenTheErrorsContains("Upstream Path Template //test contains double forward slash, Ocelot does not support this at the moment. Please raise an issue in GitHib if you need this feature.");
    }

    [Theory]
    [InlineData("https://test")]
    [InlineData("http://test")]
    [InlineData("/test/http://")]
    [InlineData("/test/https://")]
    public async Task Upstream_path_template_should_not_contain_scheme(string upstreamPathTemplate)
    {
        // Arrange
        var route = new FileRoute
        {
            DownstreamPathTemplate = "/test",
            UpstreamPathTemplate = upstreamPathTemplate,
        };

        // Act
        var result = await _validator.ValidateAsync(route);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ThenTheErrorsContains($"Upstream Path Template {upstreamPathTemplate} contains double forward slash, Ocelot does not support this at the moment. Please raise an issue in GitHib if you need this feature.");
    }

    [Theory]
    [Trait("Feat", "1229")]
    [Trait("PR", "2294")]
    [InlineData(null, true, null)]
    [InlineData(-1L, false, "RateLimitOptions.Limit is negative or zero for the route /test")]
    [InlineData(0L, false, "RateLimitOptions.Limit is negative or zero for the route /test")]
    [InlineData(1L, true, null)]
    public async Task ShouldValidate_FileRateLimitByHeaderRule_Limit(long? limit, bool valid, string errorMessage)
    {
        // Arrange
        var route = GivenRoute();
        route.RateLimitOptions = new()
        {
            Limit = limit,
            Period = "1s",
        };

        // Act
        var result = await _validator.ValidateAsync(route);

        // Assert
        result.IsValid.ShouldBe(valid);
        if (!result.IsValid)
            result.ThenSingleErrorIs(errorMessage);
    }

    [Fact]
    public async Task Should_not_be_valid_if_enable_rate_limiting_true_and_period_is_empty()
    {
        // Arrange
        var route = GivenRoute();
        route.RateLimitOptions = new()
        {
            EnableRateLimiting = true,
        };

        // Act
        var result = await _validator.ValidateAsync(route);

        // Assert
        result.ThenTheErrorsContains("RateLimitOptions.Period is empty");
        result.ThenTheErrorsContains("RateLimitOptions.Period does not contain integer then ms (millisecond), s (second), m (minute), h (hour), d (day) e.g. 1m for 1 minute period");
    }

    [Fact]
    public async Task Should_not_be_valid_if_enable_rate_limiting_true_and_period_has_value()
    {
        // Arrange
        var route = GivenRoute();
        route.RateLimitOptions = new()
        {
            Period = "test",
        };

        // Act
        var result = await _validator.ValidateAsync(route);

        // Assert
        result.ThenSingleErrorIs("RateLimitOptions.Period does not contain integer then ms (millisecond), s (second), m (minute), h (hour), d (day) e.g. 1m for 1 minute period");
    }

    [Theory]
    [Trait("Feat", "1229")]
    [Trait("PR", "2294")]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("1s", true)]
    [InlineData("12.34s", true)]
    [InlineData("1ms", true)]
    [InlineData("12.34ms", true)]
    [InlineData("2m", true)]
    [InlineData("23.45m", true)]
    [InlineData("3h", true)]
    [InlineData("34.56h", true)]
    [InlineData("4d", true)]
    [InlineData("4.5d", true)]
    [InlineData("123", false)]
    [InlineData("-123", false)]
    [InlineData("bad", false)]
    [InlineData(" 3s ", true)]
    [InlineData(" -3s ", false)]
    public void IsValidPeriod_ReflectionLifeHack_BranchesAreCovered(string period, bool expected)
    {
        // Arrange
        var method = _validator.GetType().GetMethod("IsValidPeriod", BindingFlags.NonPublic | BindingFlags.Static);
        var argument = new FileRateLimitByHeaderRule { Period = period };

        // Act
        bool actual = (bool)method.Invoke(_validator, new object[] { argument });

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task Should_not_be_valid_if_specified_authentication_provider_isnt_registered()
    {
        // Arrange
        var route = new FileRoute
        {
            DownstreamPathTemplate = "/test",
            UpstreamPathTemplate = "/test",
            AuthenticationOptions = new FileAuthenticationOptions
            {
                AuthenticationProviderKey = "JwtLads",
            },
        };

        // Act
        var result = await _validator.ValidateAsync(route);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ThenTheErrorsContains("AuthenticationOptions: AllowAnonymous:False,AllowedScopes:[],AuthenticationProviderKey:'JwtLads',AuthenticationProviderKeys:[] is unsupported authentication provider");
    }

    [Fact]
    public async Task Should_not_be_valid_if_not_using_service_discovery_and_no_host_and_ports()
    {
        // Arrange
        var route = new FileRoute
        {
            DownstreamPathTemplate = "/test",
            UpstreamPathTemplate = "/test",
        };

        // Act
        var result = await _validator.ValidateAsync(route);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ThenTheErrorsContains("When not using service discovery DownstreamHostAndPorts must be set and not empty or Ocelot cannot find your service!");
    }

    [Fact]
    public async Task Should_be_valid_if_using_service_discovery_and_no_host_and_ports()
    {
        // Arrange
        var route = new FileRoute
        {
            DownstreamPathTemplate = "/test",
            UpstreamPathTemplate = "/test",
            ServiceName = "Lads",
        };

        // Act
        var result = await _validator.ValidateAsync(route);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_be_valid_re_route_using_host_and_port_and_paths()
    {
        // Arrange
        var route = new FileRoute
        {
            DownstreamPathTemplate = "/test",
            UpstreamPathTemplate = "/test",
            DownstreamHostAndPorts = new List<FileHostAndPort>
            {
                new()
                {
                    Host = "localhost",
                    Port = 5000,
                },
            },
        };

        // Act
        var result = await _validator.ValidateAsync(route);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_be_valid_if_specified_authentication_provider_is_registered()
    {
        // Arrange
        const string key = "JwtLads";
        var route = new FileRoute
        {
            DownstreamPathTemplate = "/test",
            UpstreamPathTemplate = "/test",
            AuthenticationOptions = new FileAuthenticationOptions
            {
                AuthenticationProviderKey = key,
            },
            DownstreamHostAndPorts = new List<FileHostAndPort>
            {
                new()
                {
                    Host = "localhost",
                    Port = 5000,
                },
            },
        };
        GivenAnAuthProvider(key);

        // Act
        var result = await _validator.ValidateAsync(route);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("1.1")]
    [InlineData("2.0")]
    [InlineData("1,0")]
    [InlineData("1,1")]
    [InlineData("2,0")]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Should_be_valid_re_route_using_downstream_http_version(string version)
    {
        // Arrange
        var route = new FileRoute
        {
            DownstreamPathTemplate = "/test",
            UpstreamPathTemplate = "/test",
            DownstreamHostAndPorts = new List<FileHostAndPort>
            {
                new()
                {
                    Host = "localhost",
                    Port = 5000,
                },
            },
            DownstreamHttpVersion = version,
        };

        // Act
        var result = await _validator.ValidateAsync(route);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("retg1.1")]
    [InlineData("re2.0")]
    [InlineData("1,0a")]
    [InlineData("a1,1")]
    [InlineData("12,0")]
    [InlineData("asdf")]
    public async Task Should_be_invalid_re_route_using_downstream_http_version(string version)
    {
        // Arrange
        var route = new FileRoute
        {
            DownstreamPathTemplate = "/test",
            UpstreamPathTemplate = "/test",
            DownstreamHostAndPorts = new List<FileHostAndPort>
            {
                new()
                {
                    Host = "localhost",
                    Port = 5000,
                },
            },
            DownstreamHttpVersion = version,
        };

        // Act
        var result = await _validator.ValidateAsync(route);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ThenTheErrorsContains("'Downstream Http Version'"); // this error message changes depending on the OS language
    }

    private static FileRoute GivenRoute() => new()
    {
        DownstreamPathTemplate = "/test",
        UpstreamPathTemplate = "/test",
        DownstreamHostAndPorts = new()
        {
            new("localhost", 5000),
        },
    };

    private void GivenAnAuthProvider(string key)
    {
        var schemes = new List<AuthenticationScheme>
        {
            new(key, key, typeof(FakeAutheHandler)),
        };

        _authProvider
            .Setup(x => x.GetAllSchemesAsync())
            .ReturnsAsync(schemes);
    }

    private class FakeAutheHandler : IAuthenticationHandler
    {
        public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
        {
            throw new System.NotImplementedException();
        }

        public Task<AuthenticateResult> AuthenticateAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task ChallengeAsync(AuthenticationProperties properties)
        {
            throw new System.NotImplementedException();
        }

        public Task ForbidAsync(AuthenticationProperties properties)
        {
            throw new System.NotImplementedException();
        }
    }
}

static class ValidationResultExtensions
{
    public static void ThenTheErrorsContains(this ValidationResult result, string expected)
        => result.Errors.ShouldContain(x => x.ErrorMessage.Contains(expected));

    public static void ThenSingleErrorIs(this ValidationResult result, string message)
    {
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal(message, result.Errors[0].ErrorMessage);
    }
}
