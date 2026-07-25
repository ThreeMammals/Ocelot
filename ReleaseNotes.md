<!--
{{0}} -> {0}
{{1}} -> {1}
Tag to substitute: {0}
https://www.nuget.org/packages/Ocelot/{0}
-->
## Pre-release 4 for [.NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) SDK (version [{0}](https://www.nuget.org/packages/Ocelot/{0}))
> Milestone: [.NET 10](https://github.com/ThreeMammals/Ocelot/milestone/13)
> Release Tag: [{0}](https://github.com/ThreeMammals/Ocelot/releases/tag/{0})
> Release Codename: [.NET 10](https://github.com/ThreeMammals/Ocelot/milestone/13)

In this major release, the Ocelot team focused on preparing the codebase and its ecosystem for [.NET 10](https://github.com/ThreeMammals/Ocelot/milestone/13), while also extracting several integrated extension packages from the mono-repo into their own dedicated repositories to speed up delivery and reduce release coupling.
This release also introduces a built-in *Quality of Service* (QoS) circuit breaker that works out of the box, without requiring the [Polly](https://www.nuget.org/packages/Polly/) library.

The updated documentation highlights [the deprecation](https://ocelot.readthedocs.io/en/latest/search.html?q=deprecated) of certain packages through multiple notes and warnings.
With the [Obsolete attributes](https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20%5BObsolete&type=code) in place, C# developers will notice several warnings in the build logs during compilation.

On top of that, this release brings a great enhancement to the [Kubernetes](https://github.com/ThreeMammals/Ocelot/blob/main/docs/features/kubernetes.rst) provider, also known as the [Ocelot.Discovery.KubeClient](https://www.nuget.org/packages/Ocelot.Discovery.KubeClient/) package.

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
