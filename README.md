![Ocelot Logo](https://raw.githubusercontent.com/ThreeMammals/Ocelot/refs/heads/assets/images/ocelot_logo.png)

[![Release Status](https://github.com/ThreeMammals/Ocelot/actions/workflows/release.yml/badge.svg)](https://github.com/ThreeMammals/Ocelot/actions/workflows/release.yml)
[![Development Status](https://github.com/ThreeMammals/Ocelot/actions/workflows/develop.yml/badge.svg)](https://github.com/ThreeMammals/Ocelot/actions/workflows/develop.yml)
[![ReadTheDocs](https://readthedocs.org/projects/ocelot/badge/?version=latest&style=flat-square)](https://app.readthedocs.org/projects/ocelot/builds/?version__slug=latest)
[![Coveralls](https://coveralls.io/repos/github/ThreeMammals/Ocelot/badge.svg?branch=main)](https://coveralls.io/github/ThreeMammals/Ocelot?branch=main)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/ThreeMammals/Ocelot/blob/main/LICENSE.md)
[![NuGet](https://img.shields.io/nuget/v/Ocelot?logo=nuget&label=NuGet&color=blue)](https://www.nuget.org/packages/Ocelot/ "Download Ocelot from NuGet.org")
[![Downloads](https://img.shields.io/nuget/dt/Ocelot?logo=nuget&label=Downloads)](https://www.nuget.org/packages/Ocelot/ "Total Ocelot downloads from NuGet.org")

[~docspassing]: https://img.shields.io/badge/Docs-passing-44CC11?style=flat-square
[~docsfailing]: https://img.shields.io/badge/Docs-failing-red?style=flat-square

## About
Ocelot is a .NET [API gateway](https://www.bing.com/search?q=API+gateway).
This project is aimed at people using .NET running a microservices (service-oriented) architecture that needs a unified point of entry into their system.
However, it will work with anything that speaks HTTP(S) and runs on any platform that [ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/) supports.

<!--
In particular we want easy integration with [IdentityServer](https://github.com/IdentityServer) reference and [Bearer](https://oauth.net/2/bearer-tokens/) tokens. 
We have been unable to find this in our current workplace without having to write our own Javascript middlewares to handle the IdentityServer reference tokens.
We would rather use the IdentityServer code that already exists to do this.
-->

Ocelot consists of a series of ASP.NET Core [middlewares](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/) arranged in a specific order.
Ocelot [custom middlewares](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/write) manipulate the `HttpRequest` object into a state specified by its configuration until it reaches a request builder middleware, where it creates a `HttpRequestMessage` object, which is used to make a request to a downstream service.
The middleware that makes the request is the last thing in the Ocelot pipeline. It does not call the next middleware.
The response from the downstream service is retrieved as the request goes back up the Ocelot pipeline.
There is a piece of middleware that maps the `HttpResponseMessage` onto the `HttpResponse` object, and that is returned to the client.
That is basically it, with a bunch of other features!

## Install
Ocelot is designed to work with [ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/) and it targets `net8.0` [LTS](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core#release-types) and `net9.0` [STS](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core#release-types) target framework monikers ([TFMs](https://learn.microsoft.com/en-us/dotnet/standard/frameworks#supported-target-frameworks)). [^1]

Install [Ocelot](https://www.nuget.org/packages/Ocelot) package and its dependencies using NuGet package manager:
```powershell
Install-Package Ocelot
```
Or via the .NET CLI:
```shell
dotnet add package Ocelot
```
> All versions are available [on NuGet](https://www.nuget.org/packages/Ocelot#versions-body-tab).

## Documentation
- [RST-sources](https://github.com/ThreeMammals/Ocelot/tree/develop/docs):
  This includes the source code for the documentation (in reStructuredText format, .rst files), which is up to date for the current [development](https://github.com/ThreeMammals/Ocelot/tree/develop/).
  And the rendered HTML documentation is available [here](https://ocelot.readthedocs.io/en/develop/).
- [Read the Docs](https://ocelot.readthedocs.io):
  This official website, in HTML format, contains a wealth of information and will be helpful if you want to understand the [features](#features) that Ocelot currently offers.
  The rendered HTML documentation, which is currently in [development](https://github.com/ThreeMammals/Ocelot/tree/develop/docs), is available [here](https://ocelot.readthedocs.io/en/develop/).
- [Ask Ocelot Guru](https://gurubase.io/g/ocelot):
  It is an AI focused on Ocelot, designed to answer your questions. [^2]

## Features
The primary features—[Configuration](https://ocelot.readthedocs.io/en/latest/features/configuration.html) and [Routing](https://ocelot.readthedocs.io/en/latest/features/routing.html)—are always utilized by users, even in a minimal app setup, without customizations or extra configurations.
Ocelot's capabilities are categorized into three main groups of features: *solid*, *hybrid*, and *feature-family* groups, which are explained below.
- *Solid features* are unique to Ocelot. They do not contain subfeatures and are not related to other features.
- *Hybrid features*, on the other hand, have multiple relationships with other features and can be part of other features.
- *Feature families* are large groups that consist of multiple subfeatures.

| Group | Features |
|-------|----------|
|Primary|[Configuration](https://ocelot.readthedocs.io/en/latest/features/configuration.html), [Routing](https://ocelot.readthedocs.io/en/latest/features/routing.html)|
| Solid |[Caching](https://ocelot.readthedocs.io/en/latest/features/caching.html), [Delegating Handlers](https://ocelot.readthedocs.io/en/latest/features/delegatinghandlers.html), [Quality of Service](https://ocelot.readthedocs.io/en/latest/features/qualityofservice.html)[^3], [Rate Limiting](https://ocelot.readthedocs.io/en/latest/features/ratelimiting.html)|
| Hybrid|[Administration](https://ocelot.readthedocs.io/en/latest/features/administration.html), [Aggregation](https://ocelot.readthedocs.io/en/latest/features/aggregation.html)[^4], [Authentication](https://ocelot.readthedocs.io/en/latest/features/authentication.html), [Configuration](https://ocelot.readthedocs.io/en/latest/features/configuration.html), [Dependency Injection](https://ocelot.readthedocs.io/en/latest/features/dependencyinjection.html), [Load Balancer](https://ocelot.readthedocs.io/en/latest/features/loadbalancer.html)|
|Family|[Configuration](https://ocelot.readthedocs.io/en/latest/features/configuration.html), [Routing](https://ocelot.readthedocs.io/en/latest/features/routing.html), [Logging](https://ocelot.readthedocs.io/en/latest/features/logging.html), [Transformations](https://ocelot.readthedocs.io/en/latest/search.html?q=Transformation), [Service Discovery](https://ocelot.readthedocs.io/en/latest/features/servicediscovery.html)[^5] |

Feature groups are explained in the table below &rarr;
| Feature | Relationships and Notes |
|---------|-------------------------|
| [Administration](https://ocelot.readthedocs.io/en/latest/features/administration.html) | [Administration](https://ocelot.readthedocs.io/en/latest/features/administration.html) heavily depends on [Authentication](https://ocelot.readthedocs.io/en/latest/features/authentication.html), and [Administration API](https://ocelot.readthedocs.io/en/latest/features/administration.html#administration-api) methods are part of [Authentication](https://ocelot.readthedocs.io/en/latest/features/authentication.html), [Caching](https://ocelot.readthedocs.io/en/latest/features/caching.html), and [Configuration](https://ocelot.readthedocs.io/en/latest/features/configuration.html) |
|[Aggregation](https://ocelot.readthedocs.io/en/latest/features/aggregation.html)[^4]| [Aggregation](https://ocelot.readthedocs.io/en/latest/features/aggregation.html) relies on [Routing](https://ocelot.readthedocs.io/en/latest/features/routing.html) |
| [Authentication](https://ocelot.readthedocs.io/en/latest/features/authentication.html) | [Authentication](https://ocelot.readthedocs.io/en/latest/features/authentication.html) followed by [Authorization](https://ocelot.readthedocs.io/en/latest/features/authorization.html) |
| [Configuration](https://ocelot.readthedocs.io/en/latest/features/configuration.html) | [Configuration](https://ocelot.readthedocs.io/en/latest/features/configuration.html) depends on [Dependency Injection](https://ocelot.readthedocs.io/en/latest/features/dependencyinjection.html), including `GET`/`POST` operations via the [Administration REST API](https://ocelot.readthedocs.io/en/latest/features/administration.html#administration-api), a specialized [Websockets](https://ocelot.readthedocs.io/en/latest/features/websockets.html) scheme/protocol, advanced [Middleware Injection](https://ocelot.readthedocs.io/en/latest/features/middlewareinjection.html), and [Metadata](https://ocelot.readthedocs.io/en/latest/features/metadata.html)-based extensions |
| [Routing](https://ocelot.readthedocs.io/en/latest/features/routing.html) | [Routing](https://ocelot.readthedocs.io/en/latest/features/routing.html) offers specialized [Websockets](https://ocelot.readthedocs.io/en/latest/features/websockets.html) and [Dynamic Routing](https://ocelot.readthedocs.io/en/latest/features/servicediscovery.html#dynamic-routing) modes but does not support [GraphQL](https://ocelot.readthedocs.io/en/latest/features/graphql.html)[^6] |
|[Load Balancer](https://ocelot.readthedocs.io/en/latest/features/loadbalancer.html)|[Load Balancer](https://ocelot.readthedocs.io/en/latest/features/loadbalancer.html) is a critical dependency for [Service Discovery](https://ocelot.readthedocs.io/en/latest/features/servicediscovery.html) |
| [Logging](https://ocelot.readthedocs.io/en/latest/features/logging.html) | [Logging](https://ocelot.readthedocs.io/en/latest/features/logging.html) includes [Error Handling](https://ocelot.readthedocs.io/en/latest/features/errorcodes.html) and [Tracing](https://ocelot.readthedocs.io/en/latest/features/tracing.html) |
| [Service Discovery](https://ocelot.readthedocs.io/en/latest/features/servicediscovery.html)[^5] | [Service Discovery](https://ocelot.readthedocs.io/en/latest/features/servicediscovery.html) with the following discovery providers: [Consul](https://ocelot.readthedocs.io/en/latest/features/servicediscovery.html#consul), [Kubernetes](https://ocelot.readthedocs.io/en/latest/features/kubernetes.html), [Eureka](https://ocelot.readthedocs.io/en/latest/features/servicediscovery.html#eureka), and [Service Fabric](https://ocelot.readthedocs.io/en/latest/features/servicefabric.html) |
| [Transformations](https://ocelot.readthedocs.io/en/latest/search.html?q=Transformation) | They provide transformations for [Claims](https://ocelot.readthedocs.io/en/latest/features/claimstransformation.html), [Headers](https://ocelot.readthedocs.io/en/latest/features/headerstransformation.html), and [Method](https://ocelot.readthedocs.io/en/latest/features/methodtransformation.html) |

> Ocelot customizations can be configured using [Metadata](https://ocelot.readthedocs.io/en/latest/features/metadata.html), developed with [Delegating Handlers](https://ocelot.readthedocs.io/en/latest/features/delegatinghandlers.html), and in advanced scenarios, they can be developed and then configured with [Middleware Injection](https://ocelot.readthedocs.io/en/latest/features/middlewareinjection.html).
For further details, refer to the [Documentation](#documentation).

## Contributing
You can see what we are working on in the [backlog](https://github.com/ThreeMammals/Ocelot/issues).
We love to receive contributions from the community, so please keep them coming.
Pull requests, issues, and commentary welcome! <img src="https://raw.githubusercontent.com/ThreeMammals/Ocelot/refs/heads/assets/images/octocat.png" alt="octocat" height="25" />

Please complete the relevant [template](https://github.com/ThreeMammals/Ocelot/tree/main/.github) for [issues](https://github.com/ThreeMammals/Ocelot/blob/main/.github/ISSUE_TEMPLATE.md) and [pull requests](https://github.com/ThreeMammals/Ocelot/blob/main/.github/PULL_REQUEST_TEMPLATE.md).
Sometimes it's worth getting in touch with us to [discuss](https://github.com/ThreeMammals/Ocelot/discussions) changes before doing any work in case this is something we are already doing or it might not make sense.
We can also give advice on the easiest way to do things <img src="https://raw.githubusercontent.com/ThreeMammals/Ocelot/refs/heads/assets/images/octocat.png" alt="octocat" height="25" />

Finally, we mark all existing issues as [![label: help wanted][~helpwanted]](https://github.com/ThreeMammals/Ocelot/labels/help%20wanted)
[![label: small effort][~smalleffort]](https://github.com/ThreeMammals/Ocelot/labels/small%20effort)
[![label: medium effort][~mediumeffort]](https://github.com/ThreeMammals/Ocelot/labels/medium%20effort)
[![label: large effort][~largeeffort]](https://github.com/ThreeMammals/Ocelot/labels/large%20effort).[^7]
If you want to contribute for the first time, we suggest looking at a [![label: help wanted][~helpwanted]](https://github.com/ThreeMammals/Ocelot/labels/help%20wanted) 
[![label: small effort][~smalleffort]](https://github.com/ThreeMammals/Ocelot/labels/small%20effort) 
[![label: good first issue][~goodfirstissue]](https://github.com/ThreeMammals/Ocelot/labels/good%20first%20issue) <img src="https://raw.githubusercontent.com/ThreeMammals/Ocelot/refs/heads/assets/images/octocat.png" alt="octocat" height="25" />

[~helpwanted]: https://img.shields.io/badge/-help%20wanted-128A0C.svg
[~smalleffort]: https://img.shields.io/badge/-small%20effort-fef2c0.svg
[~mediumeffort]: https://img.shields.io/badge/-medium%20effort-e0f42c.svg
[~largeeffort]: https://img.shields.io/badge/-large%20effort-10526b.svg
[~goodfirstissue]: https://img.shields.io/badge/-good%20first%20issue-ffc4d8.svg

### Notes
[^1]: Starting with version [21](https://github.com/ThreeMammals/Ocelot/releases/tag/21.0.0) and higher, the solution's code base supports [Multitargeting](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-multitargeting-overview) as SDK-style projects. It should be easier for teams to migrate to the currently supported [.NET 8 and 9](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core#lifecycle) frameworks. Also, new features will be available for all .NET SDKs that we support via multitargeting. Find out more here: [Target frameworks in SDK-style projects](https://learn.microsoft.com/en-us/dotnet/standard/frameworks)
[^2]: [Ocelot Guru](https://gurubase.io/g/ocelot) is an unofficial tool to get answers regarding Ocelot: please consider it an advanced search tool. Thus, we have an official [Questions & Answers](https://github.com/ThreeMammals/Ocelot/discussions/categories/q-a) category in the [Discussions](https://github.com/ThreeMammals/Ocelot/discussions) space.
[^3]: Retry policies only via [Polly](https://github.com//App-vNext/Polly) library referenced within the [Ocelot.Provider.Polly](https://www.nuget.org/packages/Ocelot.Provider.Polly) extension package.
[^4]: Previously, the [Aggregation](https://ocelot.readthedocs.io/en/latest/features/aggregation.html) feature was called [Request Aggregation](https://ocelot.readthedocs.io/en/23.4.3/features/requestaggregation.html) in versions [23.4.3](https://github.com/ThreeMammals/Ocelot/releases/tag/23.4.3) and earlier. Internally, within the Ocelot team, this feature is referred to as [Multiplexer](https://github.com/ThreeMammals/Ocelot/tree/main/src/Ocelot/Multiplexer).
[^5]: Ocelot supports the following service discovery providers: (**1**) [Consul](https://www.consul.io) through the [Ocelot.Provider.Consul](https://www.nuget.org/packages/Ocelot.Provider.Consul) extension package, (**2**) [Kubernetes](https://kubernetes.io) via the [Ocelot.Provider.Kubernetes](https://www.nuget.org/packages/Ocelot.Provider.Kubernetes) extension package, and (**3**) [Netflix Eureka](https://spring.io/projects/spring-cloud-netflix), which utilizes the [Steeltoe.Discovery.Eureka](https://www.nuget.org/packages/Steeltoe.Discovery.Eureka) package referenced within the [Ocelot.Provider.Eureka](https://www.nuget.org/packages/Ocelot.Provider.Eureka) extension package. Additionally, Ocelot supports (**4**) Azure [Service Fabric](https://azure.microsoft.com/en-us/products/service-fabric/) for service discovery, along with special modes such as [Dynamic Routing](https://ocelot.readthedocs.io/en/latest/features/servicediscovery.html#dynamic-routing) and [Custom Providers](https://ocelot.readthedocs.io/en/latest/features/servicediscovery.html#custom-providers).
[^6]: Ocelot does not directly support [GraphQL](https://graphql.org/). Developers can easily integrate the [GraphQL for .NET](https://github.com/graphql-dotnet/graphql-dotnet) library. 
[^7]: See all [labels](https://github.com/ThreeMammals/Ocelot/issues/labels) for the repository, which are useful for searching and filtering.
