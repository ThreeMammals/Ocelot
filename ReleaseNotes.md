<!--
{{0}} -> {0}
{{1}} -> {1}
Tag to substitute: {0}
https://www.nuget.org/packages/Ocelot/{0}
-->
## Upgrade to [.NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) SDK (TFM `net10.0`, version [25.0](https://www.nuget.org/packages/Ocelot/{0})) a.k.a. the [.NET 10](https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/) release
> **Milestone: [.NET 10](https://github.com/ThreeMammals/Ocelot/milestone/13)** :point_left:
> Codenamed: [.NET 10](https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/)
> Read the Docs: [Ocelot 25.0](https://ocelot.readthedocs.io/en/{0}/) with [PDF](https://ocelot.readthedocs.io/_/downloads/en/{0}/pdf/)
> Target Framework Monikers: `net8.0`, `net9.0`, `net10.0`

### :information_source: About
On November 11th, 2025, the [.NET team](https://devblogs.microsoft.com/dotnet/author/dotnet) announced the release of the [.NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) framework:

* .NET Blog: [Announcing .NET 10](https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/)

This major release upgrades the [Ocelot](https://www.nuget.org/packages/Ocelot#supportedframeworks-body-tab) package [TFMs](https://learn.microsoft.com/en-us/dotnet/standard/frameworks#latest-versions) to **`net10.0`** in addition to the current `net8.0` and `net9.0`. Thus, the current Ocelot [supported frameworks](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core#lifecycle) are .NET 8 LTS, .NET 9 STS, and **.NET 10** LTS.

Additionally, in this major release the Ocelot team focused on preparing the codebase and its ecosystem for the **.NET 10** SDK ([.NET 10](https://github.com/ThreeMammals/Ocelot/milestone/13) milestone), while also extracting several [integrated extension packages](https://www.nuget.org/profiles/ThreeMammals) from [the v24.1 monorepo](https://github.com/ThreeMammals/Ocelot/tree/24.1.0/src) into their own [dedicated repositories](https://github.com/orgs/ThreeMammals/repositories?q=Ocelot.) to speed up delivery and reduce release coupling. As a result of these [DevOps](https://github.com/ThreeMammals/Ocelot/labels/DevOps) activities, the Ocelot repository now hosts only two package projects — [Ocelot](https://github.com/ThreeMammals/Ocelot/tree/{0}/src) (`src` folder) and [Ocelot.Testing](https://github.com/ThreeMammals/Ocelot/tree/{0}/testing) (`testing` folder) — along with two solutions, `Ocelot.slnx` and `Ocelot.Samples.slnx`, which can be opened or built only if the **.NET 10** SDK is installed. Since the extension packages now live in separate repositories, a new [Release Radar](https://github.com/ThreeMammals/Ocelot#satellite-release-radar) status page has been introduced to track their progress.

This major version also includes the following feature updates:
- A built-in [Quality of Service](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/qualityofservice.rst) (QoS) circuit breaker (express edition) that works out of the box without requiring the [Polly](https://www.nuget.org/packages/Polly/) library.
- Significant enhancements to the [WebSockets](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/websockets.rst) feature, including [WebSockets middleware](https://github.com/ThreeMammals/Ocelot/blob/{0}/src/WebSockets/WebSocketsProxyMiddleware.cs) overriding and the addition of [Security Options](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/routing.rst#security-options-6) to the WebSockets pipeline.
- Improved [Configuration](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/configuration.rst) experience that merges multiple `ocelot.*.json` files, keeping users' [extended properties](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/configuration.rst#extend-with-custom-properties-6) available during Ocelot app startup and at runtime via the standard `IConfiguration` service in the DI container.

Finally, because the repository has a [new folder structure](https://github.com/ThreeMammals/Ocelot#file_folder-repository-structure), the development team recommends recloning or even reforking the Ocelot repository for a successful upgrade to the new version in order to avoid potential build errors.

As a best practice, the release strategy has been aligned with [.NET SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) monthly [patches](https://github.com/dotnet/core/tree/main/release-notes/10.0#release-notes) (usually available between the 10th and 15th of each month). The development team will therefore also aim to roll out monthly Ocelot patches that reference the new .NET SDK patched versions. Ocelot patches will follow the form of a beta or patched version `major.minor.*`, where `*` is the Ocelot patch number corresponding to the newly available [.NET SDK patch](https://github.com/dotnet/core/blob/main/release-notes/10.0/README.md).

Ideally, the Ocelot team expects accelerated releases with more frequent minor/patch versions, because [DevOps](https://github.com/ThreeMammals/Ocelot/labels/DevOps) is now prepared for the new release strategy and offers fast delivery.

For successful contributions, maintainers will announce an identity-verification procedure (details for first-time contributors will be published soon).

### :new: What's New?

- :star: **[Quality of Service](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/qualityofservice.rst)**: A "[Built-in Circuit Breaker](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/qualityofservice.rst#built-in-qos)" feature [implementation](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/qualityofservice.rst#implementations-overview) was added by @ocelot-ot in pull request #2385 — no [Polly](https://www.nuget.org/packages/Polly/) required. :star:

  Ocelot's QoS schema no longer strictly depends on the external [Ocelot.QualityOfService.Polly](https://www.nuget.org/packages/Ocelot.QualityOfService.Polly/) package to provide circuit breaking and timeout enforcement.
  A lightweight, thread-safe circuit breaker (`Closed` &rarr; `Open` &rarr; `HalfOpen` &rarr; `Closed`) ships in the Ocelot core package, supporting both count-based and `FailureRatio`-based modes.
  The [two implementations](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/qualityofservice.rst#implementations-overview) are mutually exclusive: the last of `AddQualityOfService()` or `AddPolly()` registered on the `OcelotBuilder` wins.
  See the [built-in QoS](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/qualityofservice.rst#built-in-qos) documentation for full details, including the `AddQualityOfService<THandler>()` extensibility point for overriding server error codes.

- **[WebSockets](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/websockets.rst)**: The "[Overridable WebSocket buffer size](https://github.com/ThreeMammals/Ocelot/issues/2386)" feature and custom middleware injection were added by @erannevo in pull request #2387.

  The previously hard-coded `DefaultWebSocketBufferSize` is now a `protected virtual` property that can be overridden by subclassing.
  The [Middleware Injection](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/middlewareinjection.rst) feature was extended with a `WebSocketsProxyMiddleware` override on the [OcelotPipelineConfiguration class](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/middlewareinjection.rst#ocelotpipelineconfiguration-class), allowing a fully custom WebSocket middleware to be injected into the pipeline.
  Refer to the "[Sample](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/websockets.rst#sample-6)" section to understand how to utilize the new feature.

- **[WebSockets](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/websockets.rst)**: The "[SecurityOptions support for the WebSocket pipeline](https://github.com/ThreeMammals/Ocelot/issues/2403)" feature was added by @CurtisRobertOliver in pull request #2406.

  Previously, IP allow/block-list [Routing](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/routing.rst) "[Security Options](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/routing.rst#security-options-6)" were not enforced for WebSocket upgrade requests (a.k.a. the `CONNECT` HTTP method), allowing them to bypass IP security.
  WebSocket upgrade requests are now intercepted by the same `IPSecurityPolicy` used for regular HTTP requests, and denied upgrades now correctly return `403 Forbidden`.

- :star: **[Configuration](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/configuration.rst)**: The "[Extend configuration with Custom Properties](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/configuration.rst#extend-with-custom-properties)" feature was implemented by @jlukawska in pull request #1183. :star:

  Ocelot's configuration builder now merges custom (non-schema) JSON properties defined across multiple `ocelot.*.json` files using Newtonsoft's `JToken` merge functionality, instead of the last-loaded file silently overwriting properties from previous files in the `Routes` collection.
  This also applies to the `DynamicRoutes` collection and the `GlobalConfiguration` section. See the updated [Configuration](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/configuration.rst) documentation, especially the "[Extend configuration with Custom Properties](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/configuration.rst#extend-with-custom-properties)" section, and the [Configuration Sample](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/configuration.rst#sample) app for details.

- :airplane: **[Aggregation](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/aggregation.rst)**: A new "[Manual/custom aggregators](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/aggregation.rst#aggregate-manually) for [Complex Aggregation](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/aggregation.rst#complex-aggregation)" feature has been published (originally developed by @TomPallister in pull requests #248, #310). :airplane:

  While working on bug #2248 in pull request #2328 by @NandanDevHub, the development team uncovered an undocumented feature in the [Multiplexer](https://github.com/ThreeMammals/Ocelot/tree/{0}/src/Multiplexer) namespace and decided to publish this pilot feature in the "[Aggregate Manually?](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/aggregation.rst#aggregate-manually)" documentation. The feature is based on the `IResponseAggregator` interface, whose correct application is currently unclear. The Ocelot team will test it further in upcoming releases, develop best practices, create a sample app, and inform the community. For this reason we warn that the feature is still in pilot status. :airplane:

### :up: What's Updated?

- **[Extension packages](https://www.nuget.org/profiles/ThreeMammals)** extraction #2334: The Ocelot team continued extracting [integrated extension packages](https://www.nuget.org/profiles/ThreeMammals) out of [the monorepo](https://github.com/ThreeMammals/Ocelot/tree/24.1.0/src) into their own [dedicated repositories](https://github.com/orgs/ThreeMammals/repositories?q=Ocelot.), each with an independent release cycle:

  * [Ocelot.Cache.CacheManager](https://github.com/ThreeMammals/Ocelot.Cache.CacheManager/) in pull request #2360
  * [Ocelot.Tracing.Butterfly](https://github.com/ThreeMammals/Ocelot.Tracing.Butterfly/) in pull request #2361
  * [Ocelot.Tracing.OpenTracing](https://github.com/ThreeMammals/Ocelot.Tracing.OpenTracing/) in pull request #2362
  * [Ocelot.Discovery.Eureka](https://github.com/ThreeMammals/Ocelot.Discovery.Eureka/) (renamed from `Ocelot.Provider.Eureka`, see issue #2371) in pull request #2372
  * [Ocelot.QualityOfService.Polly](https://github.com/ThreeMammals/Ocelot.QualityOfService.Polly/) (renamed from `Ocelot.Provider.Polly`, see issue #2378) in pull request #2380
  * [Ocelot.Discovery.Consul](https://github.com/ThreeMammals/Ocelot.Discovery.Consul/) (renamed from `Ocelot.Provider.Consul`, see issue #2378) in pull request #2388
  * [Ocelot.Discovery.KubeClient](https://github.com/ThreeMammals/Ocelot.Discovery.KubeClient/) (renamed from `Ocelot.Provider.Kubernetes`, see issue #2378) in pull request #2404

  This keeps the Ocelot core repository lean, avoids delays caused by the integrated packages' own release schedules, and lets each provider evolve independently.
  Consult the [Caching](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/caching.rst), [Tracing](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/tracing.rst), [Service Discovery](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/servicediscovery.rst), and [Kubernetes](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/kubernetes.rst) chapters for package-specific upgrade notes.
  Please note that some of the packages are deprecated! To watch the status of each package and its repository, go to the [Release Radar](https://github.com/ThreeMammals/Ocelot/blob/{0}/ReleaseRadar.md) status page.

- **[Kubernetes](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/kubernetes.rst)**: The "[PollKube provider](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/kubernetes.rst#pollkube-provider-3)" was redesigned by @raman-m in pull request #2358.

  The `PollKube` discovery provider was redesigned to utilize `PeriodicTimer` ([introduced](https://github.com/dotnet/runtime/pull/53899) in .NET 6). The provider is based on an "active polling" strategy that requires stable behavior and careful management of timing events in multi-threaded scenarios. The new `PeriodicTimer` is designed for thread safety, and its callbacks replace the old `Timer` callbacks, which work fine in a synchronous flow.
  Please note that the `PollKube` discovery provider is now part of the [Ocelot.Discovery.KubeClient](https://github.com/ThreeMammals/Ocelot.Discovery.KubeClient) package.

- **[DevOps](https://github.com/ThreeMammals/Ocelot/labels/DevOps)**: Extensive testing and tooling modernization was carried out by contributors ahead of [.NET 10](https://github.com/ThreeMammals/Ocelot/milestone/13):

  * Solutions upgraded to the [Visual Studio 2026 Solution File Format](https://devblogs.microsoft.com/visualstudio/new-simpler-solution-file-format/) a.k.a. [.NET 10 CLI solution file format](https://devblogs.microsoft.com/dotnet/introducing-slnx-support-dotnet-cli/) (`.sln` &rarr; `.slnx`), by @raman-m in pull request #2354.
  * Testing projects migrated from xUnit v2 to xUnit v3 (by @methran1304 in #2356), and acceptance tests migrated to the `Microsoft.Testing.Platform` framework (by @raman-m in #2392).
  * [Ocelot.Testing](https://github.com/ThreeMammals/Ocelot.Testing) was re-embedded into the monorepo (by @ocelot-ot in #2364) and later excluded from Cobertura coverage via `coverlet.runsettings` (by @ocelot-ot in #2365).
  * [Codecov](https://about.codecov.io/) coverage reporting was installed for the repository, by @raman-m in pull request #2359.
  * NuGet packages were continuously bumped to their latest versions to support .NET SDK `10.0.*` (.NET Runtime `10.0.*`), by @raman-m and @ocelot-ot in pull requests #2367, #2389, #2402, and #2409.

  The updated documentation also highlights [the deprecation](https://github.com/search?q=repo%3AThreeMammals%2FOcelot+deprecated+language%3AreStructuredText&type=code&l=reStructuredText) of certain packages through multiple notes and warnings.
  With the [Obsolete attributes](https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20%5BObsolete&type=code) in place, C# developers will notice several warnings in the build logs during compilation.

### :package: Patches

- **[Routing](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/routing.rst)**: Issue #2346 was patched by @bhargav-polara in pull request #2351.

  This fixes a bug where downstream URL query parameter names could be corrupted if they contained a placeholder with a non-empty value.
  The `DownstreamUrlCreatorMiddleware` query-string merging algorithm was refactored to take full advantage of ASP.NET Core's `QueryHelpers`.

- **[Error Handling](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/errorcodes.rst)**: Issue #2376 was patched by @Majdi-Zlitni in pull request #2379.

  Requests containing non-ASCII characters in HTTP header values previously surfaced as an unhandled `HttpRequestException` and an HTTP `502 Bad Gateway` response.
  Ocelot now catches this `HttpRequestException` and maps it to a `400 Bad Request` response, in line with [RFC 7230](https://www.rfc-editor.org/rfc/rfc7230) "[Field Parsing](https://www.rfc-editor.org/info/rfc7230/#section-3.2.4)" rules.

- :fire: **[WebSockets](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/websockets.rst)**: Critical issue #2403 was patched by @CurtisRobertOliver in pull request #2406. :fire:

  Previously, IP allow/block-list [Routing](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/routing.rst) "[Security Options](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/routing.rst#security-options-6)" were not enforced for WebSocket upgrade requests, allowing them to bypass IP security.
  WebSocket upgrade requests are now intercepted by the same `IPSecurityPolicy` used for regular HTTP requests, and denied upgrades now correctly return `403 Forbidden`.

  This critical security issue was [detected by security scanners on GitHub](https://github.com/ThreeMammals/Ocelot/issues/2403#event-7907427333) after the bug was reported. :see_no_evil:
  Many thanks to **George Chen** (@geo-chen) for reporting this critical bug.

- **[Aggregation](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/aggregation.rst)**: Issue #2248 was patched by @NandanDevHub in pull request #2328.

  [Complex Aggregation](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/aggregation.rst#complex-aggregation) routes configured with the `RouteKeysConfig` array now correctly map route keys to the corresponding aggregator context, fixing a bug where keys could be mismatched or dropped during multiplexing.

- **[Administration](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/administration.rst)**: Issue #989 was patched by @mmustafasenoglu in pull request #2412.

  The `{{adminPath}}/configuration` and `{{adminPath}}/outputcache/{{region}}` endpoints are now decorated with `[ApiExplorerSettings(IgnoreApi = true)]` so that they no longer appear in Swagger/OpenAPI documentation generated for the downstream API surface.
