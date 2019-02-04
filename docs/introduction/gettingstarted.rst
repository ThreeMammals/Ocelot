Getting Started
===============

Ocelot is designed to work with .NET Core only and is currently 
built to netstandard2.0. `This <https://docs.microsoft.com/en-us/dotnet/articles/standard/library>`_ documentation may prove helpful when working out if Ocelot would be suitable for you.

.NET Core 2.1
^^^^^^^^^^^^^

**Install NuGet package**

Install Ocelot and it's dependencies using nuget. You will need to create a netstandard2.0 project and bring the package into it. Then follow the Startup below and :doc:`../features/configuration` sections
to get up and running.

   ``Install-Package Ocelot``

All versions can be found `here <https://www.nuget.org/packages/Ocelot/>`_.

**Configuration**

The following is a very basic ocelot.json. It won't do anything but should get Ocelot starting.

.. code-block:: json

    {
        "ReRoutes": [],
        "GlobalConfiguration": {
            "BaseUrl": "https://api.mybusiness.com"
        }
    }

The most important thing to note here is BaseUrl. Ocelot needs to know the URL it is running under
in order to do Header find & replace and for certain administration configurations. When setting this URL it should be the external URL that clients will see Ocelot running on e.g. If you are running containers Ocelot might run on the url http://123.12.1.1:6543 but has something like nginx in front of it responding on https://api.mybusiness.com. In this case the Ocelot base url should be https://api.mybusiness.com. 

If you are using containers and require Ocelot to respond to clients on http://123.12.1.1:6543
then you can do this, however if you are deploying multiple Ocelot's you will probably want to pass this on the command line in some kind of script. Hopefully whatever scheduler you are using can pass the IP.

**Program**

Then in your Program.cs you will want to have the following. The main things to note are 
AddOcelot() (adds ocelot services), UseOcelot().Wait() (sets up all the Ocelot middleware).

.. code-block:: csharp

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

.NET Core 1.0
^^^^^^^^^^^^^

**Install NuGet package**

Install Ocelot and it's dependecies using nuget. You will need to create a netcoreapp1.0+ projct and bring the package into it. Then follow the Startup below and :doc:`../features/configuration` sections
to get up and running. Please note you will need to choose one of the Ocelot packages from the NuGet feed.

All versions can be found `here <https://www.nuget.org/packages/Ocelot/>`_.

**Configuration**

The following is a very basic ocelot.json. It won't do anything but should get Ocelot starting.

.. code-block:: json

    {
        "ReRoutes": [],
        "GlobalConfiguration": {}
    }

**Program**

Then in your Program.cs you will want to have the following. 

.. code-block:: csharp

    public class Program
    {
        public static void Main(string[] args)
        {
            IWebHostBuilder builder = new WebHostBuilder();
            
            builder.ConfigureServices(s => {
            });

            builder.UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>();

            var host = builder.Build();

            host.Run();
        }
    }

**Startup**

An example startup using a json file for configuration can be seen below. 

.. code-block:: csharp

    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddJsonFile("ocelot.json")
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOcelot(Configuration);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseOcelot().Wait();
        }
    }

This is pretty much all you need to get going.
