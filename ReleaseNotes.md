<!--
{{0}} -> {0}
{{1}} -> {1}
Tag to substitute: {0}
https://www.nuget.org/packages/Ocelot/{0}
-->
## Upgrade to [.NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) SDK (TFM `net10.0`, version 25.0) a.k.a. [.NET 10](https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/) release
> **Milestone: [.NET 10](https://github.com/ThreeMammals/Ocelot/milestone/13)** 👈 
> Codenamed: [.NET 10](https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/)
> Read the Docs: [Ocelot 25.0](https://ocelot.readthedocs.io/en/25.0/) with [PDF](https://ocelot.readthedocs.io/_/downloads/en/25.0/pdf/)
> Target Framework Monikers: `net8.0`, `net9.0`, `net10.0`

### :information_source: About
On November 11th, 2025, the [.NET team](https://devblogs.microsoft.com/dotnet/author/dotnet) announced the release of the [.NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) framework:

* .NET Blog: [Announcing .NET 10](https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/)

This major release upgrades the [Ocelot](https://www.nuget.org/packages/Ocelot#supportedframeworks-body-tab) package [TFMs](https://learn.microsoft.com/en-us/dotnet/standard/frameworks#latest-versions) to **`net10.0`** in addition to the current `net8.0` and `net9.0`. Thus, the current Ocelot [supported frameworks](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core#lifecycle) are .NET 8 LTS, .NET 9 STS, and **.NET 10** LTS.

Additionally, in this major release the Ocelot team focused on preparing the codebase and its ecosystem for the **.NET 10** SDK ([.NET 10](https://github.com/ThreeMammals/Ocelot/milestone/13) milestone), while also extracting several [integrated extension packages](https://www.nuget.org/profiles/ThreeMammals) from [the v24.1 monorepo](https://github.com/ThreeMammals/Ocelot/tree/24.1.0/src) into their own [dedicated repositories](https://github.com/orgs/ThreeMammals/repositories?q=Ocelot.) to speed up delivery and reduce release coupling. As a result of these [DevOps](https://github.com/ThreeMammals/Ocelot/labels/DevOps) activities, the Ocelot repository now hosts only two package projects — [Ocelot](https://github.com/ThreeMammals/Ocelot/tree/develop/src) (`src` folder) and [Ocelot.Testing](https://github.com/ThreeMammals/Ocelot/tree/develop/testing) (`testing` folder) — along with two solutions, `Ocelot.slnx` and `Ocelot.Samples.slnx`, which can be opened or built only if the **.NET 10** SDK is installed. Since the extension packages now live in separate repositories, a new [Release Radar](https://github.com/ThreeMammals/Ocelot#satellite-release-radar) status page has been introduced to track their progress.

This major version also includes the following feature updates:
- A built-in [Quality of Service](https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/qualityofservice.rst) (QoS) circuit breaker (express edition) that works out of the box without requiring the [Polly](https://www.nuget.org/packages/Polly/) library.
- Significant enhancements to the [Websockets](https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/websockets.rst) feature, including [Websockets middleware](https://github.com/ThreeMammals/Ocelot/blob/develop/src/WebSockets/WebSocketsProxyMiddleware.cs) overriding and the addition of [Security Options](https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/routing.rst#security-options-6) to the WebSockets pipeline.
- Improved [Configuration](https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/configuration.rst) experience that merges multiple `ocelot.*.json` files, keeping users' [extended properties](https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/configuration.rst#extend-with-custom-properties-6) available during Ocelot app startup and at runtime via the standard `IConfiguration` service in the DI container.

Finally, because the repository has a [new folder structure](https://github.com/ThreeMammals/Ocelot#file_folder-repository-structure), the development team recommends recloning or even reforking the Ocelot repository for a successful upgrade to the new version in order to avoid potential build errors.

As a best practice, the release strategy has been aligned with [.NET SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) monthly [patches](https://github.com/dotnet/core/tree/main/release-notes/10.0#release-notes) (usually available between the 10th and 15th of each month). The development team will therefore also aim to roll out monthly Ocelot patches that reference the new .NET SDK patched versions. Ocelot patches will follow the form of a beta or patched version `major.minor.*`, where `*` is the Ocelot patch number corresponding to the newly available [.NET SDK patch](https://github.com/dotnet/core/blob/main/release-notes/10.0/README.md).

Ideally, the Ocelot team expects accelerated releases with more frequent minor/patch versions, because the [DevOps](https://github.com/ThreeMammals/Ocelot/labels/DevOps) is now prepared for the new release strategy and offers fast delivery.

For successful contributions, maintainers will announce an identity-verification procedure (details for first-time contributors will be published soon).

### :new: What's New?

- **[Quality of Service](https://github.com/ThreeMammals/Ocelot/blob/main/docs/features/qualityofservice.rst)**: A "[Built-in Circuit Breaker](https://github.com/ThreeMammals/Ocelot/issues/2384)" implementation was added by @ocelot-ot in pull request #2385, no [Polly](https://www.nuget.org/packages/Polly/) required.

  Ocelot's QoS schema no longer strictly depends on the external [Ocelot.QualityOfService.Polly](https://www.nuget.org/packages/Ocelot.QualityOfService.Polly/) package to provide circuit breaking and timeout enforcement.
  A lightweight, thread-safe circuit breaker (`Closed` &rarr; `Open` &rarr; `HalfOpen` &rarr; `Closed`) ships in the Ocelot core package, supporting both count-based and `FailureRatio`-based modes.
  The two implementations are mutually exclusive: the last of `AddQualityOfService()` or `AddPolly()` registered on the `OcelotBuilder` wins.
  See the built-in QoS documentation for full details, including the `AddQualityOfService<THandler>()` extensibility point for overriding server error codes.

- **[Websockets](https://github.com/ThreeMammals/Ocelot/blob/main/docs/features/websockets.rst)**: [Overridable WebSocket buffer size](https://github.com/ThreeMammals/Ocelot/issues/2386) and custom middleware injection was added by @erannevo in pull request #2387.

  The previously hard-coded `DefaultWebSocketBufferSize` is now a `protected virtual` property that can be overridden by subclassing.
  The [Middleware Injection](https://github.com/ThreeMammals/Ocelot/blob/main/docs/features/middlewareinjection.rst) feature was extended with a `WebSocketsProxyMiddleware` override on `OcelotPipelineConfiguration`, allowing a fully custom WebSocket middleware to be injected into the pipeline.

- **[Websockets](https://github.com/ThreeMammals/Ocelot/blob/main/docs/features/websockets.rst)**: [SecurityOptions support for the WebSocket pipeline](https://github.com/ThreeMammals/Ocelot/issues/2403) was added by @CurtisRobertOliver in pull request #2406.

  Previously, IP allow/block-list [Routing](https://github.com/ThreeMammals/Ocelot/blob/main/docs/features/routing.rst) `SecurityOptions` were not enforced for WebSocket upgrade requests, allowing them to bypass IP security.
  WebSocket upgrade requests are now intercepted by the same `IPSecurityPolicy` used for regular HTTP requests, and denied upgrades now correctly return `403 Forbidden`.

- **[Configuration](https://github.com/ThreeMammals/Ocelot/blob/main/docs/features/configuration.rst)**: "[Merge custom JSON properties across multiple configuration files](https://github.com/ThreeMammals/Ocelot/issues/651)" was implemented by @jlukawska in pull request #1183.

  Ocelot's configuration builder now merges custom (non-schema) JSON properties defined across multiple `ocelot.*.json` files using Newtonsoft's `JToken` merge functionality, instead of the last-loaded file silently overwriting properties from previous files.
  This also applies to the `DynamicRoutes` collection. See the updated [Configuration](https://github.com/ThreeMammals/Ocelot/blob/main/docs/features/configuration.rst) documentation and the `Configuration` sample app for details.

- **[Aggregation](https://github.com/ThreeMammals/Ocelot/blob/main/docs/features/aggregation.rst)**: "[Correct mapping of RouteKeysConfig arrays in aggregates](https://github.com/ThreeMammals/Ocelot/issues/2248)" fix, contributed in pull request #2328.

  Aggregate routes configured with the `RouteKeysConfig` array now correctly map route keys to the corresponding aggregator context, fixing a bug where keys could be mismatched or dropped during multiplexing.

### :up: What's Updated?

- Package extraction #2334: The Ocelot team, led by @raman-m, continued extracting integrated extension packages out of the mono-repo into their own dedicated repositories, each with an independent release cycle:

  * [Ocelot.Cache.CacheManager](https://www.nuget.org/packages/Ocelot.Cache.CacheManager/) &mdash; pull request #2360
  * [Ocelot.Tracing.Butterfly](https://www.nuget.org/packages/Ocelot.Tracing.Butterfly/) &mdash; pull request #2361
  * [Ocelot.Tracing.OpenTracing](https://www.nuget.org/packages/Ocelot.Tracing.OpenTracing/) &mdash; pull request #2362
  * [Ocelot.Discovery.Eureka](https://www.nuget.org/packages/Ocelot.Discovery.Eureka/) (renamed from `Ocelot.Provider.Eureka`, see issue #2371) &mdash; pull request #2372
  * [Ocelot.QualityOfService.Polly](https://www.nuget.org/packages/Ocelot.QualityOfService.Polly/) (renamed from `Ocelot.Provider.Polly`, see issue #2378) &mdash; pull request #2380
  * [Ocelot.Discovery.Consul](https://www.nuget.org/packages/Ocelot.Discovery.Consul/) (renamed from `Ocelot.Provider.Consul`, see issue #2378) &mdash; pull request #2388
  * [Ocelot.Discovery.KubeClient](https://www.nuget.org/packages/Ocelot.Discovery.KubeClient/) (renamed from `Ocelot.Provider.Kubernetes`, see issue #2378) &mdash; pull request #2404

  This keeps the Ocelot core repo lean, avoids delays caused by integrated packages' own release schedules, and lets each provider evolve independently.
  Consult the [Caching](https://github.com/ThreeMammals/Ocelot/blob/main/docs/features/caching.rst), [Tracing](https://github.com/ThreeMammals/Ocelot/blob/main/docs/features/tracing.rst), [Service Discovery](https://github.com/ThreeMammals/Ocelot/blob/main/docs/features/servicediscovery.rst) and [Kubernetes](https://github.com/ThreeMammals/Ocelot/blob/main/docs/features/kubernetes.rst) chapters for package-specific upgrade notes.

- **[Error Codes](https://github.com/ThreeMammals/Ocelot/blob/main/docs/features/errorcodes.rst)**: [Non-ASCII header exceptions mapped to 400 Bad Request](https://github.com/ThreeMammals/Ocelot/issues/2374) per RFC 7230, in pull request #2379.

  Requests containing non-ASCII characters in HTTP header values previously surfaced as an unhandled `HttpRequestException` and an HTTP 500 response.
  Ocelot now catches this `HttpRequestException` and maps it to a `400 Bad Request` response, in line with RFC 7230 "Field Parsing" rules.

- **[Administration](https://github.com/ThreeMammals/Ocelot/blob/main/docs/features/administration.rst)**: "[Hide Administration controllers from Swagger and OpenAPI](https://github.com/ThreeMammals/Ocelot/issues/989)" fix, contributed in pull request #2412.

  The `{{adminPath}}/configuration` and `{{adminPath}}/outputcache/{{region}}` endpoints are now decorated with `[ApiExplorerSettings(IgnoreApi = true)]` so that they no longer appear in Swagger/OpenAPI documentation generated for the downstream API surface.

- **[DevOps](https://github.com/ThreeMammals/Ocelot/labels/DevOps)**: Extensive testing and tooling modernization was carried out by @raman-m and contributors ahead of [.NET 10](https://github.com/ThreeMammals/Ocelot/milestone/13):

  * Solutions upgraded to the Visual Studio 2026 format.
  * Testing projects migrated from xUnit v2 to xUnit v3, and acceptance tests migrated to the Microsoft.Testing.Platform framework.
  * [Ocelot.Testing](https://github.com/ThreeMammals/Ocelot.Testing) was re-embedded into the mono-repo and later excluded from Cobertura coverage via `coverlet.runsettings`.
  * [Codecov](https://about.codecov.io/) coverage reporting was installed for the repository.
  * All NuGet packages were bumped to their latest versions to support .NET SDK `10.0.302` (.NET Runtime `10.0.10`).

### :package: Patches Included

- **[Routing](https://github.com/ThreeMammals/Ocelot/blob/main/docs/features/routing.rst)**: Issue #2346 patch, contributed in pull request #2351.

  This fixes a bug where downstream URL query parameter names could be corrupted if they contained a placeholder with a non-empty value.
  The `DownstreamUrlCreatorMiddleware` query string merging algorithm was refactored to take full advantage of ASP.NET Core's `QueryHelpers`.
