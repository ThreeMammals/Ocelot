using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.RateLimiting;
using System.Reflection;

namespace Ocelot.UnitTests.RateLimiting;

public class RateLimitOptionsCreatorTests : UnitTest
{
    private readonly RateLimitOptionsCreator _creator = new(Mock.Of<IRateLimiting>());

    [Fact]
    [Trait("PR", "58")] // https://github.com/ThreeMammals/Ocelot/pull/58
    [Trait("Release", "1.4.0")]
    public void Should_create_rate_limit_options()
    {
        // Arrange
        var route = new FileRoute
        {
            RateLimitOptions = new()
            {
                ClientWhitelist = new List<string>(),
                Period = "Period",
                Limit = 1,
                Wait = "OneSecond",
                EnableRateLimiting = true,
            },
        };
        var globalConfig = new FileGlobalConfiguration
        {
            RateLimitOptions = new()
            {
                ClientIdHeader = "ClientIdHeader",
                EnableHeaders = true,
                QuotaExceededMessage = "QuotaMessage",
                RateLimitCounterPrefix = "RateLimitCounterPrefix",
                HttpStatusCode = 200,
            },
        };
        var options = route.RateLimitOptions;
        RateLimitOptions expected = new()
        {
            ClientIdHeader = "ClientIdHeader",
            ClientWhitelist = options.ClientWhitelist,
            EnableHeaders = true,
            EnableRateLimiting = true,
            StatusCode = 200,
            QuotaMessage = "QuotaMessage",
            KeyPrefix = "RateLimitCounterPrefix",
            Rule = new(options.Period, options.Wait, options.Limit.Value),
        };
        bool enabled = true;

        // Act
        var result = _creator.Create(route, globalConfig);

        // Assert
        enabled.ShouldBeTrue();
        result.ClientIdHeader.ShouldBe(expected.ClientIdHeader);
        result.ClientWhitelist.ShouldBe(expected.ClientWhitelist);
        result.EnableHeaders.ShouldBe(expected.EnableHeaders);
        result.EnableRateLimiting.ShouldBe(expected.EnableRateLimiting);
        result.StatusCode.ShouldBe(expected.StatusCode);
        result.QuotaMessage.ShouldBe(expected.QuotaMessage);
        result.KeyPrefix.ShouldBe(expected.KeyPrefix);
        result.Rule.Limit.ShouldBe(expected.Rule.Limit);
        result.Rule.Period.ShouldBe(expected.Rule.Period);
        result.Rule.Wait.ShouldBe(expected.Rule.Wait);
    }

    #region PR 2294
    [Fact]
    [Trait("Feat", "1229")] // https://github.com/ThreeMammals/Ocelot/issues/1229
    [Trait("PR", "2294")] // https://github.com/ThreeMammals/Ocelot/pull/2294
    public void Create_ArgumentNullChecks()
    {
        // Arrange, Act, Assert
        Assert.Throws<ArgumentNullException>(() => _creator.Create(null, new()));
        Assert.Throws<ArgumentNullException>(() => _creator.Create(new FileRoute(), null));
    }

    [Fact]
    [Trait("Feat", "1229")]
    [Trait("PR", "2294")]
    public void Create_GlobalRouteKeysCollectionIsNull_IsGlobalDefaultsToTrue()
    {
        // Arrange, Act, Assert: branch 1
        FileRoute route = new();
        FileGlobalConfiguration global = new();
        var actual = _creator.Create(route, global);
        Assert.NotNull(actual);
        Assert.False(actual.EnableRateLimiting);

        // Arrange, Act, Assert: branch 2
        global.RateLimitOptions = new(); // -> RouteKeys is null
        actual = _creator.Create(route, global);
        Assert.NotNull(actual);
        Assert.True(actual.EnableRateLimiting);
    }

    [Theory]
    [Trait("Feat", "1229")]
    [Trait("PR", "2294")]
    [InlineData(false)]
    [InlineData(true)]
    public void Create_GlobalRouteKeys_ContainsRouteKey(bool contains)
    {
        // Arrange
        FileRoute route = new() { Key = contains ? "R1" : "?" };
        FileGlobalConfiguration global = new()
        {
            RateLimitOptions = new()
            {
                RouteKeys = ["R1"],
                EnableRateLimiting = true,
            },
        };

        // Act
        var actual = _creator.Create(route, global);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(contains, actual.EnableRateLimiting);
    }

    [Theory]
    [Trait("Feat", "1229")]
    [Trait("PR", "2294")]
    [InlineData(null, null, true)]
    [InlineData(false, null, false)]
    [InlineData(null, false, false)]
    [InlineData(false, false, false)]
    public void Create_DisabledRateLimiting(bool? ruleEnableRateLimiting, bool? globalEnableRateLimiting, bool expected)
    {
        // Arrange
        FileRoute route = new()
        {
            RateLimitOptions = new() { EnableRateLimiting = ruleEnableRateLimiting },
        };
        FileGlobalConfiguration global = new()
        {
            RateLimitOptions = new() { EnableRateLimiting = globalEnableRateLimiting },
        };

        // Act
        var actual = _creator.Create(route, global);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(expected, actual.EnableRateLimiting);
    }

    [Theory]
    [Trait("Feat", "1229")]
    [Trait("PR", "2294")]
    [InlineData(true, null, false, "rule")]
    [InlineData(null, true, true, "global")]
    [InlineData(true, true, false, "rule")]
    [InlineData(true, true, true, "rule")]
    [InlineData(null, null, true, "Oc-Client")]
    public void Create_ByHeaderRules(bool? hasRule, bool? hasGlobal, bool isGlobal, string expected)
    {
        // Arrange
        FileRoute route = new()
        {
            Key = "R1",
            RateLimitOptions = !hasRule.HasValue ? null : new() { ClientIdHeader = "rule" },
        };
        FileGlobalConfiguration global = new()
        {
            RateLimitOptions = !hasGlobal.HasValue ? null :
                new() { RouteKeys = [isGlobal ? "R1" : "?"], ClientIdHeader = "global" },
        };

        // Act
        var actual = _creator.Create(route, global);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(expected, actual.ClientIdHeader);
    }

    [Fact]
    [Trait("Feat", "1229")]
    [Trait("PR", "2294")]
    public void MergeHeaderRules_ArgumentNullChecks()
    {
        // Arrange
        MethodInfo method = _creator.GetType().GetMethod("MergeHeaderRules", BindingFlags.Instance | BindingFlags.NonPublic);
        FileRateLimitByHeaderRule rule = new();
        FileGlobalRateLimitByHeaderRule global = new();

        // Act
        var ex1 = Assert.Throws<TargetInvocationException>(() => method?.Invoke(_creator, [null, global]));
        var ex2 = Assert.Throws<TargetInvocationException>(() => method?.Invoke(_creator, [rule, null]));

        // Assert
        Assert.NotNull(ex1.InnerException);
        Assert.True(ex1.InnerException is ArgumentNullException);
        Assert.Equal(nameof(rule), ((ArgumentNullException)ex1.InnerException).ParamName);
        Assert.NotNull(ex2.InnerException);
        Assert.True(ex2.InnerException is ArgumentNullException);
        Assert.Equal(nameof(global), ((ArgumentNullException)ex2.InnerException).ParamName);
    }

    [Fact]
    [Trait("Feat", "1229")]
    [Trait("PR", "2294")]
    public void MergeHeaderRules_FromGlobal()
    {
        // Arrange
        FileRateLimitByHeaderRule rule = new();
        FileGlobalRateLimitByHeaderRule global = new()
        {
            ClientIdHeader = "111",
            ClientWhitelist = ["222"],
            DisableRateLimitHeaders = null,
            EnableHeaders = false,
            EnableRateLimiting = true,
            HttpStatusCode = 300,
            StatusCode = 400,
            QuotaExceededMessage = "55",
            QuotaMessage = "66",
            RateLimitCounterPrefix = "77",
            KeyPrefix = "88",
            Period = "9s",
            PeriodTimespan = 10.0D,
            Wait = "11s",
            Limit = 12,
        };
        MethodInfo method = _creator.GetType().GetMethod("MergeHeaderRules", BindingFlags.Instance | BindingFlags.NonPublic);

        // Act
        var actual = method?.Invoke(_creator, [rule, global]) as RateLimitOptions;

        // Assert
        Assert.NotNull(actual);
        Assert.Equal("111", actual.ClientIdHeader);
        Assert.Contains("222", actual.ClientWhitelist);
        Assert.False(actual.EnableHeaders);
        Assert.True(actual.EnableRateLimiting);
        Assert.Equal(300, actual.StatusCode);
        Assert.Equal("55", actual.QuotaMessage);
        Assert.Equal("77", actual.KeyPrefix);
        Assert.Equal("12/9s/w10s", actual.Rule.ToString());
    }

    [Fact]
    public void Create_RateLimiting_ByMethod()
    {
        // Arrange, Act, Assert: scenario 1
        FileRoute route = new();
        FileGlobalConfiguration global = new()
        {
            RateLimiting = new() { ByMethod = [] },
        };
        var actual = _creator.Create(route, global);
        Assert.NotNull(actual);
        Assert.False(actual.EnableRateLimiting);

        // Arrange, Act, Assert: scenario 2
        route.UpstreamPathTemplate = "/MiladRivandi";
        global.RateLimiting.ByMethod = new FileGlobalRateLimit[]
        {
            new() { Pattern = "/Milad*" },
        };
        actual = _creator.Create(route, global);
        Assert.NotNull(actual);
        Assert.True(actual.EnableRateLimiting);
    }
    #endregion
}
