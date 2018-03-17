[<img src="http://threemammals.com/images/ocelot_logo.png">](http://threemammals.com/ocelot)

[![Build status](https://ci.appveyor.com/api/projects/status/r6sv51qx36sis1je?branch=develop&svg=true)](https://ci.appveyor.com/project/TomPallister/ocelot-fcfpb) Windows (AppVeyor)
[![Build Status](https://travis-ci.org/ThreeMammals/Ocelot.svg?branch=develop)](https://travis-ci.org/ThreeMammals/Ocelot) Linux & OSX (Travis)

[![Windows Build history](https://buildstats.info/appveyor/chart/TomPallister/ocelot-fcfpb?branch=develop&includeBuildsFromPullRequest=false)](https://ci.appveyor.com/project/TomPallister/ocelot-fcfpb/history?branch=develop)

[![Coverage Status](https://coveralls.io/repos/github/TomPallister/Ocelot/badge.svg?branch=develop)](https://coveralls.io/github/TomPallister/Ocelot?branch=develop)

# Ocelot

Ocelot is a .NET Api Gateway. This project is aimed at people using .NET running 
a micro services / service orientated architecture 
that need a unified point of entry into their system. However it will worth with anything that
speaks HTTP and run on any platform that asp.net core supports.

In particular I want easy integration with 
IdentityServer reference and bearer tokens. 

We have been unable to find this in my current workplace
without having to write our own Javascript middlewares 
to handle the IdentityServer reference tokens. We would
rather use the IdentityServer code that already exists
to do this.

Ocelot is a bunch of middlewares in a specific order.

Ocelot manipulates the HttpRequest object into a state specified by its configuration until 
it reaches a request builder middleware where it creates a HttpRequestMessage object which is 
used to make a request to a downstream service. The middleware that makes the request is 
the last thing in the Ocelot pipeline. It does not call the next middleware. 
The response from the downstream service is retrieved as the requests goes back up the Ocelot pipeline. 
There is a piece of middleware that maps the HttpResponseMessage onto the HttpResponse object and that 
is returned to the client. That is basically it with a bunch of other features!

## Features

A quick list of Ocelot's capabilities for more information see the [documentation](http://ocelot.readthedocs.io/en/latest/).

* Routing
* Request Aggregation
* Service Discovery with Consul
* Service Fabric
* Authentication
* Authorisation
* Rate Limiting
* Caching
* Retry policies / QoS
* Load Balancing
* Logging / Tracing / Correlation
* Headers / Query String / Claims Transformation
* Custom Middleware / Delegating Handlers
* Configuration / Administration REST API

## How to install

Ocelot is designed to work with ASP.NET core only and is currently 
built to netcoreapp2.0 [this](https://docs.microsoft.com/en-us/dotnet/articles/standard/library) documentation may prove helpful when working out if Ocelot would be suitable for you.

Install Ocelot and it's dependencies using NuGet. 

`Install-Package Ocelot`

All versions can be found [here](https://www.nuget.org/packages/Ocelot/)

## Documentation

Please click [here](http://ocelot.readthedocs.io/en/latest/) for the Ocleot documentation. This includes lots of information and will be helpful if you want to understand the features Ocelot currently offers.

## Coming up

You can see what we are working on [here](https://github.com/ThreeMammals/Ocelot/issues).

## Contributing

We love to receive contributions from the community so please keep them coming :) 

Pull requests, issues and commentary welcome!

Please complete the relavent template for issues and PRs. Sometimes it's worth getting in touch with us to discuss changes 
before doing any work incase this is something we are already doing or it might not make sense. We can also give
advice on the easiest way to do things :)

Finally we mark all existing issues as help wanted, small, medium and large effort. If you want to contriute for the first time I suggest looking at a help wanted & small effort issue :)

## Things that are currently annoying me

[![](https://codescene.io/projects/697/status.svg) Get more details at **codescene.io**.](https://codescene.io/projects/697/jobs/latest-successful/results)



