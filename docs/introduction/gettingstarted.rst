Getting Started
===============

Ocelot is designed to work with .NET Core only and is currently 
built to netcoreapp2.0 `this <https://docs.microsoft.com/en-us/dotnet/articles/standard/library>`_ documentation may prove helpful when working out if Ocelot would be suitable for you.

.NET Core 2.0
^^^^^^^^^^^^^

**Install NuGet package**

Install Ocelot and it's dependecies using nuget. You will need to create a netcoreapp2.0 projct and bring the package into it. Then follow the Startup below and :doc:`../features/configuration` sections
to get up and running.

   ``Install-Package Ocelot``

All versions can be found `here <https://www.nuget.org/packages/Ocelot/>`_.

**Configuration**

The following is a very basic configuration.json. It won't do anything but should get Ocelot starting.

.. code-block:: json

    {
        "ReRoutes": [],
        "GlobalConfiguration": {}
    }

**Program**

Then in your Program.cs you will want to have the following. The main things to note are AddOcelotBaseUrl("http://localhost:5000") (adds the url this instance of Ocelot will run under), 
AddOcelot() (adds ocelot services), UseOcelot().Wait() (sets up all the Ocelot middleware). It is important to call AddOcelotBaseUrl as Ocelot needs to know the URL that it is exposed to the outside world on.

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
                        .AddJsonFile("configuration.json")
                        .AddEnvironmentVariables()
                        .AddOcelotBaseUrl("http://localhost:5000");
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

The following is a very basic configuration.json. It won't do anything but should get Ocelot starting.

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
                .AddJsonFile("configuration.json")
                .AddEnvironmentVariables()
                .AddOcelotBaseUrl("http://localhost:5000");

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