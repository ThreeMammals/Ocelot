![Ocelot Logo](https://raw.githubusercontent.com/ThreeMammals/Ocelot/refs/heads/assets/images/ocelot_logo.png)

[![develop Status](https://github.com/ThreeMammals/Ocelot/actions/workflows/develop.yml/badge.svg)](https://github.com/ThreeMammals/Ocelot/actions/workflows/develop.yml)
[![ReadTheDocs Status](https://readthedocs.org/projects/ocelot/badge/?version=develop&style=flat-default)](https://app.readthedocs.org/projects/ocelot/builds/?version__slug=develop)
[![Coveralls Status](https://coveralls.io/repos/github/ThreeMammals/Ocelot/badge.svg?branch=develop)](https://coveralls.io/github/ThreeMammals/Ocelot?branch=develop)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/ThreeMammals/Ocelot/blob/develop/LICENSE.md)
[![NuGet Version](https://img.shields.io/nuget/v/Ocelot.svg)](https://www.nuget.org/packages/Ocelot/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Ocelot.svg)](https://www.nuget.org/packages/Ocelot/)

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

## Features
A concise list of Ocelot's capabilities, for further details refer to [Documentation](#documentation)

* [Routing](https://ocelot.readthedocs.io/en/latest/features/routing.html)
* [Request Aggregation](https://ocelot.readthedocs.io/en/latest/features/requestaggregation.html)
* [GraphQL](https://ocelot.readthedocs.io/en/latest/features/graphql.html) [^1]
* [Service Discovery](https://ocelot.readthedocs.io/en/latest/features/servicediscovery.html) [^2]
* [Service Fabric](https://ocelot.readthedocs.io/en/latest/features/servicefabric.html)
* [Kubernetes](https://ocelot.readthedocs.io/en/latest/features/kubernetes.html)
* [Websockets](https://ocelot.readthedocs.io/en/latest/features/websockets.html)
* [Authentication](https://ocelot.readthedocs.io/en/latest/features/authentication.html)
* [Authorization](https://ocelot.readthedocs.io/en/latest/features/authorization.html)
* [Rate Limiting](https://ocelot.readthedocs.io/en/latest/features/ratelimiting.html)
* [Caching](https://ocelot.readthedocs.io/en/latest/features/caching.html)
* [Quality of Service](https://ocelot.readthedocs.io/en/latest/features/qualityofservice.html) [^3]
* [Load Balancer](https://ocelot.readthedocs.io/en/latest/features/loadbalancer.html)
* [Logging](https://ocelot.readthedocs.io/en/latest/features/logging.html) / [Tracing](https://ocelot.readthedocs.io/en/latest/features/tracing.html) / [Correlation](https://ocelot.readthedocs.io/en/latest/features/requestid.html)
* [Headers](https://ocelot.readthedocs.io/en/latest/features/headerstransformation.html) / [Method](https://ocelot.readthedocs.io/en/latest/features/methodtransformation.html) / [Query String](https://ocelot.readthedocs.io/en/latest/search.html?q=Query+String&check_keywords=yes&area=default) / [Claims](https://ocelot.readthedocs.io/en/latest/features/claimstransformation.html) Transformation
* [Custom Middleware](https://ocelot.readthedocs.io/en/latest/features/middlewareinjection.html) / [Delegating Handlers](https://ocelot.readthedocs.io/en/latest/features/delegatinghandlers.html)
* [Configuration](https://ocelot.readthedocs.io/en/latest/features/configuration.html) / [Administration](https://ocelot.readthedocs.io/en/latest/features/administration.html) REST API
* [Platform](https://ocelot.readthedocs.io/en/latest/building/building.html?highlight=Platform#building) & Cloud Agnostic [Building](https://ocelot.readthedocs.io/en/latest/building/building.html)

## Install
Ocelot is designed to work with [ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/) and it targets `net8.0` [LTS](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core#release-types) and `net9.0` [STS](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core#release-types) target framework monikers ([TFMs](https://learn.microsoft.com/en-us/dotnet/standard/frameworks#supported-target-frameworks)). [^4]

Install [Ocelot](https://www.nuget.org/packages/Ocelot) package and its dependencies using NuGet package manager:
```powershell
Install-Package Ocelot
```
Or via the .NET CLI:
```shell
dotnet add package Ocelot
```
All versions are available [on NuGet](https://www.nuget.org/packages/Ocelot#versions-body-tab).

## Documentation
- [RST-sources](https://github.com/ThreeMammals/Ocelot/tree/develop/docs):
  This includes the source code of the documentation as **.rst**-files, which are up to date for current development.
- [Read the Docs](https://ocelot.readthedocs.io):
  This includes a lot of information and will be helpful if you want to understand the features Ocelot currently offers.
- [Ask Ocelot Guru](https://gurubase.io/g/ocelot):
  It is an Ocelot-focused AI designed to answer your questions. [^5]

## Coming up
You can see what we are working on in the [backlog](https://github.com/ThreeMammals/Ocelot/issues).

## Contributing
We love to receive contributions from the community, so please keep them coming.
Pull requests, issues, and commentary welcome! <img src="https://raw.githubusercontent.com/ThreeMammals/Ocelot/refs/heads/assets/images/octocat.png" alt="octocat" height="20">

Please complete the relevant [template](https://github.com/ThreeMammals/Ocelot/tree/main/.github) for [issues](https://github.com/ThreeMammals/Ocelot/blob/main/.github/ISSUE_TEMPLATE.md) and [pull requests](https://github.com/ThreeMammals/Ocelot/blob/main/.github/PULL_REQUEST_TEMPLATE.md).
Sometimes it's worth getting in touch with us to [discuss](https://github.com/ThreeMammals/Ocelot/discussions) changes before doing any work in case this is something we are already doing or it might not make sense.
We can also give advice on the easiest way to do things <img src="https://raw.githubusercontent.com/ThreeMammals/Ocelot/refs/heads/assets/images/octocat.png" alt="octocat" height="20">

Finally, we mark all existing issues as [![label: help wanted][~helpwanted]](https://github.com/ThreeMammals/Ocelot/labels/help%20wanted)
[![label: small effort][~smalleffort]](https://github.com/ThreeMammals/Ocelot/labels/small%20effort)
[![label: medium effort][~mediumeffort]](https://github.com/ThreeMammals/Ocelot/labels/medium%20effort)
[![label: large effort][~largeeffort]](https://github.com/ThreeMammals/Ocelot/labels/large%20effort). [^6]
If you want to contribute for the first time, we suggest looking at a [![label: help wanted][~helpwanted]](https://github.com/ThreeMammals/Ocelot/labels/help%20wanted) 
[![label: small effort][~smalleffort]](https://github.com/ThreeMammals/Ocelot/labels/small%20effort) 
[![label: good first issue][~goodfirstissue]](https://github.com/ThreeMammals/Ocelot/labels/good%20first%20issue) <img src="https://raw.githubusercontent.com/ThreeMammals/Ocelot/refs/heads/assets/images/octocat.png" alt="octocat" height="20">

[~helpwanted]: https://img.shields.io/badge/-help%20wanted-128A0C.svg
[~smalleffort]: https://img.shields.io/badge/-small%20effort-fef2c0.svg
[~mediumeffort]: https://img.shields.io/badge/-medium%20effort-e0f42c.svg
[~largeeffort]: https://img.shields.io/badge/-large%20effort-10526b.svg
[~goodfirstissue]: https://img.shields.io/badge/-good%20first%20issue-ffc4d8.svg

### Notes
[^1]: Ocelot does not directly support [GraphQL](https://graphql.org/). Developers can easily integrate the [GraphQL for .NET](https://github.com/graphql-dotnet/graphql-dotnet) library. 
[^2]: Ocelot supports [Consul](https://www.consul.io/), [Netflix Eureka](https://www.nuget.org/packages/Steeltoe.Discovery.Eureka), [Service Fabric](https://azure.microsoft.com/en-us/products/service-fabric/) service discovery providers, as well as special modes like [Dynamic Routing](/ThreeMammals/Ocelot/blob/main/docs/features/servicediscovery.rst#dynamic-routing) and [Custom Providers](/ThreeMammals/Ocelot/blob/main/docs/features/servicediscovery.rst#custom-providers).
[^3]: Retry policies only via [Polly](/App-vNext/Polly) library.
[^4]: Starting with version [21.0](https://github.com/ThreeMammals/Ocelot/releases/tag/21.0.0), the solution's code base supports [Multitargeting](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-multitargeting-overview) as SDK-style projects. It should be easier for teams to migrate to the currently supported [.NET 8 and 9](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core#lifecycle) frameworks. Also, new features will be available for all .NET SDKs that we support via multitargeting. Find out more here: [Target frameworks in SDK-style projects](https://learn.microsoft.com/en-us/dotnet/standard/frameworks)
[^5]: [Ocelot Guru](https://gurubase.io/g/ocelot) is an unofficial tool to get answers regarding Ocelot: please consider it an advanced search tool. Thus, we have an official [Questions & Answers](https://github.com/ThreeMammals/Ocelot/discussions/categories/q-a) category in the [Discussions](https://github.com/ThreeMammals/Ocelot/discussions) space.
[^6]: See all [labels](https://github.com/ThreeMammals/Ocelot/issues/labels) for the repository, which are useful for searching and filtering.
