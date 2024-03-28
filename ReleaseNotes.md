## February 2024 (version {0}) aka [February'24](https://github.com/ThreeMammals/Ocelot/milestone/5) release
> Codenamed as **[February'24](https://github.com/ThreeMammals/Ocelot/milestone/5)**
> Read the Docs: [Ocelot 23.2](https://ocelot.readthedocs.io/en/23.2.0/)

### Focus On

<details>
  <summary><b>New features of</b>: Service Fabric and ...</summary>
 
- **[Service Fabric](https://ocelot.readthedocs.io/en/latest/features/servicefabric.html)**: Published old undocumented "[Placeholders in Service Name](https://ocelot.readthedocs.io/en/23.2.0/features/servicefabric.html#placeholders-in-service-name)" feature of [Service Fabric](https://ocelot.readthedocs.io/en/23.2.0/features/servicefabric.html) [service discovery provider](https://ocelot.readthedocs.io/en/23.2.0/search.html?q=ServiceDiscoveryProvider). This feature is available starting from version [13.0.0](https://github.com/ThreeMammals/Ocelot/releases/tag/13.0.0).
</details>

<details>
  <summary><b>Stabilization</b> aka bug fixing</summary>

  - [683](https://github.com/ThreeMammals/Ocelot/issues/683) by PR [1927](https://github.com/ThreeMammals/Ocelot/pull/1927)
    Ocelot configuration validation logic has updated with [new rules](https://github.com/search?q=repo%3AThreeMammals%2FOcelot+IsPlaceholderNotDuplicatedIn+IsUpstreamPlaceholderDefinedInDownstream+IsDownstreamPlaceholderDefinedInUpstream&type=code) to search for placeholder duplicates in path templates.
    See more in the [FileConfigurationFluentValidator](https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20FileConfigurationFluentValidator&type=code) class.
  - See [all bugs](https://github.com/ThreeMammals/Ocelot/issues?q=is%3Aissue+is%3Aclosed+label%3Abug+milestone%3AFebruary%2724) of the [February'24](https://github.com/ThreeMammals/Ocelot/milestone/5) milestone
</details>
