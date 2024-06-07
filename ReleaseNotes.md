## Spring 2024 (version {0}) aka [Twilight Texas](https://www.timeanddate.com/eclipse/solar/2024-april-8) release
> Codenamed: **[Twilight Texas](https://www.timeanddate.com/eclipse/solar/2024-april-8)**
> Read the Docs: [Ocelot 23.3](https://ocelot.readthedocs.io/en/{0}/)

### What's new?

- **[Service Discovery](https://github.com/ThreeMammals/Ocelot/blob/main/docs/features/servicediscovery.rst)**: Introducing a new feature for "[Customization of services creation](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/servicediscovery.rst#consul-service-builder-3)" in two primary service discovery providers: [Consul](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/servicediscovery.rst#consul-service-builder-3) and [Kubernetes](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/kubernetes.rst#downstream-scheme-vs-port-names-3), developed by @raman-m.
  The customization for both `Consul` and `Kube` providers in service creation is achieved through the overriding of virtual methods in default implementations. The recommendation was to separate the provider's logic and introduce `public virtual` and `protected virtual` methods in concrete classes, enabling:
  - The use of `public virtual` methods as dictated by interface definitions.
  - The application of `protected virtual` methods to allow developers to customize atomic operations through inheritance from existing concrete classes.
  - The injection of new interface objects into the provider's constructor.
  - The overriding of the default behavior of classes.

  Ultimately, customization relies on the virtual methods within the default implementation classes, providing developers the flexibility to override them as necessary for highly tailored Consul/K8s configurations in their specific environments.
  For further details, refer to the respective pull requests for both providers:
  - `Kube` &rarr; PR #2052
  - `Consul` &rarr; PR #2067

- **[Routing](https://github.com/ThreeMammals/Ocelot/blob/main/docs/features/routing.rst)**: Introducing the new "[Routing based on Request Header](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/routing.rst#upstream-headers-3)" feature by @jlukawska.
  In addition to routing via `UpstreamPathTemplate`, you can now define an `UpstreamHeaderTemplates` options dictionary. For a route to match, all headers specified in this section are required to be present in the request headers.
  For more details, see PR #1312.

- **[Configuration](https://github.com/ThreeMammals/Ocelot/blob/main/docs/features/configuration.rst)**: Introducing the "[Custom Default Version Policy](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/configuration.rst#downstreamhttpversionpolicy-3)" feature by @ibnuda.
  The configurable `HttpRequestMessage.VersionPolicy` helps avoid HTTP protocol connection errors and stabilizes connections to downstream services, especially when you're not developing those services, documentation is scarce, or the deployed HTTP protocol version is uncertain.
  For developers of downstream services, it's possible to `ConfigureKestrel` server and its endpoints with new protocol settings. However, attention to version policy is also required, and this feature provides precise version settings for HTTP connections.
  Essentially, this feature promotes the use of HTTP protocols beyond 1.0/1.1, such as HTTP/2 or even HTTP/3.
  For additional details, refer to PR #1673.

- **[Configuration](https://github.com/ThreeMammals/Ocelot/blob/main/docs/features/configuration.rst)**: Introducing the new "[Route Metadata](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/configuration.rst#route-metadata)" feature by @vantm. Undoubtedly, this is the standout feature of the release! :star:
  Route metadata enables Ocelot developers to incorporate custom functions that address specific needs or to create their own plugins/extensions.
  In versions of Ocelot prior to [{0}](https://github.com/ThreeMammals/Ocelot/releases/tag/{0}), the configuration was limited to predefined values that Ocelot used internally. This was sufficient for official extensions, but posed challenges for third-party developers who needed to implement configurations not included in the standard `FileConfiguration`. Applying an option to a specific route required knowledge of the array index and other details that might not be readily accessible using the standard `IConfiguration` or `IOptions<FileConfiguration>` models from ASP.NET. Now, metadata can be directly accessed in the `DownstreamRoute` object. Furthermore, metadata can also be retrieved from the global JSON section via the `FileConfiguration.GlobalConfiguration` property.
  For more information, see the details in PR #1843 on this remarkable feature.

### Focus On

<details>
  <summary><b>Updates of the features</b>: Configuration, Service Discovery, Routing and Quality of Service</summary>

  - [Configuration](https://github.com/ThreeMammals/Ocelot/blob/main/docs/features/configuration.rst): New features are "[Custom Default Version Policy](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/configuration.rst#downstreamhttpversionpolicy-3)" by @ibnuda and "[Route Metadata](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/configuration.rst#route-metadata)" by @vantm.

  - [Service Discovery](https://github.com/ThreeMammals/Ocelot/blob/main/docs/features/servicediscovery.rst): New feature is "[Customization of services creation](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/servicediscovery.rst#consul-service-builder-3)" by @raman-m.

  - [Routing](https://github.com/ThreeMammals/Ocelot/blob/main/docs/features/routing.rst): New feature is "[Routing based on Request Header](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/routing.rst#upstream-headers-3)" by @jlukawska.

  - [Quality of Service](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/qualityofservice.rst): The team has decided to remove the Polly V7 policies logic and the corresponding Ocelot `AddPollyV7` extensions (referenced in PR #2079). Furthermore, the Polly V8 Circuit Breaker has been mandated as the primary strategy (as per PR #2086).
    See more detaild below in "**Ocelot extra packages**" paragraph.
</details>

<details>
  <summary><b>Ocelot extra packages</b></summary>

  - **[Ocelot.Provider.Polly](https://www.nuget.org/packages/Ocelot.Provider.Polly)**

    - Our team has resolved to eliminate the Polly V7 policies logic and the corresponding Ocelot `AddPollyV7` extensions entirely (refer to the "[Polly v7 vs v8](https://github.com/ThreeMammals/Ocelot/blob/23.2.0/docs/features/qualityofservice.rst#polly-v7-vs-v8)" documentation). In the previous [23.2](https://github.com/ThreeMammals/Ocelot/releases/tag/23.2.0) release, named [Lunar Eclipse](https://github.com/ThreeMammals/Ocelot/releases/tag/23.2.0), we included these to maintain the legacy Polly behavior, allowing development teams to transition or retain the old Polly V7 functionality. We are now confident that it is time to progress alongside Polly, shifting our focus to the new Polly V8 [resilience pipelines](https://www.pollydocs.org/pipelines/). For more details, see PR #2079.

    - Additionally, we have implemented Polly v8 Circuit Breaker as the primary strategy. Our Quality of Service (QoS) relies on two main strategies: [Circuit Breaker](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/qualityofservice.rst#circuit-breaker-strategy) and [Timeout](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/qualityofservice.rst#timeout-strategy). If both Circuit Breaker and Timeout are [configured](https://github.com/ThreeMammals/Ocelot/blob/{0}/docs/features/qualityofservice.rst#configuration) with their respective properties in the `QoSOptions` of the route JSON, then the Circuit Breaker strategy will take precedence in the constructed resilience pipeline. For more details, refer to PR #2086.
</details>

<details>
  <summary><b>Stabilization</b> aka bug fixing</summary>

  - Fixed #2034 in PR #2045 by @raman-m
  - Fixed #2039 in PR #2050 by @PaulARoy
  - Fixed #1590 in PR #1592 by @sergio-str
  - Fixed #2054 #2059 in PR #2058 by @thiagoloureiro
  - Fixed #954 #957 #1026 in PR #2067 by @raman-m
  - Fixed #2002 in PR #2003 by @bbenameur
  - Fixed #2085 in PR #2086 by @RaynaldM
  - See [all bugs](https://github.com/ThreeMammals/Ocelot/issues?q=is%3Aissue+milestone%3ASpring%2724+is%3Aclosed+label%3Abug) of the [Spring'24](https://github.com/ThreeMammals/Ocelot/milestone/6) milestone
</details>

<details>
  <summary><b>Documentation</b> for version <a href="https://ocelot.readthedocs.io/en/{0}/">{0}</a></summary>

  - [Caching](https://ocelot.readthedocs.io/en/{0}/features/caching.html): New [EnableContentHashing option](https://ocelot.readthedocs.io/en/{0}/features/caching.html#enablecontenthashing-option) and [Global Configuration](https://ocelot.readthedocs.io/en/{0}/features/caching.html#global-configuration) sections
  - [Configuration](https://ocelot.readthedocs.io/en/{0}/features/configuration.html): New [DownstreamHttpVersionPolicy](https://ocelot.readthedocs.io/en/{0}/features/configuration.html#downstreamhttpversionpolicy-3) and [Route Metadata](https://ocelot.readthedocs.io/en/{0}/features/configuration.html#route-metadata)
  - [Kubernetes](https://ocelot.readthedocs.io/en/{0}/features/kubernetes.html): New [Downstream Scheme vs Port Names](https://ocelot.readthedocs.io/en/{0}/features/kubernetes.html#downstream-scheme-vs-port-names-3) section
  - [Metadata](https://ocelot.readthedocs.io/en/{0}/features/metadata.html): This is new chapter for [Route Metadata](https://ocelot.readthedocs.io/en/{0}/features/configuration.html#route-metadata) feature.
  - [Quality of Service](https://ocelot.readthedocs.io/en/{0}/features/qualityofservice.html)
  - [Rate Limiting](https://ocelot.readthedocs.io/en/{0}/features/ratelimiting.html)
  - [Request Aggregation](https://ocelot.readthedocs.io/en/{0}/features/requestaggregation.html)
  - [Routing](https://ocelot.readthedocs.io/en/{0}/features/routing.html): New [Upstream Headers](https://ocelot.readthedocs.io/en/{0}/features/routing.html#upstream-headers-3) section
  - [Service Discovery](https://ocelot.readthedocs.io/en/{0}/features/servicediscovery.html): New [Consul Service Builder](https://ocelot.readthedocs.io/en/{0}/features/servicediscovery.html#consul-service-builder-3) section
</details>
