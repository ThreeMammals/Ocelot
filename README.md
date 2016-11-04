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

Ocelot is designed to work with ASP.NET core only and is currently built to netcoreapp1.4 [this](https://docs.microsoft.com/en-us/dotnet/articles/standard/library) documentation may prove helpful when working out if Ocelot would be suitable for you.

Install Ocelot and it's dependecies using nuget. At the moment all we have is the pre version. Once we have something working in a half decent way we will drop a version.

`Install-Package Ocelot -Pre`

All versions can be found [here](https://www.nuget.org/packages/Ocelot/)

## Configuration

An example configuration can be found [here](https://github.com/TomPallister/Ocelot/blob/develop/test/Ocelot.ManualTest/configuration.json) 
and an explained configuration can be found [here](https://github.com/TomPallister/Ocelot/blob/develop/configuration-explanation.txt). More detailed instructions to come on how to configure this.

## Startup

An example startup using a json file for configuration can be seen below. Currently this is the only way to get configuration into Ocelot.

	public class Startup
    {
        public IConfigurationRoot Configuration { get; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("configuration.json", optional: true, reloadOnChange: true)
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOcelotFileConfiguration(Configuration);
            services.AddOcelot();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseOcelot();
        }
    }


This is pretty much all you need to get going.......more to come! 

## Logging

Ocelot uses the standard logging interfaces ILoggerFactory / ILogger<T> at the moment. This is encapsulated in  IOcelotLogger with 
an implementation for the standard asp.net core logging stuff at the moment.

## Caching 

Ocelot supports some very rudimentary caching at the moment provider by the [CacheManager](http://cachemanager.net/) project. This is an amazing project
that is solving a lot of caching problems. I would reccomend using this package to cache with Ocelot. If you look at the example [here](https://github.com/TomPallister/Ocelot/blob/develop/test/Ocelot.ManualTest/Startup.cs)
you can see how the cache manager is setup and then passed into the Ocelot AddOcelotOutputCaching configuration method. You can 
use any settings supported by the CacheManager package and just pass them in.

Anyway Ocelot currently supports caching on the URL of the downstream service and setting a TTL in seconds to expire the cache. More to come!

## Not supported

Ocelot does not support...
	* Chunked Encoding - Ocelot will always get the body size and return Content-Length header. Sorry
	if this doesn't work for your use case! 
	* Fowarding a host header - The host header that you send to Ocelot will not be forwarded to
	the downstream service. Obviously this would break everything :(


