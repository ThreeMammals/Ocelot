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

Ocelot is designed to work with ASP.NET core only and is currently built to .NET Standard 1.4 [this](https://docs.microsoft.com/en-us/dotnet/articles/standard/library) documentation may prove helpful when working out if Ocelot would be suitable for you.

Install Ocelot and it's dependecies using nuget. At the moment all we have is the pre version. Once we have something working in a half decent way we will drop a version.

`Install-Package Ocelot -Pre`

All versions can be found [here](https://www.nuget.org/packages/Ocelot/)

## Configuration

An example configuration can be found [here](https://github.com/TomPallister/Ocelot/blob/develop/test/Ocelot.ManualTest/configuration.json). More detailed instructions to come on how to configure this.

"ReRoutes": [
	{
		# The url we are forwarding the request to
		"UpstreamTemplate": "/identityserverexample",
		# The path we are listening on for this re route
		"UpstreamTemplate": "/identityserverexample",
		# The method we are listening for on this re route
		"UpstreamHttpMethod": "Get",
		# Only support identity server at the moment
		"AuthenticationOptions": {
			"Provider": "IdentityServer",
			"ProviderRootUrl": "http://localhost:52888",
			"ScopeName": "api",
			"AdditionalScopes": [
				"openid",
				"offline_access"
			],
		# Required if using reference tokens
			"ScopeSecret": "secret"
		},
		# WARNING - will overwrite any headers already in the request with these values.
		# Ocelot will look in the user claims for the key in [] then return the value and save
		# it as a header with the given key before the colon (:). The index selection on value 
		# means that Ocelot will use the delimiter specified after the next > to split the 
		# claim value and return the index specified.
		"AddHeadersToRequest": {
			"CustomerId": "Claims[CustomerId] > value",
			"LocationId": "Claims[LocationId] > value",
			"UserType": "Claims[sub] > value[0] > |",
			"UserId": "Claims[sub] > value[1] > |"
		},
		# WARNING - will overwrite any claims already in the request with these values.
		# Ocelot will look in the user claims for the key in [] then return the value and save
		# it as a claim with the given key before the colon (:). The index selection on value 
		# means that Ocelot will use the delimiter specified after the next > to split the 
		# claim value and return the index specified.
		"AddClaimsToRequest": {
			"CustomerId": "Claims[CustomerId] > value",
			"LocationId": "Claims[LocationId] > value",
			"UserType": "Claims[sub] > value[0] > |",
			"UserId": "Claims[sub] > value[1] > |"
		},
		# WARNING - will overwrite any query string entries already in the request with these values.
		# Ocelot will look in the user claims for the key in [] then return the value and save
		# it as a query string with the given key before the colon (:). The index selection on value 
		# means that Ocelot will use the delimiter specified after the next > to split the 
		# claim value and return the index specified.
		"AddQueriesToRequest": {
			"CustomerId": "Claims[CustomerId] > value",
			"LocationId": "Claims[LocationId] > value",
			"UserType": "Claims[sub] > value[0] > |",
			"UserId": "Claims[sub] > value[1] > |"
		},
		# This specifies any claims that are required for the user to access this re route.
		# In this example the user must have the claim type UserType and 
		# the value must be registered
		"RouteClaimsRequirement": {
			"UserType": "registered"
		},
		# This tells Ocelot to look for a header and use its value as a request/correlation id. 
		# If it is set here then the id will be forwarded to the downstream service. If it
		# does not then it will not be forwarded
		"RequestIdKey": "OcRequestId"
	}

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

Ocelot uses the standard logging interfaces ILoggerFactory / ILogger<T> as such you can use any logging provider you like such as default, nlog, serilog or whatever you want.

## Not supported

Ocelot does not support...
	- Chunked Encoding - Ocelot will always get the body size and return Content-Length header. Sorry
	if this doesn't work for your use case! 
	- Fowarding a host header - The host header that you send to Ocelot will not be forwarded to
	the downstream service. Obviously this would break everything :(


