![Ocelot Logo](/images/ocelot_logo.png)

[![CircleCI](https://circleci.com/gh/ThreeMammals/Ocelot/tree/main.svg?style=svg)](https://circleci.com/gh/ThreeMammals/Ocelot/tree/main)

<!-- [![Coverage Status](https://coveralls.io/repos/github/ThreeMammals/Ocelot/badge.svg)](https://coveralls.io/github/ThreeMammals/Ocelot) -->

## About

Ocelot is a .NET API Gateway. This project is aimed at people using .NET running a microservices / service-oriented architecture 
that need a unified point of entry into their system. However it will work with anything that speaks HTTP(S) and run on any platform that ASP.NET Core supports.

In particular we want easy integration with [IdentityServer](https://github.com/IdentityServer) reference and [Bearer](https://oauth.net/2/bearer-tokens/) tokens. 
We have been unable to find this in our current workplace without having to write our own Javascript middlewares to handle the IdentityServer reference tokens.
We would rather use the IdentityServer code that already exists to do this.

Ocelot is a bunch of middlewares in a specific order.

Ocelot manipulates the `HttpRequest` object into a state specified by its configuration until it reaches a request builder middleware, where it creates a `HttpRequestMessage` object which is used to make a request to a downstream service.
The middleware that makes the request is the last thing in the Ocelot pipeline. It does not call the next middleware.
The response from the downstream service is retrieved as the requests goes back up the Ocelot pipeline.
There is a piece of middleware that maps the `HttpResponseMessage` onto the `HttpResponse` object and that is returned to the client.
That is basically it with a bunch of other features!

## Features

A quick list of Ocelot's capabilities, for more information see the [Documentation](#documentation).

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

Ocelot is designed to work with ASP.NET Core and it targets `net6.0`, `net7.0` and `net8.0` frameworks. [^4]

Install [Ocelot package](https://www.nuget.org/packages/Ocelot) and its dependencies using NuGet Package Manager:
```powershell
Install-Package Ocelot
```
Or via the .NET CLI:
```shell
dotnet add package Ocelot
```
All versions can be found [on nuget](https://www.nuget.org/packages/Ocelot#versions-body-tab).

## Documentation
- [Ocelot documentation — Read the Docs](https://ocelot.readthedocs.io)
  <br/>This includes lots of information and will be helpful if you want to understand the features Ocelot currently offers.
- [Ocelot RST Docs](https://github.com/ThreeMammals/Ocelot/tree/develop/docs)
  <br/>This includes source code of documentation as **.rst** files which are up to date for current development.

## Coming up
You can see what we are working on in [backlog](https://github.com/ThreeMammals/Ocelot/issues).

## Contributing

We love to receive contributions from the community, so please keep them coming :octocat: 
<br/>Pull requests, issues and commentary welcome!

Please complete the relevant [template](https://github.com/ThreeMammals/Ocelot/tree/main/.github) for [issues](https://github.com/ThreeMammals/Ocelot/blob/main/.github/ISSUE_TEMPLATE.md) and [PRs](https://github.com/ThreeMammals/Ocelot/blob/main/.github/PULL_REQUEST_TEMPLATE.md).
Sometimes it's worth getting in touch with us to [discuss](https://github.com/ThreeMammals/Ocelot/discussions) changes before doing any work in case this is something we are already doing or it might not make sense.
We can also give advice on the easiest way to do things :octocat: 

Finally, we mark all existing issues as [![label: help wanted][~helpwanted]](https://github.com/ThreeMammals/Ocelot/labels/help%20wanted)
[![label: small effort][~smalleffort]](https://github.com/ThreeMammals/Ocelot/labels/small%20effort)
[![label: medium effort][~mediumeffort]](https://github.com/ThreeMammals/Ocelot/labels/medium%20effort)
[![label: large effort][~largeeffort]](https://github.com/ThreeMammals/Ocelot/labels/large%20effort). [^5]
<br/>If you want to contribute for the first time, we suggest looking at a [![label: help wanted][~helpwanted]](https://github.com/ThreeMammals/Ocelot/labels/help%20wanted) 
[![label: small effort][~smalleffort]](https://github.com/ThreeMammals/Ocelot/labels/small%20effort) 
[![label: good first issue][~goodfirstissue]](https://github.com/ThreeMammals/Ocelot/labels/good%20first%20issue) :octocat: 

[~helpwanted]: https://img.shields.io/badge/-help%20wanted-128A0C.svg
[~smalleffort]: https://img.shields.io/badge/-small%20effort-fef2c0.svg
[~mediumeffort]: https://img.shields.io/badge/-medium%20effort-e0f42c.svg
[~largeeffort]: https://img.shields.io/badge/-large%20effort-10526b.svg
[~goodfirstissue]: https://img.shields.io/badge/-good%20first%20issue-ffc4d8.svg

### Notes
[^1]: Ocelot doesn’t directly support [GraphQL](https://graphql.org/). Developers can easily integrate the [GraphQL for .NET](/graphql-dotnet/graphql-dotnet) library.
[^2]: Ocelot does support [Consul](https://www.consul.io/), [Netflix Eureka](https://www.nuget.org/packages/Steeltoe.Discovery.Eureka), [Service Fabric](https://azure.microsoft.com/en-us/products/service-fabric/) service discovery providers, and special modes like [Dynamic Routing](/ThreeMammals/Ocelot/blob/main/docs/features/servicediscovery.rst#dynamic-routing) and [Custom Providers](/ThreeMammals/Ocelot/blob/main/docs/features/servicediscovery.rst#custom-providers).
[^3]: Retry policies only via [Polly](/App-vNext/Polly) library.
[^4]: Starting with [v21.0](https://github.com/ThreeMammals/Ocelot/releases/tag/21.0.0), the solution's code base supports [Multitargeting](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-multitargeting-overview) as SDK-style projects. It should be easier for teams to move between (migrate to) .NET 6, 7 and 8 frameworks. Also, new features will be available for all .NET SDKs which we support via multitargeting. Find out more here: [Target frameworks in SDK-style projects](https://learn.microsoft.com/en-us/dotnet/standard/frameworks)
[^5]: See all [labels](https://github.com/ThreeMammals/Ocelot/issues/labels) of the repository.
