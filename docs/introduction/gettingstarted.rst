Getting Started
===============

Ocelot is designed to work with ASP.NET core only and is currently 
built to netcoreapp2.0 `this <https://docs.microsoft.com/en-us/dotnet/articles/standard/library>`_ documentation may prove helpful when working out if Ocelot would be suitable for you.


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
                .UseStartup<Startup>();

            var host = builder.Build();

            host.Run();
        }
    }

Sadly we need to inject the IWebHostBuilder interface to get the applications scheme, url and port later. I cannot find a better way of doing this at the moment without setting this in a static or some kind of config.

**Startup**

An example startup using a json file for configuration can be seen below. 
Currently this is the only way to get configuration into Ocelot.

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
            services.AddOcelot(Configuration)
                    .AddCacheManager(x => {
                        x.WithMicrosoftLogging(log =>
                        {
                            log.AddConsole(LogLevel.Debug);
                        })
                        .WithDictionaryHandle();
                    });;
            }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));

            app.UseOcelot().Wait();
        }
    }

This is pretty much all you need to get going.......more to come! 