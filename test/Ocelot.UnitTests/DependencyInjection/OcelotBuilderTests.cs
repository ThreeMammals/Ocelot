using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.MiddlewareAnalysis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ocelot.Configuration.Setter;
using Ocelot.DependencyInjection;
using Ocelot.Infrastructure;
using Ocelot.LoadBalancer.Balancers;
using Ocelot.Multiplexer;
using Ocelot.Requester;
using Ocelot.Responses;
using Ocelot.UnitTests.Requester;
using Ocelot.Values;
using System.Reflection;
using System.Text.Encodings.Web;
using static Ocelot.UnitTests.Multiplexing.UserDefinedResponseAggregatorTests;

namespace Ocelot.UnitTests.DependencyInjection;

public class OcelotBuilderTests : UnitTest
{
    private readonly IConfiguration _configRoot;
    private readonly IServiceCollection _services;
    private IServiceProvider _serviceProvider;
    private IOcelotBuilder _ocelotBuilder;

    public OcelotBuilderTests()
    {
        _configRoot = new ConfigurationRoot(new List<IConfigurationProvider>());
        _services = new ServiceCollection();
        _services.AddSingleton(GetHostingEnvironment());
        _services.AddSingleton(_configRoot);
    }

    private static IWebHostEnvironment GetHostingEnvironment()
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.Setup(e => e.ApplicationName)
            .Returns(typeof(OcelotBuilderTests).GetTypeInfo().Assembly.GetName().Name);
        return environment.Object;
    }

    [Fact]
    public void Should_add_specific_delegating_handlers_transient()
    {
        // Arrange
        _ocelotBuilder = _services.AddOcelot(_configRoot);

        // Act
        _ocelotBuilder.AddDelegatingHandler<FakeDelegatingHandler>();
        _ocelotBuilder.AddDelegatingHandler<FakeDelegatingHandlerTwo>();

        // Assert
        ThenTheProviderIsRegisteredAndReturnsSpecificHandlers<FakeDelegatingHandler, FakeDelegatingHandlerTwo>();
        ThenTheSpecificHandlersAreTransient();
    }

    [Fact]
    public void Should_add_type_specific_delegating_handlers_transient()
    {
        // Arrange
        _ocelotBuilder = _services.AddOcelot(_configRoot);

        // Act
        _ocelotBuilder.AddDelegatingHandler(typeof(FakeDelegatingHandler));
        _ocelotBuilder.AddDelegatingHandler(typeof(FakeDelegatingHandlerTwo));

        // Assert
        ThenTheProviderIsRegisteredAndReturnsSpecificHandlers<FakeDelegatingHandler, FakeDelegatingHandlerTwo>();
        ThenTheSpecificHandlersAreTransient();
    }

    [Fact]
    public void Should_add_global_delegating_handlers_transient()
    {
        // Arrange
        _ocelotBuilder = _services.AddOcelot(_configRoot);

        // Act
        _ocelotBuilder.AddDelegatingHandler<FakeDelegatingHandler>(true);
        _ocelotBuilder.AddDelegatingHandler<FakeDelegatingHandlerTwo>(true);

        // Assert
        ThenTheProviderIsRegisteredAndReturnsHandlers<FakeDelegatingHandler, FakeDelegatingHandlerTwo>();
        ThenTheGlobalHandlersAreTransient();
    }

    [Fact]
    public void Should_add_global_type_delegating_handlers_transient()
    {
        // Arrange
        _ocelotBuilder = _services.AddOcelot(_configRoot);

        // Act
        _ocelotBuilder.AddDelegatingHandler<FakeDelegatingHandler>(true);
        _ocelotBuilder.AddDelegatingHandler<FakeDelegatingHandlerTwo>(true);

        // Assert
        ThenTheProviderIsRegisteredAndReturnsHandlers<FakeDelegatingHandler, FakeDelegatingHandlerTwo>();
        ThenTheGlobalHandlersAreTransient();
    }

    [Fact]
    public void Should_set_up_services()
    {
        // Arrange, Act, Assert
        _ocelotBuilder = _services.AddOcelot(_configRoot);
    }

    [Fact]
    public void Should_return_ocelot_builder()
    {
        // Arrange, Act
        _ocelotBuilder = _services.AddOcelot(_configRoot);

        // Assert
        _ocelotBuilder.ShouldBeOfType<OcelotBuilder>();
    }

    [Fact]
    public void Should_use_logger_factory()
    {
        // Arrange
        _ocelotBuilder = _services.AddOcelot(_configRoot);
        _serviceProvider = _services.BuildServiceProvider(true);

        // Act
        var logger = _serviceProvider.GetService<IFileConfigurationSetter>();

        // Assert
        logger.ShouldNotBeNull();
    }

    [Fact]
    public void Should_set_up_without_passing_in_config()
    {
        // Arrange, Act, Assert
        _ocelotBuilder = _services.AddOcelot();
    }

    [Fact]
    public void Should_add_singleton_defined_aggregators()
    {
        // Arrange
        _ocelotBuilder = _services.AddOcelot(_configRoot);

        // Act
        _ocelotBuilder.AddSingletonDefinedAggregator<TestDefinedAggregator>();
        _ocelotBuilder.AddSingletonDefinedAggregator<TestDefinedAggregator>();

        // Assert
        ThenTheProviderIsRegisteredAndReturnsSpecificAggregators<TestDefinedAggregator, TestDefinedAggregator>();

        // Then The Aggregators Are Singleton<TestDefinedAggregator, TestDefinedAggregator>
        var aggregators = _serviceProvider.GetServices<IDefinedAggregator>().ToList();
        var first = aggregators[0];
        aggregators = _serviceProvider.GetServices<IDefinedAggregator>().ToList();
        var second = aggregators[0];
        first.ShouldBe(second);
    }

    [Fact]
    public void Should_add_transient_defined_aggregators()
    {
        // Arrange
        _ocelotBuilder = _services.AddOcelot(_configRoot);

        // Act
        _ocelotBuilder.AddTransientDefinedAggregator<TestDefinedAggregator>();
        _ocelotBuilder.AddTransientDefinedAggregator<TestDefinedAggregator>();

        // Assert
        ThenTheProviderIsRegisteredAndReturnsSpecificAggregators<TestDefinedAggregator, TestDefinedAggregator>();

        // Then The Aggregators Are Transient<TestDefinedAggregator, TestDefinedAggregator>
        var aggregators = _serviceProvider.GetServices<IDefinedAggregator>().ToList();
        var first = aggregators[0];
        aggregators = _serviceProvider.GetServices<IDefinedAggregator>().ToList();
        var second = aggregators[0];
        first.ShouldNotBe(second);
    }

    [Fact]
    public void Should_add_custom_load_balancer_creators_by_default_ctor()
    {
        // Arrange
        _ocelotBuilder = _services.AddOcelot(_configRoot);

        // Act
        _ocelotBuilder.AddCustomLoadBalancer<FakeCustomLoadBalancer>();

        // Assert
        ThenTheProviderIsRegisteredAndReturnsBothBuiltInAndCustomLoadBalancerCreators();
    }

    [Fact]
    public void Should_add_custom_load_balancer_creators_by_factory_method()
    {
        // Arrange
        _ocelotBuilder = _services.AddOcelot(_configRoot);

        // Act
        _ocelotBuilder.AddCustomLoadBalancer(() => new FakeCustomLoadBalancer());

        // Assert
        ThenTheProviderIsRegisteredAndReturnsBothBuiltInAndCustomLoadBalancerCreators();
    }

    [Fact]
    public void Should_add_custom_load_balancer_creators_by_di_factory_method()
    {
        // Arrange
        _ocelotBuilder = _services.AddOcelot(_configRoot);

        // Act
        _ocelotBuilder.AddCustomLoadBalancer(provider => new FakeCustomLoadBalancer());

        // Assert
        ThenTheProviderIsRegisteredAndReturnsBothBuiltInAndCustomLoadBalancerCreators();
    }

    [Fact]
    public void Should_add_custom_load_balancer_creators_by_factory_method_with_arguments()
    {
        // Arrange
        _ocelotBuilder = _services.AddOcelot(_configRoot);

        // Act
        _ocelotBuilder.AddCustomLoadBalancer((route, discoveryProvider) => new FakeCustomLoadBalancer());

        // Assert
        ThenTheProviderIsRegisteredAndReturnsBothBuiltInAndCustomLoadBalancerCreators();
    }

    [Fact]
    public void Should_replace_iplaceholder()
    {
        // Arrange
        _ocelotBuilder = _services.AddOcelot(_configRoot);

        // Act
        _ocelotBuilder.AddConfigPlaceholders();

        // Assert
        _serviceProvider = _services.BuildServiceProvider(true);
        var placeholders = _serviceProvider.GetService<IPlaceholders>();
        placeholders.ShouldBeOfType<ConfigAwarePlaceholders>();
    }

    [Fact]
    public void Should_add_custom_load_balancer_creators()
    {
        // Arrange
        _ocelotBuilder = _services.AddOcelot(_configRoot);

        // Act
        _ocelotBuilder.AddCustomLoadBalancer((provider, route, discoveryProvider) => new FakeCustomLoadBalancer());

        // Assert
        ThenTheProviderIsRegisteredAndReturnsBothBuiltInAndCustomLoadBalancerCreators();
    }

    [Fact]
    public void Should_use_default_mvc_builder()
    {
        // Arrange, Act
        _ocelotBuilder = _services.AddOcelot();

        // Assert
        CstorShouldUseDefaultBuilderToInitMvcCoreBuilder();
    }

    private void CstorShouldUseDefaultBuilderToInitMvcCoreBuilder()
    {
        _ocelotBuilder.ShouldNotBeNull();
        _ocelotBuilder.MvcCoreBuilder.ShouldNotBeNull();
        _serviceProvider = _services.BuildServiceProvider(true);
        using IServiceScope scope = _serviceProvider.CreateScope();

        // .AddMvcCore()
        _serviceProvider.GetServices<IConfigureOptions<MvcOptions>>()
            .FirstOrDefault(s => s.GetType().Name == "MvcCoreMvcOptionsSetup")
            .ShouldNotBeNull();

        // .AddLogging()
        _serviceProvider.GetService<ILoggerFactory>()
            .ShouldNotBeNull().ShouldBeOfType<LoggerFactory>();
        _serviceProvider.GetService<IConfigureOptions<LoggerFilterOptions>>()
            .ShouldNotBeNull();

        // .AddMiddlewareAnalysis()
        _serviceProvider.GetService<IStartupFilter>()
            .ShouldNotBeNull().ShouldBeOfType<AnalysisStartupFilter>();

        // .AddWebEncoders()
        _serviceProvider.GetService<HtmlEncoder>().ShouldNotBeNull();
        _serviceProvider.GetService<JavaScriptEncoder>().ShouldNotBeNull();
        _serviceProvider.GetService<UrlEncoder>().ShouldNotBeNull();

        // .AddApplicationPart(assembly)
        IList<ApplicationPart> list = _ocelotBuilder.MvcCoreBuilder.PartManager.ApplicationParts;
        list.ShouldNotBeNull().Count.ShouldBe(2);
        list.ShouldContain(part => part.Name == "Ocelot");
        list.ShouldContain(part => part.Name == "Ocelot.UnitTests");

        // .AddControllersAsServices()
        _serviceProvider.GetService<IControllerActivator>()
            .ShouldNotBeNull().ShouldBeOfType<ServiceBasedControllerActivator>();

        // .AddAuthorization()
        scope.ServiceProvider.GetService<IAuthenticationService>()
            .ShouldNotBeNull().ShouldBeOfType<AuthenticationService>();
        _serviceProvider.GetService<IApplicationModelProvider>()
            .ShouldNotBeNull()
            .GetType().Name.ShouldBe("AuthorizationApplicationModelProvider");

        // .AddNewtonsoftJson()
        _serviceProvider.GetServices<IConfigureOptions<MvcOptions>>()
            .FirstOrDefault(s => s.GetType().Name == "NewtonsoftJsonMvcOptionsSetup")
            .ShouldNotBeNull();
        _serviceProvider.GetService<IActionResultExecutor<JsonResult>>()
            .ShouldNotBeNull()
            .GetType().Name.ShouldBe("NewtonsoftJsonResultExecutor");
        _serviceProvider.GetService<IJsonHelper>()
            .ShouldNotBeNull()
            .GetType().Name.ShouldBe("NewtonsoftJsonHelper");
    }

    [Fact]
    public void Should_use_custom_mvc_builder_no_configuration()
    {
        // Arrange, Act
        WhenISetupOcelotServicesWithCustomMvcBuider();

        // Assert
        CstorShouldUseCustomBuilderToInitMvcCoreBuilder();
        ShouldFindConfiguration();
    }

    [Theory]
    [Trait("PR", "1986")]
    [Trait("Issue", "1518")]
    [InlineData(false)]
    [InlineData(true)]
    public void Should_use_custom_mvc_builder_with_configuration(bool hasConfig)
    {
        // Arrange, Act
        WhenISetupOcelotServicesWithCustomMvcBuider(
            hasConfig ? _configRoot : null,
            true);

        // Assert
        CstorShouldUseCustomBuilderToInitMvcCoreBuilder();
        ShouldFindConfiguration();
    }

    [Fact]
    public void CreateInstance_CreatedFromImplementationInstance()
    {
        // Arrange
        var method = typeof(OcelotBuilder).GetMethod("CreateInstance", BindingFlags.NonPublic | BindingFlags.Static);
        ServiceDescriptor descriptor = new(GetType(), this);

        // Act
        var result = method.Invoke(null, [null, descriptor]);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<OcelotBuilderTests>(result);
        Assert.Equal(this, result);
    }

    [Fact]
    public void CreateInstance_CreatedByImplementationFactory()
    {
        // Arrange
        var method = typeof(OcelotBuilder).GetMethod("CreateInstance", BindingFlags.NonPublic | BindingFlags.Static);

        object factory(IServiceProvider p) => this;
        ServiceDescriptor descriptor = new(GetType(), factory, ServiceLifetime.Singleton);

        // Act
        var result = method.Invoke(null, [null, descriptor]);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<OcelotBuilderTests>(result);
        Assert.Equal(this, result);
    }

    private bool _fakeCustomBuilderCalled;
    private IMvcCoreBuilder FakeCustomBuilder(IMvcCoreBuilder builder, Assembly assembly)
    {
        _fakeCustomBuilderCalled = true;
        return builder.AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.WriteIndented = true;
            });
    }

    private void WhenISetupOcelotServicesWithCustomMvcBuider(IConfiguration configuration = null, bool useConfigParam = false)
    {
        _fakeCustomBuilderCalled = false;
        _ocelotBuilder = !useConfigParam
            ? _services.AddOcelotUsingBuilder(FakeCustomBuilder)
            : _services.AddOcelotUsingBuilder(configuration, FakeCustomBuilder);
    }

    private void CstorShouldUseCustomBuilderToInitMvcCoreBuilder()
    {
        _fakeCustomBuilderCalled.ShouldBeTrue();

        _ocelotBuilder.ShouldNotBeNull();
        _ocelotBuilder.MvcCoreBuilder.ShouldNotBeNull();
        _serviceProvider = _services.BuildServiceProvider(true);

        // .AddMvcCore()
        _serviceProvider.GetServices<IConfigureOptions<MvcOptions>>()
            .FirstOrDefault(s => s.GetType().Name == "MvcCoreMvcOptionsSetup")
            .ShouldNotBeNull();

        // .AddJsonOptions(options => { })
        _serviceProvider.GetService<IOptionsMonitorCache<JsonOptions>>()
            .ShouldNotBeNull().ShouldBeOfType<OptionsCache<JsonOptions>>();
        _serviceProvider.GetService<IConfigureOptions<JsonOptions>>()
            .ShouldNotBeNull().ShouldBeOfType<ConfigureNamedOptions<JsonOptions>>();
    }

    private void ShouldFindConfiguration()
    {
        _ocelotBuilder.ShouldNotBeNull();
        var actual = _ocelotBuilder.Configuration.ShouldNotBeNull();
        actual.Equals(_configRoot).ShouldBeTrue(); // check references equality
        actual.ShouldBe(_configRoot);
    }

    private void ThenTheSpecificHandlersAreTransient()
    {
        var handlers = _serviceProvider.GetServices<DelegatingHandler>().ToList();
        var first = handlers[0];
        handlers = _serviceProvider.GetServices<DelegatingHandler>().ToList();
        var second = handlers[0];
        first.ShouldNotBe(second);
    }

    private void ThenTheGlobalHandlersAreTransient()
    {
        var handlers = _serviceProvider.GetServices<GlobalDelegatingHandler>().ToList();
        var first = handlers[0].DelegatingHandler;
        handlers = _serviceProvider.GetServices<GlobalDelegatingHandler>().ToList();
        var second = handlers[0].DelegatingHandler;
        first.ShouldNotBe(second);
    }

    private void ThenTheProviderIsRegisteredAndReturnsHandlers<TOne, TWo>()
    {
        _serviceProvider = _services.BuildServiceProvider(true);
        var handlers = _serviceProvider.GetServices<GlobalDelegatingHandler>().ToList();
        handlers[0].DelegatingHandler.ShouldBeOfType<TOne>();
        handlers[1].DelegatingHandler.ShouldBeOfType<TWo>();
    }

    private void ThenTheProviderIsRegisteredAndReturnsSpecificHandlers<TOne, TWo>()
    {
        _serviceProvider = _services.BuildServiceProvider(true);
        var handlers = _serviceProvider.GetServices<DelegatingHandler>().ToList();
        handlers[0].ShouldBeOfType<TOne>();
        handlers[1].ShouldBeOfType<TWo>();
    }

    private void ThenTheProviderIsRegisteredAndReturnsSpecificAggregators<TOne, TWo>()
    {
        _serviceProvider = _services.BuildServiceProvider(true);
        var handlers = _serviceProvider.GetServices<IDefinedAggregator>().ToList();
        handlers[0].ShouldBeOfType<TOne>();
        handlers[1].ShouldBeOfType<TWo>();
    }

    private void ThenTheProviderIsRegisteredAndReturnsBothBuiltInAndCustomLoadBalancerCreators()
    {
        _serviceProvider = _services.BuildServiceProvider(true);
        var creators = _serviceProvider.GetServices<ILoadBalancerCreator>().ToList();
        creators.Count(c => c.GetType() == typeof(NoLoadBalancerCreator)).ShouldBe(1);
        creators.Count(c => c.GetType() == typeof(RoundRobinCreator)).ShouldBe(1);
        creators.Count(c => c.GetType() == typeof(CookieStickySessionsCreator)).ShouldBe(1);
        creators.Count(c => c.GetType() == typeof(LeastConnectionCreator)).ShouldBe(1);
        creators.Count(c => c.GetType() == typeof(DelegateInvokingLoadBalancerCreator<FakeCustomLoadBalancer>)).ShouldBe(1);
    }

    private class FakeCustomLoadBalancer : ILoadBalancer
    {
        public string Type => nameof(FakeCustomLoadBalancer);
        public Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext) => throw new NotImplementedException();
        public void Release(ServiceHostAndPort hostAndPort) => throw new NotImplementedException();
    }
}
