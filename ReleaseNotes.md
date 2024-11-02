## :fire: Hot fixing v[23.3.4](https://github.com/ThreeMammals/Ocelot/releases/tag/23.3.4) (version {0}) aka [October'24](https://github.com/ThreeMammals/Ocelot/milestone/7) release
> Read the Docs: [Ocelot 23.3](https://ocelot.readthedocs.io/en/{0}/)
> Hot fixed version: [23.3.4](https://github.com/ThreeMammals/Ocelot/releases/tag/23.3.4)
> Milestone: [October'24](https://github.com/ThreeMammals/Ocelot/milestone/7)

:heart: A heartfelt "Thank You" to **[Nikolai Masson](https://github.com/Niksson)** (@Niksson) and **[Nikolay Kuksov](https://github.com/kick2nick)** (@kick2nick) for their contributions!

### :information_source: About
This release provides minor bug fixes from the previous [23.3.4](https://github.com/ThreeMammals/Ocelot/releases/tag/23.3.4) release. All bugs have been addressed in the [October'24](https://github.com/ThreeMammals/Ocelot/milestone/7) milestone.

:notebook: For projects utilizing the [Service Discovery](https://ocelot.readthedocs.io/en/latest/features/servicediscovery.html) feature, it is recommended to update to this version to benefit from the unstable release [23.3.4](https://github.com/ThreeMammals/Ocelot/releases/tag/23.3.4), which includes fixes for both `Consul` and `Kube` discovery providers.

### :technologist: Technical Information
The Ocelot solution encountered a significant issue with the disabled scope validation of services in the DI-container, affecting both testing projects and the core library. Initially, this was not problematic when services were designed as singletons by previous contributors and our team. However, with the introduction of more scoped services by the Ocelot team, it became clear that our testing projects could not adequately handle them.
This patch introduces scope validation across all domains: unit tests, acceptance tests, and the core library itself. We advise always enabling scope validation in your custom Ocelot solutions, especially when dealing with numerous C# overridden classes in the DI-container and any attached custom functionality.

The patch enhances functionality for two primary [Service Discovery](https://github.com/ThreeMammals/Ocelot/blob/main/docs/features/servicediscovery.rst) providers:
- The [Ocelot.Provider.Consul](https://www.nuget.org/packages/Ocelot.Provider.Consul) provider. The addressed bug is issue #2178, reported on October 17, 2024.
  The `System.InvalidOperationException` error stating _"Cannot resolve scoped service 'Ocelot.Provider.Consul.Interfaces.IConsulServiceBuilder' from root provider"_ has been resolved.
  To clarify, the `IConsulServiceBuilder` service is a scoped service in DI, injected via the `AddConsul()` or [AddConsul&lt;T&gt;()](https://github.com/ThreeMammals/Ocelot/blob/main/docs/features/servicediscovery.rst#addconsult-method) methods. Therefore, the `DefaultConsulServiceBuilder` should also be a scoped service, with `HttpContext` injected to meet your development requirements.
- The [Ocelot.Provider.Kubernetes](https://www.nuget.org/packages/Ocelot.Provider.Kubernetes) provider had an issue reported as #977 on August 1, 2019.
  It involved a `System.InvalidOperationException` with the message: _"Cannot resolve scoped service 'KubeClient.IKubeApiClient' from root provider."_ This "invalid scopes" error occurred only in development mode, as release mode DLLs do not validate scopes. However, the `KubeApiClient` is designed to have a [scoped](https://github.com/tintoy/dotnet-kube-client/blob/84b055c885a2afe00781559ea400c0bd8cdfce6d/src/KubeClient.Extensions.DependencyInjection/ClientRegistrationExtensions.cs#L42-L43) lifetime. Acceptance tests passed because scope validation was disabled, and the `KubeClient` was [replaced](https://github.com/ThreeMammals/Ocelot/blob/414f63439d32ed9a3c09a77c86f035ed9c34aa56/test/Ocelot.AcceptanceTests/ServiceDiscovery/KubernetesServiceDiscoveryTests.cs#L315) with a singleton. This inconsistency was identified and reproduced by the old [977 issue](https://github.com/ThreeMammals/Ocelot/issues/977). As a temporary solution, the `IKubeApiClient` was [registered as a singleton](https://github.com/ThreeMammals/Ocelot/blob/e4bc9ff59d9defc385996a09be85fbb845e06af9/src/Ocelot.Provider.Kubernetes/OcelotBuilderExtensions.cs#L16).
  Looking ahead, our team intends to redesign the Kubernetes provider to have a default service builder that is scoped, similar to the Consul provider.

### :exclamation: Breaking Changes
Upgrading from [23.3.4](https://github.com/ThreeMammals/Ocelot/releases/tag/23.3.4) to [{0}](https://github.com/ThreeMammals/Ocelot/releases/tag/{0}) introduces **no breaking changes**. However, upgrading from [23.3.0](https://github.com/ThreeMammals/Ocelot/releases/tag/23.3.0) or earlier versions may result in some incompatibilities. For further information, please refer to the release notes of v[23.3.4](https://github.com/ThreeMammals/Ocelot/releases/tag/23.3.4).
