## November-December 2023 (version {0}) aka [Swiss??? Quality](https://www.google.com/search?q=swiss+quality) release
> Codenamed as **[?Made in Switzerland?](https://www.bing.com/search?q=Made+in+Switzerland)**

### Focused On

<details>
  <summary><b>System performance</b>. System core performance review, redesign of system core related to routing and content streaming</summary>

  - Modification of the `RequestMapper` with a brand new `StreamHttpContent` class, in `Ocelot.Request.Mapper` namespace. The request body is no longer copied when it is handled by the API gateway avoiding Out-of-Memory incidents of pods/containers. This significantly reduces the gateway's memory consumption, and allows you to transfer content larger than 2 GB in streaming scenarios.
  - Introduction of a new Message Invoker pool, in `Ocelot.Requester` namespace. We have replaced [HttpClient](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient) class with [HttpMessageInvoker](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpmessageinvoker), which is the base class for `HttpClient`. The overall logic for managing the pool has been simplified, resulting in a reduction in the number of CPU cycles.
  - Full HTTP content buffering is deactivated, resulting in a 50% reduction in memory consumption and a performance improvement of around 10%. Content is no longer copied on the API gateway, avoiding Out-of-Memory issues.
  - **TODO** Include screenshots from Production...
</details>

<details>
  <summary><b>Ocelot extra packages</b>. Total 3 Ocelot packs were updated</summary>
 
  - [Ocelot.Cache.CacheManager](https://github.com/ThreeMammals/Ocelot/tree/main/src/Ocelot.Cache.CacheManager): Introduced default cache key generator with improved performance (the `DefaultCacheKeyGenerator` class). Old version of `CacheKeyGenerator` had significant performance issue when reading full content of HTTP request for caching key calculation of MD5 hash value. This hash value was excluded from the caching key.
  - [Ocelot.Provider.Kubernetes](https://github.com/ThreeMammals/Ocelot/tree/main/src/Ocelot.Provider.Kubernetes): Fixed long lasting breaking change being added in [15.0.0](https://github.com/ThreeMammals/Ocelot/releases/tag/15.0.0) version, see commit [6e5471a714dddb0a3a40fbb97eac2810cee1c78d](https://github.com/ThreeMammals/Ocelot/commit/6e5471a714dddb0a3a40fbb97eac2810cee1c78d). The bug persisted for more than 3 years in **15.0.0-22.0.1** versions masking multiple times via class renaming! Special thanks to @ZisisTsatsas who once again brought this issue to our attention and our team finally realized that we had a breaking change and the provider was broken.
  - [Ocelot.Provider.Polly](https://github.com/ThreeMammals/Ocelot/tree/main/src/Ocelot.Provider.Polly): A minor changes without feature delivery. We are preparing for a major update to the package in the next release.
</details>

<details>
  <summary><b>Middlewares</b>. Total 8 Ocelot middlewares were updated</summary>
 
  - `AuthenticationMiddleware`: Added new [Multiple Authentication Schemes](https://github.com/ThreeMammals/Ocelot/pull/1870) feature by @MayorSheFF 
  - `OutputCacheMiddleware`, `RequestIdMiddleware`: Added new [Cache by Header Value](https://github.com/ThreeMammals/Ocelot/pull/1172) by @EngRajabi, and redesigned as [Default CacheKeyGenerator] feature by @raman-m
  - `DownstreamUrlCreatorMiddleware`: Fixed [bug](https://github.com/ThreeMammals/Ocelot/issues/748) for ending/omitting slash in path templates aka [Empty placeholders](https://github.com/ThreeMammals/Ocelot/pull/1911) feature by @AlyHKafoury 
  - `ConfigurationMiddleware`, `HttpRequesterMiddleware`, `ResponderMiddleware`: System upgrade for [Custom HttpMessageInvoker pooling](https://github.com/ThreeMammals/Ocelot/pull/1824) feature by @ggnaegi
  - `DownstreamRequestInitialiserMiddleware`: System upgrade for [Performance of Request Mapper](https://github.com/ThreeMammals/Ocelot/pull/1724) feature by @ggnaegi
</details>

<details>
  <summary>Documentation for <b>Authentication</b>, <b>Caching</b>, <b>Kubernetes</b> and <b>Routing</b></summary>
 
  - [Authentication](https://ocelot.readthedocs.io/en/latest/features/authentication.html)
  - [Caching](https://ocelot.readthedocs.io/en/latest/features/caching.html)
  - [Kubernetes](https://ocelot.readthedocs.io/en/latest/features/kubernetes.html)
  - [Routing](https://ocelot.readthedocs.io/en/latest/features/routing.html)
</details>

<details>
  <summary><b>Stabilization</b> aka bug fixing</summary>

  - See [all bugs](https://github.com/ThreeMammals/Ocelot/issues?q=is%3Aissue+milestone%3ANov-December%2723+is%3Aclosed+label%3Abug) of the [Nov-December'23](https://github.com/ThreeMammals/Ocelot/milestone/2) milestone
</details>

<details>
  <summary><b>Testing</b></summary>

  - The `Ocelot.Benchmarks` testing project has been updated with new `PayloadBenchmarks` and `ResponseBenchmarks` by @ggnaegi
  - The `Ocelot.AcceptanceTests` testing project has been refactored by @raman-m using the new `AuthenticationSteps` class, and more refactoring will be done in future releases
</details>
