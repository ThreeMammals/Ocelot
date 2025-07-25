﻿using KubeClient;
using KubeClient.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Provider.Kubernetes;
using Ocelot.Provider.Kubernetes.Interfaces;
using Ocelot.ServiceDiscovery;
using Ocelot.ServiceDiscovery.Providers;
using System.Runtime.CompilerServices;

namespace Ocelot.UnitTests.Kubernetes;

public sealed class KubernetesProviderFactoryTests : KubernetesProviderFactoryTestsBase
{
    [Theory]
    [Trait("Bug", "977")]
    [InlineData(typeof(Kube))]
    [InlineData(typeof(PollKube))]
    [InlineData(typeof(WatchKube))]
    public void CreateProvider_ClientHasOriginalLifetimeWithEnabledScopesValidation_ShouldResolveProvider(Type providerType)
    {
        // Arrange
        _builder.AddKubernetes();
        var endpointClient = new Mock<IEndPointClient>();
        endpointClient.Setup(x => x.Watch(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Mock.Of<IObservable<IResourceEventV1<EndpointsV1>>>());
        
        var kubeClient = new Mock<IKubeApiClient>();
        kubeClient.Setup(x => x.ResourceClient(It.IsAny<Func<IKubeApiClient, IEndPointClient>>()))
            .Returns(endpointClient.Object);
        var descriptor = _builder.Services.First(x => x.ServiceType == typeof(IKubeApiClient));
        _builder.Services.Replace(ServiceDescriptor.Describe(descriptor.ServiceType, _ => kubeClient.Object, descriptor.Lifetime));

        // Act
        var actual = CreateProvider(providerType.Name);

        // Assert
        actual.ShouldNotBeNull().ShouldBeOfType(providerType);
    }
    
    [Theory]
    [Trait("Bug", "977")]
    [InlineData(nameof(Kube))]
    [InlineData(nameof(PollKube))]
    [InlineData(nameof(WatchKube))]
    public void CreateProvider_ClientHasScopedLifetimeWithEnabledScopesValidation_ShouldFailToResolve(string providerType)
    {
        // Arrange
        _builder.AddKubernetes();
        var descriptor = ServiceDescriptor.Describe(typeof(IKubeApiClient), _ => Mock.Of<IKubeApiClient>(), ServiceLifetime.Scoped);
        _builder.Services.Replace(descriptor);

        // Act
        IServiceDiscoveryProvider actual = null;
        var func = () => actual = CreateProvider(providerType);

        // Assert
        var ex = func.ShouldThrow<InvalidOperationException>();
        ex.Message.ShouldContain("Cannot resolve scoped service 'KubeClient.IKubeApiClient' from root provider");
        actual.ShouldBeNull();
    }

    [Fact]
    [Trait("Feat", "2256")]
    public void CreateProvider_KubeApiClientFactory_ShouldCreateFromOptions()
    {
        // Arrange
        _builder.AddKubernetes(false); // !!!

        // In app user must setup by the following:
        //MyOptions options = new();
        //_builder.Configuration.GetSection(nameof(MyOptions)).Bind(options);
        var options = new Mock<IOptions<KubeClientOptions>>();
        options.SetupGet(x => x.Value).Returns(new KubeClientOptions
        {
            ApiEndPoint = new UriBuilder(Uri.UriSchemeHttps, IPAddress.Loopback.ToString(), PortFinder.GetRandomPort()).Uri,
            ClientCertificate = FakeKubeApiClientFactory.CreateCertificate(),
            KubeNamespace = nameof(CreateProvider_KubeApiClientFactory_ShouldCreateFromOptions),
        });
        _builder.Services.AddSingleton<IOptions<KubeClientOptions>>(options.Object);

        // Act
        var actual = CreateProvider(nameof(Kube));

        // Assert
        actual.ShouldNotBeNull().ShouldBeOfType<Kube>();
    }

    [Fact]
    [Trait("Feat", "2256")]
    public void CreateProvider_HasConfigureOptions_ShouldCallConfigure()
    {
        // Arrange
        _builder.AddKubernetes(configureOptions: null, username: "myUser"); // !!!

        // Act, Assert
        var actual = CreateProvider(nameof(Kube));
        actual.ShouldNotBeNull().ShouldBeOfType<Kube>();

        // Act, Assert
        var provider = _builder.Services.BuildServiceProvider(true);
        var o = provider.GetService<IOptions<KubeClientOptions>>().ShouldNotBeNull();
        o.Value.ShouldNotBeNull().Username.ShouldBe("myUser");

        // Act, Assert
        var configureOptions = provider.GetService<IConfigureOptions<KubeClientOptions>>().ShouldNotBeNull();
        var opts = new KubeClientOptions();
        configureOptions.Configure(opts);
        opts.Username.ShouldBe("myUser");
    }
}

[Collection(nameof(SequentialTests))]
public sealed class KubernetesProviderFactorySequentialTests : KubernetesProviderFactoryTestsBase
{
    [Fact]
    [Trait("Feat", "2256")]
    public async Task CreateProvider_KubeApiClientFactory_ShouldCreateFromPodServiceAccount()
    {
        // Arrange
        _builder.AddKubernetes(true); // !!!
        var serviceAccountPath = Path.Combine(AppContext.BaseDirectory, TestID);
        var stub = new FakeKubeApiClientFactory(null, null, serviceAccountPath);
        var original = _builder.Services.First(x => x.ServiceType == typeof(IKubeApiClientFactory));
        var descriptor = ServiceDescriptor.Describe(original.ServiceType, _ => stub, original.Lifetime);
        _builder.Services.Replace(descriptor);

        var expectedHost = IPAddress.Loopback.ToString();
        Environment.SetEnvironmentVariable("KUBERNETES_SERVICE_HOST", expectedHost);
        int expectedPort = PortFinder.GetRandomPort();
        Environment.SetEnvironmentVariable("KUBERNETES_SERVICE_PORT", expectedPort.ToString());

        folders.Add(serviceAccountPath);
        if (!Directory.Exists(serviceAccountPath))
        {
            Directory.CreateDirectory(serviceAccountPath);
        }

        var path = Path.Combine(serviceAccountPath, "namespace");
        await File.WriteAllTextAsync(path, nameof(CreateProvider_KubeApiClientFactory_ShouldCreateFromPodServiceAccount));
        files.Add(path);

        path = Path.Combine(serviceAccountPath, "token");
        await File.WriteAllTextAsync(path, TestID);
        files.Add(path);

        path = Path.Combine(serviceAccountPath, "ca.crt");
        await FakeKubeApiClientFactory.CreateCertificate(path);
        files.Add(path);

        // Act
        var actualProvider = CreateProvider(nameof(Kube));

        // Assert
        actualProvider.ShouldNotBeNull().ShouldBeOfType<Kube>();
        stub.ShouldNotBeNull();
        stub.Actual.ShouldNotBeNull();
        stub.Actual.ApiEndPoint.ShouldNotBeNull();
        stub.Actual.ApiEndPoint.Host.ShouldBe(expectedHost);
        stub.Actual.ApiEndPoint.Port.ShouldBe(expectedPort);
        stub.Actual.DefaultNamespace.ShouldNotBeNull(nameof(CreateProvider_KubeApiClientFactory_ShouldCreateFromPodServiceAccount));
        Environment.SetEnvironmentVariable("KUBERNETES_SERVICE_PORT", null);
    }

    [Fact]
    [Trait("Bug", "2299")]
    public void Bug2299_StepsToReproduce_ShouldNotThrowExceptionByPathCombine()
    {
        // Arrange
        _builder.AddKubernetes(); // !!!
        Environment.SetEnvironmentVariable("KUBERNETES_SERVICE_HOST", "localhost");
        Environment.SetEnvironmentVariable("KUBERNETES_SERVICE_PORT", PortFinder.GetRandomPort().ToString());

        // Act
        var ex = Assert.ThrowsAny<Exception>(
            () => CreateProvider(nameof(Kube)));

        // Assert
        ex.ShouldNotBeOfType<ArgumentNullException>();
        ex.ShouldBeOfType<DirectoryNotFoundException>();
        ex.StackTrace.ShouldContain("at KubeClient.KubeClientOptions.FromPodServiceAccount(String serviceAccountPath)");
        ex.StackTrace.ShouldNotContain("at System.IO.Path.Combine(String path1, String path2)");
        ex.Message.ShouldNotBe("Value cannot be null. (Parameter 'path1')");
        Environment.SetEnvironmentVariable("KUBERNETES_SERVICE_PORT", null);
    }
}

public class KubernetesProviderFactoryTestsBase : FileUnitTest
{
    protected readonly IOcelotBuilder _builder;

    public KubernetesProviderFactoryTestsBase()
    {
        var config = new FileConfiguration();
        config.GlobalConfiguration.ServiceDiscoveryProvider = new()
        {
            Scheme = Uri.UriSchemeHttp,
            Host = "localhost",
            Port = 888,
            Namespace = nameof(KubernetesProviderFactoryTests),
            Token = TestID,
        };
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddOcelot(config, null, MergeOcelotJson.ToMemory)
            .Build();
        _builder = new ServiceCollection().AddOcelot(configuration);
    }

    protected IServiceDiscoveryProvider CreateProvider(string providerType)
    {
        var serviceProvider = _builder.Services.BuildServiceProvider(true);
        var config = GivenServiceProvider(providerType);
        var route = GivenRoute();
        return serviceProvider
            .GetRequiredService<ServiceDiscoveryFinderDelegate>() // returns KubernetesProviderFactory.Get instance
            .Invoke(serviceProvider, config, route);
    }

    protected static ServiceProviderConfiguration GivenServiceProvider(string type) => new(
        type: type,
        scheme: string.Empty,
        host: string.Empty,
        port: 1,
        token: string.Empty,
        configurationKey: string.Empty,
        pollingInterval: 9_000);

    private static DownstreamRoute GivenRoute([CallerMemberName] string serviceName = nameof(KubernetesProviderFactoryTests))
        => new DownstreamRouteBuilder().WithServiceName(serviceName).Build();
}
