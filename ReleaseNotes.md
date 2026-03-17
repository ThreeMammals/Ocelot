<!--
Tag to substitute: {0}
https://www.nuget.org/packages/Ocelot/{0}
-->
## Pre-release for .NET 10 SDK (version [25.0](https://www.nuget.org/packages/Ocelot/#versions-body-tab) Beta 1)
> Milestone: [.NET 10](https://github.com/ThreeMammals/Ocelot/milestone/13)

### :information_source: About
This is a pre-release for the [.NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) SDK 10.0.103.
Most features function as usual, with a minor warning for developers and teams who utilize [service discovery](https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/servicediscovery.rst) via [Kubernetes](https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/kubernetes.rst).

### :warning: Warning
1. The [Ocelot.Provider.Kubernetes](https://www.nuget.org/packages/Ocelot.Provider.Kubernetes) extension package is under development. Specifically, the [PollKube](https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/kubernetes.rst#pollkube-provider-3) provider is unstable since it is still in development.
Do not upgrade to the current beta version or use it at your own risk. Other [Kubernetes](https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/kubernetes.rst) providers, such as [Kube](https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/kubernetes.rst#kube-provider) and [WatchKube](https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/kubernetes.rst#watchkube-provider-4), should function correctly.

2. The [Ocelot.Provider.Eureka](https://www.nuget.org/packages/Ocelot.Provider.Eureka) extension package is under development. The integrated [Steeltoe.Discovery.Eureka](https://www.nuget.org/packages/Steeltoe.Discovery.Eureka) package requires an upgrade to version 4.1.0.
