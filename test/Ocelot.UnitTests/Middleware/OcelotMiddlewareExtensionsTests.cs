using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ocelot.Administration;
using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.DependencyInjection;
using Ocelot.Errors;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Responses;
using System.Diagnostics;
using System.Reflection;

namespace Ocelot.UnitTests.Middleware;

public class OcelotMiddlewareExtensionsTests : UnitTest
{
    private readonly Mock<IInternalConfigurationCreator> _mockCreator;
    private readonly Mock<IInternalConfigurationRepository> _mockRepo;
    private readonly Mock<IOptionsMonitor<FileConfiguration>> _mockFileConfig;
    private readonly Mock<IInternalConfiguration> _mockInternalConfig;
    private readonly Mock<IOcelotLoggerFactory> _mockLoggerFactory;
    private readonly Mock<IOcelotLogger> _mockLogger;

    public OcelotMiddlewareExtensionsTests()
    {
        _mockInternalConfig = new Mock<IInternalConfiguration>();

        _mockCreator = new Mock<IInternalConfigurationCreator>();
        _mockCreator
            .Setup(x => x.Create(It.IsAny<FileConfiguration>()))
            .ReturnsAsync(new OkResponse<IInternalConfiguration>(_mockInternalConfig.Object));

        _mockRepo = new Mock<IInternalConfigurationRepository>();
        _mockRepo.Setup(x => x.AddOrReplace(It.IsAny<IInternalConfiguration>()))
            .Returns(new OkResponse());
        _mockRepo.Setup(x => x.Get())
            .Returns(new OkResponse<IInternalConfiguration>(_mockInternalConfig.Object));

        _mockFileConfig = new Mock<IOptionsMonitor<FileConfiguration>>();
        _mockFileConfig.Setup(x => x.CurrentValue).Returns(new FileConfiguration());
        _mockFileConfig
            .Setup(x => x.OnChange(It.IsAny<Action<FileConfiguration, string>>()))
            .Returns((IDisposable)null);

        _mockLogger = new Mock<IOcelotLogger>();
        _mockLoggerFactory = new Mock<IOcelotLoggerFactory>();
        _mockLoggerFactory
            .Setup(x => x.CreateLogger<OcelotDiagnosticListener>())
            .Returns(_mockLogger.Object);
    }

    [Fact]
    public async Task UseOcelot_NoArgs_ReturnsApplicationBuilder()
    {
        // Arrange
        var builder = GivenFullApplicationBuilder();

        // Act
        var result = await builder.UseOcelot();

        // Assert
        Assert.NotNull(result);
        Assert.Same(builder, result);
    }

    [Fact]
    public async Task UseOcelot_WithPipelineConfigAction_ActionIsInvoked()
    {
        // Arrange
        var builder = GivenFullApplicationBuilder();
        var actionInvoked = false;

        // Act
        var result = await builder.UseOcelot(config =>
        {
            actionInvoked = true;
        });

        // Assert
        Assert.NotNull(result);
        Assert.True(actionInvoked);
    }

    [Fact]
    public async Task UseOcelot_WithNullPipelineConfigAction_ReturnsApplicationBuilder()
    {
        // Arrange
        var builder = GivenFullApplicationBuilder();
        Action<OcelotPipelineConfiguration> action = null;

        // Act
        var result = await builder.UseOcelot(action);

        // Assert
        Assert.NotNull(result);
        Assert.Same(builder, result);
    }

    [Fact]
    public async Task UseOcelot_WithPipelineConfiguration_ReturnsApplicationBuilder()
    {
        // Arrange
        var builder = GivenFullApplicationBuilder();
        var pipelineConfig = new OcelotPipelineConfiguration();

        // Act
        var result = await builder.UseOcelot(pipelineConfig);

        // Assert
        Assert.NotNull(result);
        Assert.Same(builder, result);
    }

    [Fact]
    public async Task UseOcelot_WithBuilderAction_ActionIsInvoked()
    {
        // Arrange
        var builder = GivenLightweightApplicationBuilder();
        var actionInvoked = false;

        // Act
        var result = await builder.UseOcelot((app, config) =>
        {
            actionInvoked = true;
        });

        // Assert
        Assert.NotNull(result);
        Assert.True(actionInvoked);
    }

    [Fact]
    public async Task UseOcelot_WithBuilderActionAndConfig_ActionReceivesCorrectConfig()
    {
        // Arrange
        var builder = GivenLightweightApplicationBuilder();
        var expectedConfig = new OcelotPipelineConfiguration();
        OcelotPipelineConfiguration receivedConfig = null;

        // Act
        await builder.UseOcelot((app, config) =>
        {
            receivedConfig = config;
        }, expectedConfig);

        // Assert
        Assert.Same(expectedConfig, receivedConfig);
    }

    [Fact]
    public async Task UseOcelot_WithBuilderActionAndNullConfig_UsesDefaultConfig()
    {
        // Arrange
        var builder = GivenLightweightApplicationBuilder();
        OcelotPipelineConfiguration receivedConfig = null;

        // Act
        await builder.UseOcelot((app, config) =>
        {
            receivedConfig = config;
        }, (OcelotPipelineConfiguration)null);

        // Assert
        Assert.NotNull(receivedConfig);
        Assert.IsType<OcelotPipelineConfiguration>(receivedConfig);
    }

    [Fact]
    public async Task UseOcelot_WithNullBuilderAction_ReturnsApplicationBuilder()
    {
        // Arrange
        var builder = GivenLightweightApplicationBuilder();
        Action<IApplicationBuilder, OcelotPipelineConfiguration> action = null;

        // Act
        var result = await builder.UseOcelot(action);

        // Assert
        Assert.NotNull(result);
        Assert.Same(builder, result);
    }

    [Fact]
    public async Task UseOcelot_WithBuilderActionAndConfig_SetsNextMiddlewareNameProperty()
    {
        // Arrange
        var builder = GivenLightweightApplicationBuilder();

        // Act
        await builder.UseOcelot((app, config) => { }, new OcelotPipelineConfiguration());

        // Assert
        Assert.True(builder.Properties.ContainsKey("analysis.NextMiddlewareName"));
        Assert.Equal("TransitionToOcelotMiddleware", builder.Properties["analysis.NextMiddlewareName"]);
    }

    [Fact]
    public async Task UseOcelot_WhenConfigCreatorReturnsError_ThrowsException()
    {
        // Arrange
        var error = new FakeError("Configuration creation failed");
        _mockCreator
            .Setup(x => x.Create(It.IsAny<FileConfiguration>()))
            .ReturnsAsync(new ErrorResponse<IInternalConfiguration>(error));

        var builder = GivenLightweightApplicationBuilder();

        // Act, Assert
        await Assert.ThrowsAnyAsync<Exception>(() => builder.UseOcelot((app, config) => { }));
    }

    [Fact]
    public async Task UseOcelot_WhenConfigRepoGetReturnsError_ThrowsException()
    {
        // Arrange
        var error = new FakeError("Repository error");
        _mockRepo.Setup(x => x.Get())
            .Returns(new ErrorResponse<IInternalConfiguration>(error));

        var builder = GivenLightweightApplicationBuilder();

        // Act, Assert
        await Assert.ThrowsAnyAsync<Exception>(() => builder.UseOcelot((app, config) => { }));
    }

    [Fact]
    public async Task UseOcelot_WhenConfigRepoGetReturnsNullData_ThrowsException()
    {
        // Arrange
        _mockRepo.Setup(x => x.Get())
            .Returns(new OkResponse<IInternalConfiguration>(null));

        var builder = GivenLightweightApplicationBuilder();

        // Act, Assert
        await Assert.ThrowsAnyAsync<Exception>(() => builder.UseOcelot((app, config) => { }));
    }

    [Fact]
    public async Task UseOcelot_WhenAdminPathExistsAndFileSetterReturnsError_ThrowsException()
    {
        // Arrange
        var mockAdminPath = new Mock<IAdministrationPath>();
        var mockSetter = new Mock<IFileConfigurationSetter>();
        var error = new FakeError("File config set error");
        mockSetter.Setup(x => x.Set(It.IsAny<FileConfiguration>()))
            .ReturnsAsync(new ErrorResponse(error));

        var builder = GivenLightweightApplicationBuilder(mockAdminPath.Object, mockSetter.Object);

        // Act, Assert
        await Assert.ThrowsAnyAsync<Exception>(() => builder.UseOcelot((app, config) => { }));
    }

    [Fact]
    public async Task UseOcelot_WhenAdminPathExistsAndFileSetterReturnsNull_ThrowsException()
    {
        // Arrange
        var mockAdminPath = new Mock<IAdministrationPath>();
        var mockSetter = new Mock<IFileConfigurationSetter>();
        mockSetter.Setup(x => x.Set(It.IsAny<FileConfiguration>()))
            .ReturnsAsync((Response)null);

        var builder = GivenLightweightApplicationBuilder(mockAdminPath.Object, mockSetter.Object);

        // Act, Assert
        await Assert.ThrowsAnyAsync<Exception>(() => builder.UseOcelot((app, config) => { }));
    }

    [Fact]
    public async Task UseOcelot_WhenAdminPathExistsAndFileSetterSucceeds_ReturnsApplicationBuilder()
    {
        // Arrange
        var mockAdminPath = new Mock<IAdministrationPath>();
        var mockSetter = new Mock<IFileConfigurationSetter>();
        mockSetter.Setup(x => x.Set(It.IsAny<FileConfiguration>()))
            .ReturnsAsync(new OkResponse());

        var builder = GivenLightweightApplicationBuilder(mockAdminPath.Object, mockSetter.Object);

        // Act
        var result = await builder.UseOcelot((app, config) => { });

        // Assert
        Assert.NotNull(result);
        Assert.Same(builder, result);
    }

    [Fact]
    public async Task UseOcelot_WithMiddlewareConfigurationDelegates_DelegatesAreInvoked()
    {
        // Arrange
        var delegateInvoked = false;
        OcelotMiddlewareConfigurationDelegate configDelegate = _ =>
        {
            delegateInvoked = true;
            return Task.CompletedTask;
        };

        var builder = GivenLightweightApplicationBuilder(configDelegates: new[] { configDelegate });

        // Act
        await builder.UseOcelot((app, config) => { });

        // Assert
        Assert.True(delegateInvoked);
    }

    [Fact]
    public async Task UseOcelot_WithMultipleMiddlewareConfigurationDelegates_AllDelegatesAreInvoked()
    {
        // Arrange
        var invokedCount = 0;
        OcelotMiddlewareConfigurationDelegate delegate1 = _ => { invokedCount++; return Task.CompletedTask; };
        OcelotMiddlewareConfigurationDelegate delegate2 = _ => { invokedCount++; return Task.CompletedTask; };
        OcelotMiddlewareConfigurationDelegate delegate3 = _ => { invokedCount++; return Task.CompletedTask; };

        var builder = GivenLightweightApplicationBuilder(configDelegates: new[] { delegate1, delegate2, delegate3 });

        // Act
        await builder.UseOcelot((app, config) => { });

        // Assert
        Assert.Equal(3, invokedCount);
    }

    [Fact]
    public async Task UseOcelot_WithBuilderActionOverloadNoConfig_InvokesActionWithConfiguration()
    {
        // Arrange
        var builder = GivenLightweightApplicationBuilder();
        var actionInvoked = false;

        // Act
        var result = await builder.UseOcelot((app, config) =>
        {
            actionInvoked = true;
            Assert.NotNull(config);
        });

        // Assert
        Assert.NotNull(result);
        Assert.True(actionInvoked);
    }

    [Fact]
    public async Task UseOcelot_WhenConfigCreatorReturnsError_ExceptionMessageContainsErrors()
    {
        // Arrange
        const string errorMessage = "Config creation failed with specific error";
        var error = new FakeError(errorMessage);
        _mockCreator
            .Setup(x => x.Create(It.IsAny<FileConfiguration>()))
            .ReturnsAsync(new ErrorResponse<IInternalConfiguration>(error));

        var builder = GivenLightweightApplicationBuilder();

        // Act
        var exception = await Assert.ThrowsAnyAsync<Exception>(() => builder.UseOcelot((app, config) => { }));

        // Assert
        Assert.Contains("Unable to start Ocelot", exception.Message);
        Assert.Contains(errorMessage, exception.Message);
    }

    [Fact]
    public async Task OnChange_WhenFileConfigChanges_CreatesAndStoresNewConfiguration()
    {
        // Arrange: capture the callback registered with OnChange
        Action<FileConfiguration, string> capturedCallback = null;
        _mockFileConfig
            .Setup(x => x.OnChange(It.IsAny<Action<FileConfiguration, string>>()))
            .Callback<Action<FileConfiguration, string>>(cb => capturedCallback = cb)
            .Returns((IDisposable)null);

        // Signal when AddOrReplace is invoked for the second time (the OnChange-triggered call)
        var addOrReplaceCount = 0;
        var onChangeCompletedTcs = new TaskCompletionSource();
        _mockRepo
            .Setup(x => x.AddOrReplace(It.IsAny<IInternalConfiguration>()))
            .Callback<IInternalConfiguration>(_ =>
            {
                if (++addOrReplaceCount >= 2)
                    onChangeCompletedTcs.TrySetResult();
            })
            .Returns(new OkResponse());

        var builder = GivenLightweightApplicationBuilder();
        await builder.UseOcelot((app, config) => { });

        Assert.NotNull(capturedCallback);

        // Act: simulate a configuration file change
        capturedCallback(new FileConfiguration(), "default");

        // Wait for the async void callback to complete
        await onChangeCompletedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert: Create and AddOrReplace are each called a second time for the changed config
        _mockCreator.Verify(x => x.Create(It.IsAny<FileConfiguration>()), Times.Exactly(2));
        _mockRepo.Verify(x => x.AddOrReplace(It.IsAny<IInternalConfiguration>()), Times.Exactly(2));
    }

    [Fact]
    public async Task UseOcelot_WhenConfigRepoGetReturnsNull_ThrowsException()
    {
        // Arrange: Get() returns null so ocelotConfiguration?.Data takes the null-conditional branch on line 145
        _mockRepo.Setup(x => x.Get())
            .Returns((Response<IInternalConfiguration>)null);

        var builder = GivenLightweightApplicationBuilder();

        // Act, Assert
        await Assert.ThrowsAnyAsync<Exception>(() => builder.UseOcelot((app, config) => { }));
    }

    private IApplicationBuilder GivenFullApplicationBuilder()
    {
        var configRoot = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configRoot);
        services.AddSingleton(GetHostingEnvironment());
        services.AddOcelot();

        // Override with mocks
        services.AddSingleton(_mockCreator.Object);
        services.AddSingleton(_mockRepo.Object);
        services.AddSingleton(_mockFileConfig.Object);
        services.AddSingleton(new DiagnosticListener("OcelotTest"));

        var provider = services.BuildServiceProvider(true);
        return new ApplicationBuilder(provider);
    }

    private IApplicationBuilder GivenLightweightApplicationBuilder(
        IAdministrationPath adminPath = null,
        IFileConfigurationSetter fileConfigSetter = null,
        IEnumerable<OcelotMiddlewareConfigurationDelegate> configDelegates = null)
    {
        // First pass: build a provider so OcelotDiagnosticListener can receive IServiceProvider
        var bootstrap = new ServiceCollection();
        RegisterLightweightServices(bootstrap, adminPath, fileConfigSetter, configDelegates);
        var bootstrapProvider = bootstrap.BuildServiceProvider(true);

        // Second pass: register the fully-constructed OcelotDiagnosticListener
        var ocelotDiagnosticListener = new OcelotDiagnosticListener(_mockLoggerFactory.Object, bootstrapProvider);
        var services = new ServiceCollection();
        RegisterLightweightServices(services, adminPath, fileConfigSetter, configDelegates);
        services.AddSingleton(ocelotDiagnosticListener);

        var finalProvider = services.BuildServiceProvider(true);
        return new ApplicationBuilder(finalProvider);
    }

    private void RegisterLightweightServices(
        IServiceCollection services,
        IAdministrationPath adminPath,
        IFileConfigurationSetter fileConfigSetter,
        IEnumerable<OcelotMiddlewareConfigurationDelegate> configDelegates)
    {
        services.AddSingleton(_mockFileConfig.Object);
        services.AddSingleton(_mockCreator.Object);
        services.AddSingleton(_mockRepo.Object);
        services.AddSingleton(_mockLoggerFactory.Object);
        services.AddSingleton(new DiagnosticListener("OcelotTest"));

        if (adminPath != null)
        {
            services.AddSingleton(adminPath);
        }

        if (fileConfigSetter != null)
        {
            services.AddSingleton(fileConfigSetter);
        }

        foreach (var configDelegate in configDelegates ?? Enumerable.Empty<OcelotMiddlewareConfigurationDelegate>())
        {
            services.AddSingleton(configDelegate);
        }
    }

    private static IWebHostEnvironment GetHostingEnvironment()
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment
            .Setup(e => e.ApplicationName)
            .Returns(typeof(OcelotMiddlewareExtensionsTests).GetTypeInfo().Assembly.GetName().Name);
        return environment.Object;
    }

    private sealed class FakeError : Error
    {
        public FakeError(string message)
            : base(message, OcelotErrorCode.UnknownError, 500) { }
    }
}
