using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using System.Reflection;

namespace Ocelot.UnitTests.QualityOfService;

[Trait("Feat", "23")] // https://github.com/ThreeMammals/Ocelot/issues/23
[Trait("Release", "1.3.2")] // https://github.com/ThreeMammals/Ocelot/releases/tag/1.3.2
[Trait("Commit", "b44c025")] // https://github.com/ThreeMammals/Ocelot/commit/b44c02510af9904a5253ab0a3e6f1f6be9cd8aeb
public class QoSOptionsCreatorTests : UnitTest
{
    private readonly QoSOptionsCreator _creator = new();

    [Fact]
    public void ShouldCreateQosOptions()
    {
        // Arrange
        var route = new FileRoute
        {
            QoSOptions = new FileQoSOptions
            {
                DurationOfBreak = 1,
                ExceptionsAllowedBeforeBreaking = 2,
                FailureRatio = 3.0D,
                SamplingDuration = 4,
                TimeoutValue = 5,
            },
        };
        var expected = new QoSOptions(2, 1)
        {
            FailureRatio = 3.0D,
            SamplingDuration = 4,
            Timeout = 5,
        };

        // Act
        var actual = _creator.Create(route.QoSOptions);

        // Assert
        AssertEquality(actual, expected);
    }

    #region PR 2081
    [Fact]
    [Trait("PR", "2081")] // https://github.com/ThreeMammals/Ocelot/pull/2081
    [Trait("Feat", "2080")] // https://github.com/ThreeMammals/Ocelot/issues/2080
    public void NoRouteOptions_ShouldCreateFromGlobalQosOptions()
    {
        // Arrange
        FileGlobalConfiguration global = new()
        {
            QoSOptions = new()
            {
                DurationOfBreak = 1,
                ExceptionsAllowedBeforeBreaking = 2,
                FailureRatio = 3.0D,
                SamplingDuration = 4,
                TimeoutValue = 5,
            },
        };
        FileRoute route = new();
        QoSOptions expected = new(global.QoSOptions);

        // Act
        var actual = _creator.Create(route, global);

        // Assert
        Assert.Equivalent(expected, actual);
        AssertEquality(actual, expected);
    }

    [Fact]
    [Trait("PR", "2081")] // https://github.com/ThreeMammals/Ocelot/pull/2081
    [Trait("Feat", "2080")] // https://github.com/ThreeMammals/Ocelot/issues/2080
    public void HasRouteOptions_ShouldCreateFromRouteQosOptions()
    {
        // Arrange
        FileGlobalConfiguration global = new()
        {
            QoSOptions = new()
            {
                DurationOfBreak = 1,
                ExceptionsAllowedBeforeBreaking = 2,
                FailureRatio = 3.0D,
                SamplingDuration = 4,
                TimeoutValue = 5,
            },
        };
        FileRoute route = new()
        {
            QoSOptions = new FileQoSOptions
            {
                DurationOfBreak = 10,
                ExceptionsAllowedBeforeBreaking = 20,
                FailureRatio = 30.0D,
                SamplingDuration = 40,
                TimeoutValue = 50,
            },
        };
        QoSOptions expected = new(route.QoSOptions);

        // Act
        var actual = _creator.Create(route, global);

        // Assert
        Assert.Equivalent(expected, actual);
        AssertEquality(actual, expected);
    }

    private static void AssertEquality(QoSOptions actual, QoSOptions expected)
    {
        Assert.Equal(expected.BreakDuration, actual.BreakDuration);
        Assert.Equal(expected.MinimumThroughput, actual.MinimumThroughput);
        Assert.Equal(expected.FailureRatio, actual.FailureRatio);
        Assert.Equal(expected.SamplingDuration, actual.SamplingDuration);
        Assert.Equal(expected.Timeout, actual.Timeout);
    }
    #endregion PR 2081

    #region PR 2339
    [Fact]
    [Trait("PR", "2339")] // https://github.com/ThreeMammals/Ocelot/pull/2339
    [Trait("Feat", "2338")] // https://github.com/ThreeMammals/Ocelot/issues/2338
    public void Create_FileQoSOptions()
    {
        // Arrange
        FileQoSOptions options = new()
        {
            DurationOfBreak = 1,
            BreakDuration = 2,
            ExceptionsAllowedBeforeBreaking = 3,
            MinimumThroughput = 4,
            FailureRatio = 5,
            SamplingDuration = 6,
            TimeoutValue = 7,
            Timeout = 8,
        };

        // Act
        var actual = _creator.Create(options);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(1, actual.BreakDuration);
        Assert.Equal(3, actual.MinimumThroughput);
        Assert.Equal(5, actual.FailureRatio);
        Assert.Equal(6, actual.SamplingDuration);
        Assert.Equal(7, actual.Timeout);
    }

    [Fact]
    [Trait("PR", "2339")]
    [Trait("Feat", "2338")]
    public void Create_FileRoute_ArgNullChecks()
    {
        // Arrange
        FileRoute route = null;
        FileGlobalConfiguration globalConfiguration = null;

        // Act, Assert
        var ex = Assert.Throws<ArgumentNullException>(() => _creator.Create(route, globalConfiguration));
        Assert.Equal(nameof(route), ex.ParamName);

        // Act, Assert
        route = new();
        ex = Assert.Throws<ArgumentNullException>(() => _creator.Create(route, globalConfiguration));
        Assert.Equal(nameof(globalConfiguration), ex.ParamName);
    }

    [Fact]
    [Trait("PR", "2339")]
    [Trait("Feat", "2338")]
    public void Create_FileRoute()
    {
        // Arrange
        FileRoute route = new()
        {
            QoSOptions = new()
            {
                DurationOfBreak = 1,
                BreakDuration = 1,
                ExceptionsAllowedBeforeBreaking = 1,
                MinimumThroughput = 1,
                FailureRatio = null,
                SamplingDuration = null,
                TimeoutValue = 1,
                Timeout = 1,
            },
        };
        FileGlobalConfiguration globalConfiguration = new()
        {
            QoSOptions = new()
            {
                DurationOfBreak = 3,
                BreakDuration = 3,
                ExceptionsAllowedBeforeBreaking = 3,
                MinimumThroughput = 3,
                FailureRatio = 3,
                SamplingDuration = 3,
                TimeoutValue = 3,
                Timeout = 3,
            },
        };

        // Act
        var actual = _creator.Create(route, globalConfiguration);

        // Assert
        Assert.Equal(1, actual.BreakDuration);
        Assert.Equal(1, actual.MinimumThroughput);
        Assert.Equal(3, actual.FailureRatio); // global
        Assert.Equal(3, actual.SamplingDuration); // global
        Assert.Equal(1, actual.Timeout);
    }

    [Fact]
    [Trait("PR", "2339")]
    [Trait("Feat", "2338")]
    public void Create_FileDynamicRoute_ArgNullChecks()
    {
        // Arrange, Act, Assert
        FileDynamicRoute route = null;
        FileGlobalConfiguration globalConfiguration = null;
        var actual = Assert.Throws<ArgumentNullException>(() => _creator.Create(route, globalConfiguration));
        Assert.Equal(nameof(route), actual.ParamName);

        // Arrange, Act, Assert 2
        route = new();
        globalConfiguration = null;
        actual = Assert.Throws<ArgumentNullException>(() => _creator.Create(route, globalConfiguration));
        Assert.Equal(nameof(globalConfiguration), actual.ParamName);
    }

    [Fact]
    [Trait("PR", "2339")]
    [Trait("Feat", "2338")]
    public void Create_FileDynamicRoute()
    {
        // Arrange
        FileDynamicRoute route = new()
        {
            QoSOptions = new()
            {
                DurationOfBreak = 1,
                BreakDuration = 1,
                ExceptionsAllowedBeforeBreaking = 1,
                MinimumThroughput = 1,
                FailureRatio = null,
                SamplingDuration = null,
                TimeoutValue = 1,
                Timeout = 1,
            },
        };
        FileGlobalConfiguration globalConfiguration = new()
        {
            QoSOptions = new()
            {
                DurationOfBreak = 3,
                BreakDuration = 3,
                ExceptionsAllowedBeforeBreaking = 3,
                MinimumThroughput = 3,
                FailureRatio = 3,
                SamplingDuration = 3,
                TimeoutValue = 3,
                Timeout = 3,
            },
        };

        // Act
        var actual = _creator.Create(route, globalConfiguration);

        // Assert
        Assert.Equal(1, actual.BreakDuration);
        Assert.Equal(1, actual.MinimumThroughput);
        Assert.Equal(3, actual.FailureRatio); // global
        Assert.Equal(3, actual.SamplingDuration); // global
        Assert.Equal(1, actual.Timeout);
    }

    [Fact]
    [Trait("PR", "2339")]
    [Trait("Feat", "2338")]
    public void Create_IRouteGrouping_NullCheck()
    {
        // Arrange
        var method = _creator.GetType().GetMethod(nameof(Create), BindingFlags.Instance | BindingFlags.NonPublic);
        IRouteGrouping grouping = null;
        FileQoSOptions options = null;
        FileGlobalQoSOptions globalOptions = null;

        // Act
        var wrapper = Assert.Throws<TargetInvocationException>(
            () => method.Invoke(_creator, [grouping, options, globalOptions]));

        // Assert
        Assert.IsType<ArgumentNullException>(wrapper.InnerException);
        var actual = (ArgumentNullException)wrapper.InnerException;
        Assert.Equal(nameof(grouping), actual.ParamName);
    }

    [Fact]
    [Trait("PR", "2339")]
    [Trait("Feat", "2338")]
    public void Create() // protected
    {
        // Scenario 1: Null check
        Create_IRouteGrouping_NullCheck();
        var method = _creator.GetType().GetMethod(nameof(Create), BindingFlags.Instance | BindingFlags.NonPublic);
        const int global = 3, route = 1;

        // Scenario 2: if branches
        FileDynamicRoute grouping = new() { Key = "r1" };
        FileQoSOptions options = null;
        FileGlobalQoSOptions globalOptions = new()
        {
            RouteKeys = null,
            DurationOfBreak = global,
            BreakDuration = global,
            ExceptionsAllowedBeforeBreaking = global,
            MinimumThroughput = global,
            FailureRatio = global,
            SamplingDuration = global,
            TimeoutValue = global,
            Timeout = global,
        };

        // Act, Assert : from global opts
        var actual = (QoSOptions)method.Invoke(_creator, [grouping, options, globalOptions]);
        Assert.Equal(global, actual.BreakDuration);
        Assert.Equal(global, actual.MinimumThroughput);
        Assert.Equal(global, actual.FailureRatio);
        Assert.Equal(global, actual.SamplingDuration);
        Assert.Equal(global, actual.Timeout);

        // Arrange 2
        options = new()
        {
            DurationOfBreak = route,
            BreakDuration = route,
            ExceptionsAllowedBeforeBreaking = route,
            MinimumThroughput = route,
            FailureRatio = route,
            SamplingDuration = route,
            TimeoutValue = route,
            Timeout = route,
        };
        globalOptions.RouteKeys = ["?"];

        // Act, Assert 2 : from route
        actual = (QoSOptions)method.Invoke(_creator, [grouping, options, globalOptions]);
        Assert.Equal(route, actual.BreakDuration);
        Assert.Equal(route, actual.MinimumThroughput);
        Assert.Equal(route, actual.FailureRatio);
        Assert.Equal(route, actual.SamplingDuration);
        Assert.Equal(route, actual.Timeout);

        globalOptions.RouteKeys = [grouping.Key];
        actual = (QoSOptions)method.Invoke(_creator, [grouping, options, globalOptions]);
        Assert.Equal(route, actual.BreakDuration);
        Assert.Equal(route, actual.MinimumThroughput);
        Assert.Equal(route, actual.FailureRatio);
        Assert.Equal(route, actual.SamplingDuration);
        Assert.Equal(route, actual.Timeout);

        globalOptions = null;
        actual = (QoSOptions)method.Invoke(_creator, [grouping, options, globalOptions]);
        Assert.Equal(route, actual.BreakDuration);
        Assert.Equal(route, actual.MinimumThroughput);
        Assert.Equal(route, actual.FailureRatio);
        Assert.Equal(route, actual.SamplingDuration);
        Assert.Equal(route, actual.Timeout);

        // Arrange 3 : Merging
        options.FailureRatio = null;
        options.SamplingDuration = null;
        globalOptions = new()
        {
            RouteKeys = null,
            DurationOfBreak = global,
            BreakDuration = global,
            ExceptionsAllowedBeforeBreaking = global,
            MinimumThroughput = global,
            FailureRatio = global,
            SamplingDuration = global,
            TimeoutValue = global,
            Timeout = global,
        };
        actual = (QoSOptions)method.Invoke(_creator, [grouping, options, globalOptions]);
        Assert.Equal(route, actual.BreakDuration);
        Assert.Equal(route, actual.MinimumThroughput);
        Assert.Equal(global, actual.FailureRatio);
        Assert.Equal(global, actual.SamplingDuration);
        Assert.Equal(route, actual.Timeout);
    }

    [Fact]
    [Trait("PR", "2339")]
    [Trait("Feat", "2338")]
    public void Create_IRouteGrouping_NoOptions()
    {
        // Arrange
        var method = _creator.GetType().GetMethod(nameof(Create), BindingFlags.Instance | BindingFlags.NonPublic);
        FileDynamicRoute route = new();
        FileQoSOptions options = null;
        FileGlobalQoSOptions globalOptions = null;

        // Act
        var actual = (QoSOptions)method.Invoke(_creator, [route, options, globalOptions]);

        // Assert
        Assert.NotNull(actual);
        Assert.Null(actual.BreakDuration);
        Assert.Null(actual.MinimumThroughput);
        Assert.Null(actual.FailureRatio);
        Assert.Null(actual.SamplingDuration);
        Assert.Null(actual.Timeout);
    }

    [Fact]
    [Trait("PR", "2339")]
    [Trait("Feat", "2338")]
    public void Merge()
    {
        // Arrange null args
        var method = _creator.GetType().GetMethod(nameof(Merge), BindingFlags.Instance | BindingFlags.NonPublic);
        FileQoSOptions options = null, global = null;

        // Act
        var actual = (QoSOptions)method.Invoke(_creator, [options, global]);

        // Assert
        Assert.NotNull(actual);
        Assert.Null(actual.BreakDuration);
        Assert.Null(actual.MinimumThroughput);
        Assert.Null(actual.FailureRatio);
        Assert.Null(actual.SamplingDuration);
        Assert.Null(actual.Timeout);
    }
    #endregion PR 2339
}
