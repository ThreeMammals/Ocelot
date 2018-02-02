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

Then in your Program.cs you will want to have the following. This can be changed if you 
don't wan't to use the default url e.g. UseUrls(someUrls) and should work as long as you keep the WebHostBuilder registration.

.. code-block:: csharp

    public class Program
    {
        public static void Main(string[] args)
        {
            IWebHostBuilder builder = new WebHostBuilder();
            builder.ConfigureServices(s => {
                s.AddSingleton(builder);
            });
            builder.UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
                    var env = hostingContext.HostingEnvironment;
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    config.AddJsonFile("configuration.json");
                    config.AddEnvironmentVariables();
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                })
                .UseIISIntegration()
                .UseStartup<ManualTestStartup>();                
            var host = builder.Build();
            host.Run();
        }
    }

Sadly we need to inject the IWebHostBuilder interface to get the applications scheme, url and port later. I cannot find a better way of doing this at the moment without setting this in a static or some kind of config.

**Startup**

An example startup using a json file for configuration can be seen below. This is the most basic startup and Ocelot has quite a few more options. Detailed in the rest of these docs! If you get a stuck a good place to look is at the ManualTests project in the source code.  

.. code-block:: csharp

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOcelot();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseOcelot().Wait();
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

Then in your Program.cs you will want to have the following. This can be changed if you 
don't wan't to use the default url e.g. UseUrls(someUrls) and should work as long as you keep the WebHostBuilder registration.

.. code-block:: csharp

    public class Program
    {
        public static void Main(string[] args)
        {
            IWebHostBuilder builder = new WebHostBuilder();
            
            builder.ConfigureServices(s => {
                s.AddSingleton(builder);
            });

            builder.UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>();

            var host = builder.Build();

            host.Run();
        }
    }

Sadly we need to inject the IWebHostBuilder interface to get the applications scheme, url and port later. I cannot find a better way of doing this at the moment without setting this in a static or some kind of config.

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