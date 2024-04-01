## February 2024 (version {0}) aka [Lunar Eclipse](https://www.timeanddate.com/eclipse/lunar/2024-march-25) release
> Codenamed: **[Lunar Eclipse](https://www.bing.com/search?q=Lunar+Eclipse+2024)**
> Read the Docs: [Ocelot 23.2](https://ocelot.readthedocs.io/en/23.2.0/)

### What's new?

- **[Configuration](https://ocelot.readthedocs.io/en/latest/features/configuration.html)**: A brand new [Merging files to memory](https://ocelot.readthedocs.io/en/23.2.0/features/configuration.html#merging-files-to-memory) by @ebjornset as a part of the [Merging Configuration Files](https://ocelot.readthedocs.io/en/23.2.0/features/configuration.html#merging-configuration-files) feature.
  The `AddOcelot` method merges the **ocelot.*.json** files into a single **ocelot.json** file as the primary configuration file, which is written back to disk and then added to the `IConfigurationBuilder` for the well-known `IConfiguration`. You can now call another `AddOcelot` method that adds the merged JSON directly from memory to the `IConfigurationBuilder`, using `AddJsonStream` instead.
  See more details in [Configuration Overview](https://ocelot.readthedocs.io/en/23.2.0/features/dependencyinjection.html#configuration-overview) of [Dependency Injection](https://ocelot.readthedocs.io/en/23.2.0/features/dependencyinjection.html).
- **[Service Fabric](https://ocelot.readthedocs.io/en/latest/features/servicefabric.html)**: Published old undocumented "[Placeholders in Service Name](https://ocelot.readthedocs.io/en/23.2.0/features/servicefabric.html#placeholders-in-service-name)" feature of [Service Fabric](https://ocelot.readthedocs.io/en/23.2.0/features/servicefabric.html) [service discovery provider](https://ocelot.readthedocs.io/en/23.2.0/search.html?q=ServiceDiscoveryProvider).
  This feature by @FelixBoers is available starting from version [13.0.0](https://github.com/ThreeMammals/Ocelot/releases/tag/13.0.0).
- **[Quality of Service](https://ocelot.readthedocs.io/en/latest/features/qualityofservice.html)**: A brand new [Polly](https://github.com/App-vNext/Polly) v8 pipelines [Extensibility](https://ocelot.readthedocs.io/en/23.2.0/features/qualityofservice.html#extensibility) feature by @RaynaldM

### Focus On

<details>
  <summary><b>Updates of the features</b>: Configuration, Dependency Injection and QoS</summary>
 
  - [Configuration](https://ocelot.readthedocs.io/en/latest/features/configuration.html): New [Merging files to memory](https://ocelot.readthedocs.io/en/23.2.0/features/configuration.html#merging-files-to-memory) feature by @ebjornset
  - [Dependency Injection](https://ocelot.readthedocs.io/en/latest/features/dependencyinjection.html): Added new overloaded [AddOcelot methods](https://ocelot.readthedocs.io/en/23.2.0/features/dependencyinjection.html#addocelot-method) by @ebjornset
  - [Quality of Service](https://ocelot.readthedocs.io/en/latest/features/qualityofservice.html): Support of new [Polly](https://github.com/App-vNext/Polly) v8 syntax and new [Extensibility](https://ocelot.readthedocs.io/en/23.2.0/features/qualityofservice.html#extensibility) feature by @RaynaldM
</details>

<details>
  <summary><b>Ocelot extra packages</b></summary>

  - [Ocelot.Provider.Polly](https://www.nuget.org/packages/Ocelot.Provider.Polly): Support of new [Polly](https://github.com/App-vNext/Polly) v8 syntax.
    *Polly* [8.0+](https://github.com/App-vNext/Polly/releases) versions introduced the concept of [resilience pipelines](https://www.pollydocs.org/pipelines/).
    All [AddPolly extensions](https://github.com/ThreeMammals/Ocelot/blob/develop/src/Ocelot.Provider.Polly/OcelotBuilderExtensions.cs) have been automatically migrated from **v7** to **v8**.
    Please note that older **v7** extensions are marked with the `[Obsolete]` attribute and renamed using the `V7` suffix. And the old **v7** implementation has been moved to the [v7 namespace](https://github.com/ThreeMammals/Ocelot/tree/develop/src/Ocelot.Provider.Polly/v7).
    See more details in [Polly v7 vs v8](https://ocelot.readthedocs.io/en/23.2.0/features/qualityofservice.html#polly-v7-vs-v8) section of [Quality of Service](https://ocelot.readthedocs.io/en/23.2.0/features/qualityofservice.html) chapter.
</details>

<details>
  <summary><b>Stabilization</b> aka bug fixing</summary>

  - [683](https://github.com/ThreeMammals/Ocelot/issues/683) by PR [1927](https://github.com/ThreeMammals/Ocelot/pull/1927)
    [New rules](https://github.com/search?q=repo%3AThreeMammals%2FOcelot+IsPlaceholderNotDuplicatedIn+IsUpstreamPlaceholderDefinedInDownstream+IsDownstreamPlaceholderDefinedInUpstream&type=code) have been added to Ocelot's configuration validation logic to find duplicate placeholders in path templates.
    See more in the [FileConfigurationFluentValidator](https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20FileConfigurationFluentValidator&type=code) class. Thanks to @AlyHKafoury!
  - [1518](https://github.com/ThreeMammals/Ocelot/issues/1518) hotfix by PR [1986](https://github.com/ThreeMammals/Ocelot/pull/1986)
    Using the default `IServiceCollection` [DI extensions](https://github.com/ThreeMammals/Ocelot/blob/develop/src/Ocelot/DependencyInjection/ServiceCollectionExtensions.cs) to register Ocelot services resulted in the `ServiceCollection` provider being forced to be created by calling `BuildServiceProvider()`.
    This resulted in problems with dependency injection libraries, or worse, causing the Ocelot app to crash!
    See more in the [ServiceCollectionExtensions](https://github.com/search?q=repo%3AThreeMammals%2FOcelot+ServiceCollectionExtensions&type=code) class. Thanks to @ArwynFr!
  - See [all bugs](https://github.com/ThreeMammals/Ocelot/issues?q=is%3Aissue+is%3Aclosed+label%3Abug+milestone%3AFebruary%2724) of the [February'24](https://github.com/ThreeMammals/Ocelot/milestone/5) milestone
</details>

<details>
  <summary><b>Documentation</b> for version <a href="https://ocelot.readthedocs.io/en/23.2.0/">23.2</a></summary>

  - [Configuration](https://ocelot.readthedocs.io/en/23.2.0/features/configuration.html)
  - [Dependency Injection](https://ocelot.readthedocs.io/en/23.2.0/features/dependencyinjection.html)
  - [Quality of Service](https://ocelot.readthedocs.io/en/23.2.0/features/qualityofservice.html)
  - [Service Fabric](https://ocelot.readthedocs.io/en/23.2.0/features/servicefabric.html)
</details>
