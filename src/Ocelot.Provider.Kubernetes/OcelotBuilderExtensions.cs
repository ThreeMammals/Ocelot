using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Provider.Kubernetes.Interfaces;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace Ocelot.Provider.Kubernetes;

public static class OcelotBuilderExtensions
{
    /// <summary>
    /// Adds Kubernetes (K8s) services with or without using a pod service account.
    /// <para>By default, <paramref name="usePodServiceAccount"/> is set to <see langword="true"/>, which means using a pod service account.</para>
    /// </summary>
    /// <remarks>If <paramref name="usePodServiceAccount"/> is <see langword="false"/>, it internally injects an <see cref="IOptions{T}"/> configuration section object (where TOptions is <see cref="KubeClientOptions"/>) to configure <see cref="KubeApiClient"/>.</remarks>
    /// <param name="builder">The Ocelot Builder instance.</param>
    /// <param name="usePodServiceAccount">If true, it creates the client from pod service account.</param>
    /// <returns>The reference to the same extended <see cref="IOcelotBuilder"/> object.</returns>
    public static IOcelotBuilder AddKubernetes(this IOcelotBuilder builder, bool usePodServiceAccount = true)
    {
        builder.Services

            //.AddSingleton<IKubeApiClient, KubeApiClient>(KubeApiClientFactory) // TODO Revert to .AddKubeClient(usePodServiceAccount) after making KubernetesProviderFactory as IServiceDiscoveryProviderFactory 
            //.AddKubeApiClientFactory(usePodServiceAccount)
            //.AddKubeClient(usePodServiceAccount)
            .AddSingleton<IKubeApiClientFactory, KubeApiClientFactory>()
            .AddSingleton<IKubeApiClient, KubeApiClient>(ResolveWithKubeApiClientFactory)

            .AddSingleton(KubernetesProviderFactory.Get) // TODO Must be removed after deprecation of ServiceDiscoveryFinderDelegate in favor of IServiceDiscoveryProviderFactory
            .AddSingleton<IKubeServiceBuilder, KubeServiceBuilder>()
            .AddSingleton<IKubeServiceCreator, KubeServiceCreator>();
        return builder;

        KubeApiClient ResolveWithKubeApiClientFactory(IServiceProvider sp)
        {
            var factory = sp.GetService<IKubeApiClientFactory>();
            return factory.Get(usePodServiceAccount);
        }
    }

    /// <summary>
    /// Adds Kubernetes (K8s) services without using a pod service account, explicitly calling a factory-action to initialize configuration options for <see cref="KubeApiClient"/>.
    /// <para>Before adding services, it internally configures options by registering the action in DI; thus an <see cref="IOptions{T}"/> (where TOptions is <see cref="KubeClientOptions"/>) object becomes available in the DI container.</para>
    /// </summary>
    /// <remarks>It operates in 2 modes:
    /// <list type="number">
    ///   <item>If <paramref name="configureOptions"/> is provided (action is not null), it calls the action ignoring all optional arguments.</item>
    ///   <item>If <paramref name="configureOptions"/> is not provided (action is null), it reads the global <see cref="FileGlobalConfiguration.ServiceDiscoveryProvider"/> options and reuses them to initialize the following properties: <see cref="KubeClientOptions.ApiEndPoint"/>, <see cref="KubeClientOptions.AccessToken"/>, and <see cref="KubeClientOptions.KubeNamespace"/>, finally initializing the rest of the properties with optional arguments.</item>
    /// </list>
    /// </remarks>
    /// <param name="builder">The Ocelot Builder instance.</param>
    /// <param name="configureOptions">An action to initialize <see cref="KubeClientOptions"/> of the client. It can be null: read the remarks.</param>
    /// <param name="defaultScheme">Optional scheme to build <see cref="KubeClientOptions.ApiEndPoint"/> URI when the global <see cref="FileServiceDiscoveryProvider.Scheme"/> is unknown, defaulting to 'https' aka <see cref="Uri.UriSchemeHttps"/>.</param>
    /// <param name="defaultHost">Optional host to build <see cref="KubeClientOptions.ApiEndPoint"/> URI when the global <see cref="FileServiceDiscoveryProvider.Host"/> is unknown, defaulting to 'localhost' aka <see cref="IPAddress.Loopback"/>.</param>
    /// <param name="defaultPort">Optional port to build <see cref="KubeClientOptions.ApiEndPoint"/> URI when the global <see cref="FileServiceDiscoveryProvider.Port"/> is unknown, defaulting to 443.</param>
    /// <param name="defaultNamespace">Optional namespace to initialize <see cref="KubeClientOptions.KubeNamespace"/> option when the global <see cref="FileServiceDiscoveryProvider.Namespace"/> is unknown, defaulting to 'default'.</param>
    /// <param name="username">Optional username to initialize the <see cref="KubeClientOptions.Username"/> option.</param>
    /// <param name="password">Optional password to initialize the <see cref="KubeClientOptions.Password"/> option.</param>
    /// <param name="accessTokenCommand">Optional command to initialize the <see cref="KubeClientOptions.AccessTokenCommand"/> option.</param>
    /// <param name="accessTokenCommandArguments">Optional arguments to initialize the <see cref="KubeClientOptions.AccessTokenCommandArguments"/> option.</param>
    /// <param name="accessTokenSelector">Optional selector to initialize the <see cref="KubeClientOptions.AccessTokenSelector"/> option.</param>
    /// <param name="accessTokenExpirySelector">Optional selector to initialize the <see cref="KubeClientOptions.AccessTokenExpirySelector"/> option.</param>
    /// <param name="initialAccessToken">Optional token to initialize the <see cref="KubeClientOptions.InitialAccessToken"/> option.</param>
    /// <param name="initialTokenExpiryUtc">Optional date-time to initialize the <see cref="KubeClientOptions.InitialTokenExpiryUtc"/> option.</param>
    /// <param name="clientCertificate">Optional certificate to initialize the <see cref="KubeClientOptions.ClientCertificate"/> option.</param>
    /// <param name="certificationAuthorityCertificate">Optional certificate to initialize the <see cref="KubeClientOptions.CertificationAuthorityCertificate"/> option.</param>
    /// <param name="allowInsecure">Optional verification flag to initialize the <see cref="KubeClientOptions.AllowInsecure"/> option, defaulting to false.</param>
    /// <param name="authStrategy">Optional strategy to initialize the <see cref="KubeClientOptions.AuthStrategy"/> option, defaulting to <see cref="KubeAuthStrategy.BearerToken"/>.</param>
    /// <param name="logHeaders">Optional log flag to initialize the <see cref="KubeClientOptions.LogHeaders"/> option, defaulting to false.</param>
    /// <param name="logPayloads">Optional log flag to initialize the <see cref="KubeClientOptions.LogPayloads"/> option, defaulting to false.</param>
    /// <param name="loggerFactory">Optional factory to initialize the <see cref="KubeClientOptions.LoggerFactory"/> option.</param>
    /// <param name="modelTypeAssemblies">Optional list to add assemblies to the <see cref="KubeClientOptions.ModelTypeAssemblies"/> option, defaulting to empty list.</param>
    /// <param name="environmentVariables">Optional dictionary to initialize the <see cref="KubeClientOptions.EnvironmentVariables"/> option, defaulting to empty list.</param>
    /// <returns>The reference to the same extended <see cref="IOcelotBuilder"/> object.</returns>
    public static IOcelotBuilder AddKubernetes(this IOcelotBuilder builder, Action<KubeClientOptions> configureOptions, // required params
        string defaultScheme = null, string defaultHost = null, int? defaultPort = null, string defaultNamespace = null, // optional params 
        string username = null, string password = null,
        string accessTokenCommand = null, string accessTokenCommandArguments = null, string accessTokenSelector = null, string accessTokenExpirySelector = null,
        string initialAccessToken = null, DateTime? initialTokenExpiryUtc = null,
        X509Certificate2 clientCertificate = null, X509Certificate2 certificationAuthorityCertificate = null,
        bool? allowInsecure = null, KubeAuthStrategy? authStrategy = null,
        bool? logHeaders = null, bool? logPayloads = null, ILoggerFactory loggerFactory = null,
        List<Assembly> modelTypeAssemblies = null, Dictionary<string, string> environmentVariables = null)
    {
        configureOptions ??= Configure;
        builder.Services.AddOptions<KubeClientOptions>().Configure(configureOptions);
        return builder.AddKubernetes(false);

        void Configure(KubeClientOptions options)
        {
            // Initialize properties with values coming from global ServiceDiscoveryProvider options
            var key = $"{nameof(FileConfiguration.GlobalConfiguration)}:{nameof(FileGlobalConfiguration.ServiceDiscoveryProvider)}";
            var section = builder.Configuration.GetSection(key);
            var scheme = section.Str(nameof(FileServiceDiscoveryProvider.Scheme), defaultScheme ?? Uri.UriSchemeHttps);
            var host = section.Str(nameof(FileServiceDiscoveryProvider.Host), defaultHost ?? IPAddress.Loopback.ToString());
            var port = section.Int(nameof(FileServiceDiscoveryProvider.Port), defaultPort ?? 443);
            options.ApiEndPoint = new UriBuilder(scheme, host, port).Uri;
            options.KubeNamespace = section.Str(nameof(FileServiceDiscoveryProvider.Namespace), defaultNamespace ?? "default");
            options.AccessToken = section.GetValue<string>(nameof(FileServiceDiscoveryProvider.Token));

            // Initialize properties with values coming from optional arguments
            options.AuthStrategy = authStrategy ?? KubeAuthStrategy.BearerToken;
            options.AllowInsecure = allowInsecure ?? false;
            options.AccessTokenCommand = accessTokenCommand;
            options.AccessTokenCommandArguments = accessTokenCommandArguments;
            options.AccessTokenExpirySelector = accessTokenExpirySelector;
            options.AccessTokenSelector = accessTokenSelector;
            options.CertificationAuthorityCertificate = certificationAuthorityCertificate;
            options.ClientCertificate = clientCertificate;
            options.EnvironmentVariables = environmentVariables ?? new();
            options.InitialAccessToken = initialAccessToken;
            options.InitialTokenExpiryUtc = initialTokenExpiryUtc;
            options.LoggerFactory = loggerFactory;
            options.LogHeaders = logHeaders ?? false;
            options.LogPayloads = logPayloads ?? false;
            options.ModelTypeAssemblies.AddRange(modelTypeAssemblies ?? new());
            options.Password = password;
            options.Username = username;
        }
    }

    private static string Str(this IConfigurationSection sec, string key, string defaultValue)
    {
        string val = sec.GetValue<string>(key, defaultValue);
        return string.IsNullOrEmpty(val) ? defaultValue : val;
    }

    private static int Int(this IConfigurationSection sec, string key, int defaultValue)
    {
        int val = sec.GetValue<int>(key, defaultValue);
        return val <= 0 ? defaultValue : val;
    }
}
