Getting Started
===============

Ocelot is designed to work with ASP.NET and is currently on net6.0.

.NET 6.0
^^^^^^^^

**Install NuGet package**

Install Ocelot and it's dependencies using nuget. You will need to create a net6.0 project and bring the package into it. Then follow the Startup below and :doc:`../features/configuration` sections
to get up and running.

   ``Install-Package Ocelot``

All versions can be found `here <https://www.nuget.org/packages/Ocelot/>`_.

**Configuration**

The following is a very basic ocelot.json. It won't do anything but should get Ocelot starting.

.. code-block:: json

    {
        "Routes": [],
        "GlobalConfiguration": {
            "BaseUrl": "https://api.mybusiness.com"
        }
    }

If you want some example that actually does something use the following:

.. code-block:: json

    {
        "Routes": [
            {
            "DownstreamPathTemplate": "/todos/{id}",
            "DownstreamScheme": "https",
            "DownstreamHostAndPorts": [
                {
                    "Host": "jsonplaceholder.typicode.com",
                    "Port": 443
                }
            ],
            "UpstreamPathTemplate": "/todos/{id}",
            "UpstreamHttpMethod": [ "Get" ]
            }
        ],
        "GlobalConfiguration": {
            "BaseUrl": "https://localhost:5000"
        }
    }

The most important thing to note here is BaseUrl. Ocelot needs to know the URL it is running under in order to do Header find & replace and for certain administration configurations. When setting this URL it should be the external URL that clients will see Ocelot running on e.g. If you are running containers Ocelot might run on the url http://123.12.1.1:6543 but has something like nginx in front of it responding on https://api.mybusiness.com. In this case the Ocelot base url should be https://api.mybusiness.com. 

If you are using containers and require Ocelot to respond to clients on http://123.12.1.1:6543 then you can do this, however if you are deploying multiple Ocelot's you will probably want to pass this on the command line in some kind of script. Hopefully whatever scheduler you are using can pass the IP.

**Program**

Then in your Program.cs you will want to have the following. The main things to note are  AddOcelot() (adds ocelot services), UseOcelot().Wait() (sets up all the Ocelot middleware).

.. code-block:: csharp

    using System.IO;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Ocelot.DependencyInjection;
    using Ocelot.Middleware;

    namespace OcelotBasic
    {
        public class Program
        {
            public static void Main(string[] args)
            {
                new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config
                        .SetBasePath(hostingContext.HostingEnvironment.ContentRootPath)
                        .AddJsonFile("appsettings.json", true, true)
                        .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", true, true)
                        .AddJsonFile("ocelot.json")
                        .AddEnvironmentVariables();
                })
                .ConfigureServices(s => {
                    s.AddOcelot();
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    //add your logging
                })
                .UseIISIntegration()
                .Configure(app =>
                {
                    app.UseOcelot().Wait();
                })
                .Build()
                .Run();
            }
        }
    }