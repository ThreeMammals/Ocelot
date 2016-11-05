# Ocelot

[![Build status](https://ci.appveyor.com/api/projects/status/roahbe4nl526ysya?svg=true)](https://ci.appveyor.com/project/TomPallister/ocelot)

[![Join the chat at https://gitter.im/Ocelotey/Lobby](https://badges.gitter.im/Ocelotey/Lobby.svg)](https://gitter.im/Ocelotey/Lobby?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

Attempt at a .NET Api Gateway

This project is aimed at people using .NET running 
a micro services / service orientated architecture 
that need a unified point of entry into their system.

In particular I want easy integration with 
IdentityServer reference and bearer tokens. 

We have been unable to find this in my current workplace
without having to write our own Javascript middlewares 
to handle the IdentityServer reference tokens. We would
rather use the IdentityServer code that already exists
to do this.

## How to install

Ocelot is designed to work with ASP.NET core only and is currently 
built to netcoreapp1.4 [this](https://docs.microsoft.com/en-us/dotnet/articles/standard/library) documentation may prove helpful when working out if Ocelot would be suitable for you.

Install Ocelot and it's dependecies using nuget. At the moment 
all we have is the pre version. Once we have something working in 
a half decent way we will drop a version.

`Install-Package Ocelot -Pre`

All versions can be found [here](https://www.nuget.org/packages/Ocelot/)

## Configuration

An example configuration can be found [here](https://github.com/TomPallister/Ocelot/blob/develop/test/Ocelot.ManualTest/configuration.json) 
and an explained configuration can be found [here](https://github.com/TomPallister/Ocelot/blob/develop/configuration-explanation.txt). More detailed instructions to come on how to configure this.

## Startup

An example startup using a json file for configuration can be seen below. 
Currently this is the only way to get configuration into Ocelot.

	 public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddJsonFile("configuration.json")
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            Action<ConfigurationBuilderCachePart> settings = (x) =>
            {
                x.WithMicrosoftLogging(log =>
                {
                    log.AddConsole(LogLevel.Debug);
                })
                .WithDictionaryHandle();
            };

            services.AddOcelotOutputCaching(settings);
            services.AddOcelotFileConfiguration(Configuration);
            services.AddOcelot();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));

            app.UseOcelot();
        }
    }


This is pretty much all you need to get going.......more to come! 

## Routing

Ocelot's primary functionality is to take incomeing http requests and forward them on
to a downstream service. At the moment in the form of another http request (in the future
this could be any transport mechanism.). 

Ocelot's describes the routing of one request to another as a ReRoute. In order to get 
anything working in Ocelot you need to set up a ReRoute in the configuration.

		{
			"ReRoutes": [
			]
		}

In order to set up a ReRoute you need to add one to the json array called ReRoutes like
the following.

		{
            "DownstreamTemplate": "http://jsonplaceholder.typicode.com/posts/{postId}",
            "UpstreamTemplate": "/posts/{postId}",
            "UpstreamHttpMethod": "Put"
        }

The DownstreamTemplate is the URL that this request will be forwarded to.
The UpstreamTemplate is the URL that Ocelot will use to identity which 
DownstreamTemplate to use for a given request. Finally the UpstreamHttpMethod is used so
Ocelot can distinguish between requests to the same URL and is obviously needed to work :)
In Ocelot you can add placeholders for variables to your Templates in the form of {something}.
The placeholder needs to be in both the DownstreamTemplate and UpstreamTemplate. If it is
Ocelot will attempt to replace the placeholder with the correct variable value from the 
Upstream URL when the request comes in.

At the moment all Ocelot routing is case sensitive. I think I will turn this off by default 
in the future with an options to make Ocelot case sensitive per ReRoute.

## Authentication

TBC...

## Authorisation

TBC...

## Claims to Headers Tranformation

TBC...

## Claims to Claims

TBC...

## Claims to Query String Parameters Tranformation

TBC...

## Logging

Ocelot uses the standard logging interfaces ILoggerFactory / ILogger<T> at the moment. 
This is encapsulated in  IOcelotLogger / IOcelotLoggerFactory with an implementation 
for the standard asp.net core logging stuff at the moment. 

There are a bunch of debugging logs in the ocelot middlewares however I think the 
system probably needs more logging in the code it calls into. Other than the debugging
there is a global error handler that should catch any errors thrown and log them as errors.

The reason for not just using bog standard framework logging is that I could not 
work out how to override the request id that get's logged when setting IncludeScopes 
to true for logging settings. Nicely onto the next feature.

## RequestId / CorrelationId

Ocelot supports a client sending a request id in the form of a header. If set Ocelot will
use the requestid for logging as soon as it becomes available in the middleware pipeline. 
Ocelot will also forward the request id with the specified header to the downstream service.
I'm not sure if have this spot on yet in terms of the pipeline order becasue there are a few 
that don't get the users request id at the moment and ocelot just logs not set for request id
which sucks. You can still get the framework request id in the logs if you set 
IncludeScopes true in your logging config.

In order to use the requestid feature in your ReRoute configuration add this setting

		"RequestIdKey": "OcRequestId"

In this example OcRequestId is the request header that contains the clients request id.

## Caching 

Ocelot supports some very rudimentary caching at the moment provider by 
the [CacheManager](http://cachemanager.net/) project. This is an amazing project
that is solving a lot of caching problems. I would reccomend using this package to 
cache with Ocelot. If you look at the example [here](https://github.com/TomPallister/Ocelot/blob/develop/test/Ocelot.ManualTest/Startup.cs)
you can see how the cache manager is setup and then passed into the Ocelot 
AddOcelotOutputCaching configuration method. You can use any settings supported by 
the CacheManager package and just pass them in.

Anyway Ocelot currently supports caching on the URL of the downstream service 
and setting a TTL in seconds to expire the cache. More to come!

In orde to use caching on a route in your ReRoute configuration add this setting.

		"FileCacheOptions": { "TtlSeconds": 15 }

In this example ttl seconds is set to 15 which means the cache will expire after 15 seconds.

## Not supported

Ocelot does not support...
	* Chunked Encoding - Ocelot will always get the body size and return Content-Length 
	header. Sorry if this doesn't work for your use case! 
	* Fowarding a host header - The host header that you send to Ocelot will not be forwarded to
	the downstream service. Obviously this would break everything :(

## Coming up

You can see what we are working on [here](https://github.com/TomPallister/Ocelot/projects/1)


