## :fire: Hotfix for issue #2299 (version {0}), aka v[{1}](https://github.com/ThreeMammals/Ocelot/releases/tag/{1}) patch :package:
> Read the Docs: [Ocelot 24.0](https://ocelot.readthedocs.io/en/24.0/) with [PDF](https://ocelot.readthedocs.io/_/downloads/en/24.0/pdf/)
> Hot fixed version: [{1}](https://github.com/ThreeMammals/Ocelot/releases/tag/{1})
> Milestone: [.NET 9](https://github.com/ThreeMammals/Ocelot/milestone/11)

### :information_source: About
:fire: Hot fixed issue: #2299 
:heart: A sincere and heartfelt "Thank You" to **Gracjan Bryłka**, @font3r for reporting the bug.

### :warning: Warning
1. This patch updates only the [Ocelot.Provider.Kubernetes](https://www.nuget.org/packages/Ocelot.Provider.Kubernetes) extension package to version [{0}](https://www.nuget.org/packages/Ocelot.Provider.Kubernetes/{0}). The main [Ocelot](https://www.nuget.org/packages/Ocelot) package was not updated or released, and remains at version [24.0.0](https://www.nuget.org/packages/Ocelot/24.0.0).
2. No further patches are planned for this major version. The next minor release, version **24.1**, codenamed "Globality", is scheduled for [Spring–Summer 2025](https://github.com/ThreeMammals/Ocelot/milestone/9).

### :exclamation: Breaking Changes
Interface Breaking Changes:
- `IKubeApiClientFactory` interface removal: The `ServiceAccountPath` property was removed because it was not intended for public use.
  Interface FQN: `Ocelot.Provider.Kubernetes.Interfaces.IKubeApiClientFactory`
  Property FQN: `Ocelot.Provider.Kubernetes.Interfaces.IKubeApiClientFactory.ServiceAccountPath`
