# Ocelot

[![Build status](https://ci.appveyor.com/api/projects/status/r6sv51qx36sis1je?svg=true)](https://ci.appveyor.com/project/TomPallister/ocelot-fcfpb)

[![Coverage Status](https://coveralls.io/repos/github/TomPallister/Ocelot/badge.svg?branch=develop)](https://coveralls.io/github/TomPallister/Ocelot?branch=develop)

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

Ocelot is a bunch of middlewares in a specific order.

Ocelot manipulates the HttpRequest object into a state specified by its configuration until 
it reaches a request builder middleware where it creates a HttpRequestMessage object which is 
used to make a request to a downstream service. The middleware that makes the request is 
the last thing in the Ocelot pipeline. It does not call the next middleware. 
The response from the downstream service is stored in a per request scoped repository 
and retrieved as the requests goes back up the Ocelot pipeline. There is a piece of middleware 
that maps the HttpResponseMessage onto the HttpResponse object and that is returned to the client.
That is basically it with a bunch of other features.

## How to install

Ocelot is designed to work with ASP.NET core only and is currently 
built to netcoreapp1.1 [this](https://docs.microsoft.com/en-us/dotnet/articles/standard/library) documentation may prove helpful when working out if Ocelot would be suitable for you.

Install Ocelot and it's dependencies using NuGet. 

`Install-Package Ocelot`

All versions can be found [here](https://www.nuget.org/packages/Ocelot/)

## Documentation

Please click [here](https://github.com/TomPallister/Ocelot/wiki) for the Ocleot documentation. This includes lots of information and will be helpful if you want to understand the features Ocelot currently offers.

## Coming up

You can see what we are working on [here](https://github.com/TomPallister/Ocelot/projects/1)

## Contributing

Pull requests, issues and commentary welcome! No special process just create a request and get in 
touch either via gitter or create an issue. 


## Things that are currently annoying me

+ The ReRoute configuration object is too large.

+ The base OcelotMiddleware lets you access things that are going to be null
and doesnt check the response is OK. I think the fact you can even call stuff
that isnt available is annoying. Let alone it be null.

[![](https://codescene.io/projects/697/status.svg) Get more details at **codescene.io**.](https://codescene.io/projects/697/jobs/latest-successful/results)

## Instruction

1.Configured your Consul first, make sure your Consul machine IP, Port, ServerName.

2.Configure your Key/Value in Consul, Ke name: OcelotConfiguration, the Value for the document shows the Json format: [Value](https://github.com/TomPallister/Ocelot/blob/develop/test/Ocelot.ManualTest/configuration.json)

3.Create a new NetCore console application and apply the Ocelot assembly;

``` 4.Add configuration file configuration. Json (name can be picked up), and the configuration file is:
  {
   "GlobalConfiguration": {
   "RequestIdKey": "OcRequestId",
   "ServiceDiscoveryProvider": {
      "Host": "localhost",->此处为Consul的主机地址
      "Port": 8500, -> 此处为Consul的主机端口
      "Provider": "Consul" -> 此处为Consul的服务名
     }
    }
  }

5.Associate the just-json configuration file in the Startup of the program
    public Startup(IHostingEnvironment env)
    {
       var builder = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddJsonFile("configuration.json")
                .AddEnvironmentVariables();
        Configuration = builder.Build();
     }

6.In the ConfigureServices method in Startup. Cs, add the following:
   Action<ConfigurationBuilderCachePart> settings = (x) =>
            {
                x.WithMicrosoftLogging(log =>
                {
                    log.AddConsole(LogLevel.Debug);
                })
                .WithDictionaryHandle();
            };
            services.AddOcelot(Configuration, settings);
            var consulConfig = new ConsulRegistryConfiguration("localhost", 8500, "consul");
            services.AddOcelotStoreConfigurationInConsul(consulConfig);
  
  7.In the Configure method in Startup. Cs, add the following content to register the middleware:
     await app.UseOcelot();
  
  8.These are the methods that Ocelot USES to store the configuration in a way that can be stored in a file, but I think you can still       store it in the same way!

