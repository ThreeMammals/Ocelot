using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ocelot.Authorization;
using Ocelot.Cache;
using Ocelot.Claims;
using Ocelot.Configuration.Repository;
using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.DownstreamRouteFinder.Middleware;
using Ocelot.DownstreamUrlCreator;
using Ocelot.Headers;
using Ocelot.Infrastructure.RequestData;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Multiplexer;
using Ocelot.PathManipulation;
using Ocelot.QueryStrings;
using Ocelot.RateLimiting;
using Ocelot.Request.Creator;
using Ocelot.Request.Mapper;
using Ocelot.Requester;
using Ocelot.Requester.Middleware;
using Ocelot.Responder;
using Ocelot.Security;
using Ocelot.WebSockets;
using System.Reflection;

namespace Ocelot.UnitTests.Middleware;

public class OcelotPipelineExtensionsTests : UnitTest
{
    private ApplicationBuilder _builder;

    [Fact]
    [Trait("PR", "283")] // https://github.com/ThreeMammals/Ocelot/pull/283
    public void Should_set_up_pipeline()
    {
        // Arrange
        GivenTheDepedenciesAreSetUp();

        // Act
        var @delegate = _builder.BuildOcelotPipeline(new OcelotPipelineConfiguration());

        // Assert
        Assert.NotNull(@delegate);
        Assert.Equal(typeof(ConfigurationMiddleware), @delegate.Target.GetType());

        var middlewares = ThenMiddlewares(22);
        Assert.Contains(typeof(ConfigurationMiddleware).FullName, middlewares);
        Assert.Contains(typeof(HttpRequesterMiddleware).FullName, middlewares);
        Assert.Equal(0, middlewares.IndexOf(typeof(ConfigurationMiddleware).FullName));
        Assert.Equal(21, middlewares.IndexOf(typeof(HttpRequesterMiddleware).FullName));
    }

    [Fact]
    [Trait("PR", "416")] // https://github.com/ThreeMammals/Ocelot/pull/416
    [Trait("PR", "510")] // https://github.com/ThreeMammals/Ocelot/pull/510
    public void Should_expand_pipeline()
    {
        // Arrange
        GivenTheDepedenciesAreSetUp();
        var configuration = new OcelotPipelineConfiguration();
        configuration.MapWhenOcelotPipeline.Add(IsWebSocketRequest, ExpandAppPipeline);

        // Act
        var @delegate = _builder.BuildOcelotPipeline(configuration);

        // Assert
        Assert.NotNull(@delegate);
        Assert.Equal(typeof(ConfigurationMiddleware), @delegate.Target.GetType());
        var middlewares = ThenMiddlewares(23);
        Assert.Contains(typeof(ConfigurationMiddleware).FullName, middlewares);
        Assert.Contains(typeof(HttpRequesterMiddleware).FullName, middlewares);
        var expectedMiddleware = $"MapWhen[{typeof(TestMiddleware)}]";
        Assert.Contains(expectedMiddleware, middlewares); // !!!
        Assert.Equal(0, middlewares.IndexOf(typeof(ConfigurationMiddleware).FullName));
        Assert.Equal(22, middlewares.IndexOf(typeof(HttpRequesterMiddleware).FullName));
        Assert.Equal(8, middlewares.IndexOf(expectedMiddleware)); // !!!
    }
    private static bool IsWebSocketRequest(HttpContext c) => c.WebSockets.IsWebSocketRequest;
    private static void ExpandAppPipeline(IApplicationBuilder app) => app.UseMiddleware<TestMiddleware>();

    #region PR 2387 // https://github.com/ThreeMammals/Ocelot/pull/2387
    [Fact]
    public void UseIfNotNull_WithNullMiddleware_ShouldReturnBuilderUnmodified()
    {
        // Arrange
        GivenApplicationBuilder();

        // Act
        var actual = _builder.UseIfNotNull(NullMiddleware);

        // Assert
        Assert.Same(_builder, actual);
        ThenMiddlewares(0);
    }

    [Fact]
    public void UseIfNotNull_WithValidMiddleware_ShouldRegisterMiddleware()
    {
        // Arrange
        GivenApplicationBuilder();

        // Act
        var actual = _builder.UseIfNotNull(CustomMiddleware);

        // Assert
        Assert.Same(_builder, actual);
        var middlewares = ThenMiddlewares(1);
        var middleware = Assert.Single(middlewares);
        Assert.Equal("Ocelot.UnitTests.Middleware.OcelotPipelineExtensionsTests.CustomMiddleware", middleware);
    }

    [Fact]
    public void UseIfNotNull_Generic_WithNullMiddleware_ShouldUseDefaultMiddleware()
    {
        // Arrange
        GivenApplicationBuilder();

        // Act
        var actual = _builder.UseIfNotNull<TestMiddleware>(NullMiddleware);

        // Assert
        Assert.Same(_builder, actual);
        var middlewares = ThenMiddlewares(1);
        var middleware = Assert.Single(middlewares);
        Assert.Equal("Ocelot.UnitTests.Middleware.OcelotPipelineExtensionsTests+TestMiddleware", middleware);
    }

    [Fact]
    public void UseIfNotNull_Generic_WithValidMiddleware_ShouldRegisterCustomMiddleware()
    {
        // Arrange
        GivenApplicationBuilder();

        // Act
        var actual = _builder.UseIfNotNull<TestMiddleware>(CustomMiddleware);

        // Assert
        Assert.Same(_builder, actual);
        var middlewares = ThenMiddlewares(1);
        var middleware = Assert.Single(middlewares);
        Assert.Equal("Ocelot.UnitTests.Middleware.OcelotPipelineExtensionsTests.CustomMiddleware", middleware);
    }

    [Fact]
    public void UseIfNotNull_Generic_WithNullMiddleware_AndAddDefaultFalse_ShouldReturnBuilderUnmodified()
    {
        // Arrange
        GivenApplicationBuilder();

        // Act
        var actual = _builder.UseIfNotNull<TestMiddleware>(NullMiddleware, addDefault: false);

        // Assert
        Assert.Same(_builder, actual);
        ThenMiddlewares(0);
    }

    [Fact]
    public void UseIfNotNull_Generic_WithValidMiddleware_AndAddDefaultFalse_ShouldRegisterCustomMiddleware()
    {
        // Arrange
        GivenApplicationBuilder();

        // Act
        var actual = _builder.UseIfNotNull<TestMiddleware>(CustomMiddleware, addDefault: false);

        // Assert
        Assert.Same(_builder, actual);
        var middlewares = ThenMiddlewares(1);
        var middleware = Assert.Single(middlewares);
        Assert.Equal("Ocelot.UnitTests.Middleware.OcelotPipelineExtensionsTests.CustomMiddleware", middleware);
    }

    [Fact]
    public void UseIfNotNull_GenericWithType_WithNullType_ShouldReturnBuilderUnmodified()
    {
        // Arrange
        GivenApplicationBuilder();
        Type nullType = null;

        // Act
        var actual = _builder.UseIfNotNull<OcelotMiddleware>(nullType);

        // Assert
        Assert.Same(_builder, actual);
        var middlewares = ThenMiddlewares(0);
        Assert.Empty(middlewares);
    }

    [Fact]
    public void UseIfNotNull_GenericWithType_WithValidType_ShouldRegisterMiddleware()
    {
        // Arrange
        GivenApplicationBuilder();
        var middlewareType = typeof(MyWsMiddleware);

        // Act
        var actual = _builder.UseIfNotNull<WebSocketsProxyMiddleware>(middlewareType);

        // Assert
        Assert.Same(_builder, actual);
        var middlewares = ThenMiddlewares(1);
        var middleware = Assert.Single(middlewares);
        Assert.Equal("Ocelot.UnitTests.Middleware.OcelotPipelineExtensionsTests+MyWsMiddleware", middleware);
    }

    [Fact]
    public void UseIfNotNull_GenericWithType_WithInvalidType_ShouldThrowException()
    {
        // Arrange
        GivenApplicationBuilder();
        var invalidType = typeof(TestMiddleware); // Different base type

        // Act & Assert
        var exception = Assert.Throws<Exception>(() =>
            _builder.UseIfNotNull<WebSocketsProxyMiddleware>(invalidType));

        // Assert
        Assert.Equal("Unable to start Ocelot: error injecting Ocelot.UnitTests.Middleware.OcelotPipelineExtensionsTests+TestMiddleware, since its base type is not a(n) Ocelot.WebSockets.WebSocketsProxyMiddleware!", exception.Message);
    }

    [Fact]
    public void ConfigureWebSockets_WithDefaultConfiguration_ShouldRegisterMiddlewares()
    {
        // Arrange
        GivenApplicationBuilder();
        var configuration = new OcelotPipelineConfiguration();

        // Act
        _builder.ConfigureWebSockets(configuration);

        // Assert
        var middlewares = ThenMiddlewares(7);
        Assert.Contains(typeof(DownstreamRouteFinderMiddleware).FullName, middlewares);
        Assert.Contains(typeof(WebSocketsProxyMiddleware).FullName, middlewares);
        Assert.Equal(0, middlewares.IndexOf(typeof(DownstreamRouteFinderMiddleware).FullName));
        Assert.Equal(6, middlewares.IndexOf(typeof(WebSocketsProxyMiddleware).FullName));
    }

    [Fact]
    public void ConfigureWebSockets_WithWebSocketsMiddlewareType_ShouldRegisterCustomMiddleware()
    {
        // Arrange
        GivenApplicationBuilder();
        var configuration = new OcelotPipelineConfiguration
        {
            WebSocketsMiddlewareType = typeof(MyWsMiddleware)
        };

        // Act
        _builder.ConfigureWebSockets(configuration);

        // Assert
        var middlewares = ThenMiddlewares(7);
        Assert.Contains(typeof(DownstreamRouteFinderMiddleware).FullName, middlewares);
        Assert.Contains(typeof(MyWsMiddleware).FullName, middlewares);
        Assert.Equal(0, middlewares.IndexOf(typeof(DownstreamRouteFinderMiddleware).FullName));
        Assert.Equal(6, middlewares.IndexOf(typeof(MyWsMiddleware).FullName));
    }

    private static Task CustomWebSocketMiddleware(HttpContext context, Func<Task> next) => next();

    [Fact]
    public void ConfigureWebSockets_WithWebSocketsMiddleware_ShouldRegisterCustomMiddleware()
    {
        // Arrange
        GivenApplicationBuilder();
        var configuration = new OcelotPipelineConfiguration
        {
            WebSocketsMiddleware = CustomWebSocketMiddleware
        };

        // Act
        _builder.ConfigureWebSockets(configuration);

        // Assert
        var middlewares = ThenMiddlewares(7);
        Assert.Contains(typeof(DownstreamRouteFinderMiddleware).FullName, middlewares);
        Assert.Contains("Ocelot.UnitTests.Middleware.OcelotPipelineExtensionsTests.CustomWebSocketMiddleware", middlewares);
        Assert.Equal(0, middlewares.IndexOf(typeof(DownstreamRouteFinderMiddleware).FullName));
        Assert.Equal(6, middlewares.IndexOf("Ocelot.UnitTests.Middleware.OcelotPipelineExtensionsTests.CustomWebSocketMiddleware"));
    }

    [Fact]
    public void ConfigureWebSockets_WithoutWebSocketsMiddlewareOpts_ShouldRegisterDefaultWsMiddleware()
    {
        // Arrange
        GivenTheDepedenciesAreSetUp();
        var noOptions = new OcelotPipelineConfiguration();

        // Act
        _builder.ConfigureWebSockets(noOptions);

        // Assert
        var middlewares = ThenMiddlewares(7);
        Assert.Contains(typeof(DownstreamRouteFinderMiddleware).FullName, middlewares);
        Assert.Contains(typeof(WebSocketsProxyMiddleware).FullName, middlewares);
        Assert.Equal(0, middlewares.IndexOf(typeof(DownstreamRouteFinderMiddleware).FullName));
        Assert.Equal(6, middlewares.IndexOf(typeof(WebSocketsProxyMiddleware).FullName));
    }

    [Fact]
    public void ConfigureWebSockets_WithWebSocketsMiddlewareType_AndWebSocketsMiddleware_ShouldPreferType()
    {
        // Arrange
        GivenApplicationBuilder();
        var configuration = new OcelotPipelineConfiguration
        {
            WebSocketsMiddlewareType = typeof(MyWsMiddleware),
            WebSocketsMiddleware = CustomWebSocketMiddleware
        };

        // Act
        _builder.ConfigureWebSockets(configuration);

        // Assert
        var middlewares = ThenMiddlewares(8);
        Assert.Contains(typeof(DownstreamRouteFinderMiddleware).FullName, middlewares);
        Assert.Contains(typeof(MyWsMiddleware).FullName, middlewares);
        Assert.Contains("Ocelot.UnitTests.Middleware.OcelotPipelineExtensionsTests.CustomWebSocketMiddleware", middlewares);
        Assert.Equal(0, middlewares.IndexOf(typeof(DownstreamRouteFinderMiddleware).FullName));
        // Both should be registered, Type first then Middleware
        Assert.Equal(6, middlewares.IndexOf(typeof(MyWsMiddleware).FullName));
        Assert.Equal(7, middlewares.IndexOf("Ocelot.UnitTests.Middleware.OcelotPipelineExtensionsTests.CustomWebSocketMiddleware"));
    }
    #endregion PR 2387

    private void GivenTheDepedenciesAreSetUp()
    {
        var services = new ServiceCollection();
        services.AddSingleton<RequestDelegate>(ctx => Task.CompletedTask);
        services.AddSingleton(Mock.Of<IOcelotLoggerFactory>()); // for OcelotMiddleware, for all
        services.AddSingleton(Mock.Of<ILoggerFactory>()); // for OcelotLoggerFactory, for all middlewares

        services.AddSingleton(Mock.Of<IDownstreamRouteProviderFactory>()); // for DownstreamRouteFinderMiddleware
        services.AddSingleton(Mock.Of<IResponseAggregatorFactory>()); // for MultiplexingMiddleware
        services.AddSingleton(Mock.Of<IRequestMapper>()); // for DownstreamRequestInitialiserMiddleware
        services.AddSingleton(Mock.Of<IDownstreamRequestCreator>()); // for DownstreamRequestInitialiserMiddleware
        services.AddSingleton(Mock.Of<ILoadBalancerHouse>()); // for LoadBalancingMiddleware
        services.AddSingleton(Mock.Of<IDownstreamPathPlaceholderReplacer>()); // for DownstreamUrlCreatorMiddleware
        services.AddSingleton(Mock.Of<IWebSocketsFactory>()); // for WebSocketsProxyMiddleware
        services.AddSingleton<IOptions<WebSocketOptions>>(Options.Create(new WebSocketOptions())); // for WebSocketMiddleware

        services.AddSingleton(Mock.Of<IHttpRequester>()); // for HttpRequesterMiddleware
        services.AddSingleton(Mock.Of<IOcelotCache<CachedResponse>>()); // for OutputCacheMiddleware
        services.AddSingleton(Mock.Of<ICacheKeyGenerator>()); // for OutputCacheMiddleware
        services.AddSingleton(Mock.Of<IChangeDownstreamPathTemplate>()); // for ClaimsToDownstreamPathMiddleware
        services.AddSingleton(Mock.Of<IAddQueriesToRequest>()); // for ClaimsToQueryStringMiddleware
        services.AddSingleton(Mock.Of<IAddHeadersToRequest>()); // for ClaimsToHeadersMiddleware
        services.AddSingleton(Mock.Of<IClaimsAuthorizer>()); // for AuthorizationMiddleware
        services.AddSingleton(Mock.Of<IScopesAuthorizer>()); // for AuthorizationMiddleware
        services.AddSingleton(Mock.Of<IAddClaimsToRequest>()); // for ClaimsToClaimsMiddleware
        services.AddSingleton(Mock.Of<IRequestScopedDataRepository>()); // for RequestIdMiddleware, ExceptionHandlerMiddleware
        services.AddSingleton(Mock.Of<IRateLimiting>()); // for RateLimitingMiddleware
        services.AddSingleton(Mock.Of<IHttpContextAccessor>()); // for RateLimitingMiddleware
        services.AddSingleton(Mock.Of<IHttpContextRequestHeaderReplacer>()); // for HttpHeadersTransformationMiddleware
        services.AddSingleton(Mock.Of<IHttpResponseHeaderReplacer>()); // for HttpHeadersTransformationMiddleware
        services.AddSingleton(Mock.Of<IAddHeadersToResponse>()); // for HttpHeadersTransformationMiddleware
        services.AddSingleton(Mock.Of<IAddHeadersToRequest>()); // for HttpHeadersTransformationMiddleware
        services.AddSingleton(Mock.Of<ISecurityPolicy>()); // for SecurityMiddleware
        services.AddSingleton(Mock.Of<IHttpResponder>()); // for ResponderMiddleware
        services.AddSingleton(Mock.Of<IErrorsToHttpStatusCodeMapper>()); // for ResponderMiddleware
        services.AddSingleton(Mock.Of<IInternalConfigurationRepository>()); // for ConfigurationMiddleware

        var provider = services.BuildServiceProvider(true);
        _builder = new ApplicationBuilder(provider);
    }

    private void GivenApplicationBuilder()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider(true);
        _builder = new ApplicationBuilder(provider);
    }

    private static readonly Func<HttpContext, Func<Task>, Task> NullMiddleware = null;
    private static Task CustomMiddleware(HttpContext context, Func<Task> next) => Task.CompletedTask;

    public class TestMiddleware : OcelotMiddleware
    {
        public TestMiddleware(RequestDelegate _, IOcelotLoggerFactory logging)
            : base(logging.CreateLogger<TestMiddleware>()) { }
        public Task Invoke(HttpContext _) => Task.CompletedTask;
    }
    private class MyWsMiddleware : WebSocketsProxyMiddleware
    {
        public MyWsMiddleware(RequestDelegate next, IOcelotLoggerFactory logging, IWebSocketsFactory factory)
            : base(next, logging, factory) { }
    }

    private List<string> ThenMiddlewares(int count)
    {
        List<Func<RequestDelegate, RequestDelegate>> _components;
        var f = _builder.GetType().GetField(nameof(_components), BindingFlags.NonPublic | BindingFlags.Instance);
        var v = f.GetValue(_builder);
        Assert.IsType<List<Func<RequestDelegate, RequestDelegate>>>(v);
        _components = v as List<Func<RequestDelegate, RequestDelegate>>;
        var middlewares = _components.Select(CreateMiddlewareDescription).ToList();
        Assert.Equal(count, middlewares.Count);
        return middlewares;
    }

    private static string CreateMiddlewareDescription(Func<RequestDelegate, RequestDelegate> middleware)
    {
        if (middleware == null)
            return "<null>";

        if (middleware.Target == null)
            return middleware.Method.Name;

        var target = middleware.Target;
        var method = middleware.Method;

        // 1. Microsoft internal middleware (UseMiddleware<T>, etc.)
        if (method.Name == "CreateMiddleware" && method.DeclaringType?.Namespace?.StartsWith("Microsoft.") == true)
            return target.ToString();

        // 2. Try to unwrap MapWhen first (highest priority for friendly name)
        var mapWhenName = TryGetMapWhenDescription(middleware);
        if (!string.IsNullOrEmpty(mapWhenName))
            return mapWhenName;

        // 3. Try to unwrap the common Func<HttpContext, Func<Task>, Task> wrapper
        var captured = TryGetCapturedMiddleware(middleware);
        if (!string.IsNullOrEmpty(captured))
            return captured;

        // 4. Generic fallback
        return target.GetType().FullName + "." + method.Name;
    }

    private static string TryGetMapWhenDescription(Func<RequestDelegate, RequestDelegate> middleware)
    {
        var targetType = middleware.Target?.GetType();
        if (targetType == null)
            return null;

        // Check if this is the closure from MapWhenExtensions
        if (!targetType.FullName?.Contains("MapWhenExtensions+<>c__DisplayClass") == true)
            return null;

        // Look for the captured MapWhenOptions (the configuration lambda)
        foreach (var field in targetType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            MapWhenOptions options;
            if (field.Name == nameof(options) && field.FieldType == typeof(MapWhenOptions))
            {
                options = field.GetValue(middleware.Target) as MapWhenOptions;
                if (options == null) continue;

                // Now we have the original configuration options (e.g. ExpandAppPipeline)
                var actionMethod = options.Branch.Method;
                if (actionMethod.DeclaringType != null)
                {
                    string typeName = actionMethod.DeclaringType.FullName;
                    string methodName = actionMethod.Name;

                    // Clean up compiler-generated names
                    if (methodName.Contains('<') || methodName.StartsWith("b__"))
                        methodName = "InlineConfig";

                    return actionMethod.DeclaringType.IsAssignableTo(typeof(OcelotMiddleware))
                        ? $"MapWhen[{typeName}]"
                        : $"MapWhen[{typeName}.{methodName}]";
                }
            }
        }

        return null;
    }

    private static string TryGetCapturedMiddleware(Func<RequestDelegate, RequestDelegate> middleware)
    {
        var target = middleware.Target;
        if (target == null) return null;

        var targetType = target.GetType();
        foreach (var field in targetType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (field.FieldType != typeof(Func<HttpContext, Func<Task>, Task>))
                continue;

            var captured = field.GetValue(target);
            if (captured is Func<HttpContext, Func<Task>, Task> originalMiddleware)
            {
                var origMethod = originalMiddleware.Method;
                var origTarget = originalMiddleware.Target;

                if (origTarget == null)
                    return $"{origMethod.DeclaringType?.FullName}.{origMethod.Name}";

                if (!origMethod.Name.Contains('<') && !origMethod.Name.StartsWith("b__"))
                    return origTarget.GetType().FullName + "." + origMethod.Name;

                return origTarget.GetType().FullName ?? "InlineMiddleware";
            }
        }

        return null;
    }
}
