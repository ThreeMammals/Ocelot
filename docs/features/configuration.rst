Configuration
=============

An example configuration can be found here in `ocelot.json`_.
There are two sections to the configuration: an array of **Routes** and a **GlobalConfiguration**:

* The **Routes** are the objects that tell Ocelot how to treat an upstream request.
* The **GlobalConfiguration** is a bit hacky and allows overrides of Route specific settings. It's useful if you do not want to manage lots of Route specific settings.

.. code-block:: json

  {
    "Routes": [],
    "GlobalConfiguration": {}
  }

Here is an example Route configuration. You don't need to set all of these things but this is everything that is available at the moment:

.. code-block:: json

  {
    "DownstreamPathTemplate": "/",
    "UpstreamPathTemplate": "/",
    "UpstreamHttpMethod": [ "Get" ],
    "DownstreamHttpMethod": "",
    "DownstreamHttpVersion": "",
    "AddHeadersToRequest": {},
    "AddClaimsToRequest": {},
    "RouteClaimsRequirement": {},
    "AddQueriesToRequest": {},
    "RequestIdKey": "",
    "FileCacheOptions": {
      "TtlSeconds": 0,
      "Region": "europe-central"
    },
    "RouteIsCaseSensitive": false,
    "ServiceName": "",
    "DownstreamScheme": "http",
    "DownstreamHostAndPorts": [
      { "Host": "localhost", "Port": 51876 }
    ],
    "QoSOptions": {
      "ExceptionsAllowedBeforeBreaking": 0,
      "DurationOfBreak": 0,
      "TimeoutValue": 0
    },
    "LoadBalancer": "",
    "RateLimitOptions": {
      "ClientWhitelist": [],
      "EnableRateLimiting": false,
      "Period": "",
      "PeriodTimespan": 0,
      "Limit": 0
    },
    "AuthenticationOptions": {
      "AuthenticationProviderKey": "",
      "AllowedScopes": []
    },
    "HttpHandlerOptions": {
      "AllowAutoRedirect": true,
      "UseCookieContainer": true,
      "UseTracing": true,
      "MaxConnectionsPerServer": 100
    },
    "DangerousAcceptAnyServerCertificateValidator": false,
    "SecurityOptions": {
      "IPAllowedList": [],
      "IPBlockedList": [],
      "ExcludeAllowedFromBlocked": false
    }
  }

More information on how to use these options is below.

Multiple Environments
---------------------

Like any other ASP.NET Core project Ocelot supports configuration file names such as **appsettings.dev.json**, **appsettings.test.json** etc.
In order to implement this add the following to you:

.. code-block:: csharp

    ConfigureAppConfiguration((hostingContext, config) =>
    {
        var env = hostingContext.HostingEnvironment;
        config.SetBasePath(env.ContentRootPath)
            .AddJsonFile("appsettings.json", true, true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true)
            .AddJsonFile("ocelot.json")
            .AddJsonFile($"ocelot.{env.EnvironmentName}.json")
            .AddEnvironmentVariables();
    })

Ocelot will now use the environment specific configuration and fall back to `ocelot.json`_ if there isn't one.

You also need to set the corresponding environment variable which is ``ASPNETCORE_ENVIRONMENT``.
More info on this can be found in the ASP.NET Core docs: `Use multiple environments in ASP.NET Core <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/environments>`_.

.. _config-merging-files:

Merging Configuration Files [#f1]_
----------------------------------

This feature allows users to have multiple configuration files to make managing large configurations easier [#f1]_.

Instead of adding the configuration directly e.g. ``AddJsonFile("ocelot.json")`` you can call ``AddOcelot()`` like below:

.. code-block:: csharp

    ConfigureAppConfiguration((hostingContext, config) =>
    {
        var env = hostingContext.HostingEnvironment;
        config.SetBasePath(env.ContentRootPath)
            .AddJsonFile("appsettings.json", true, true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true)
            .AddOcelot(env) // happy path
            .AddEnvironmentVariables();
    })

In this scenario Ocelot will look for any files that match the pattern ``^ocelot\.(.*?)\.json$`` and then merge these together.
If you want to set the **GlobalConfiguration** property, you must have a file called **ocelot.global.json**. 

The way Ocelot merges the files is basically load them, loop over them, add any Routes, add any **AggregateRoutes** and if the file is called **ocelot.global.json** add the **GlobalConfiguration** aswell as any Routes or **AggregateRoutes**.
Ocelot will then save the merged configuration to a file called `ocelot.json`_ and this will be used as the source of truth while Ocelot is running.

At the moment there is no validation at this stage it only happens when Ocelot validates the final merged configuration.
This is something to be aware of when you are investigating problems. 
We would advise always checking what is in `ocelot.json`_ file if you have any problems.

Keep files in a folder
^^^^^^^^^^^^^^^^^^^^^^

You can also give Ocelot a specific path to look in for the configuration files like below:

.. code-block:: csharp

    ConfigureAppConfiguration((hostingContext, config) =>
    {
        var env = hostingContext.HostingEnvironment;
        config.SetBasePath(env.ContentRootPath)
            .AddJsonFile("appsettings.json", true, true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true)
            .AddOcelot("/my/folder", env) // happy path
            .AddEnvironmentVariables();
    })

Ocelot needs the ``HostingEnvironment`` so it knows to exclude anything environment specific from the merging algorithm. 

.. _config-merging-tomemory:

Merging files to memory [#f2]_
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

By default Ocelot writes the merged configuration back to disk as `ocelot.json`_ (primary configuration file) with adding the file to config.

If your server don't have write permissions to your configuration folder, you can instruct Ocelot to use the merged configuration directly from memory instead, like this:

.. code-block:: csharp

    // It implicitly calls ASP.NET AddJsonStream extension method for IConfigurationBuilder
    // config.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json)));
    config.AddOcelot(hostingContext.HostingEnvironment, MergeOcelotJson.ToMemory);

This feature is extremely useful in a cloud environment such as Azure, AWS, GCP where the application may not have enough write permissions to save files.
Additionally, the Docker container environment may lack permissions and would require significant DevOps effort to implement file write operation.
So don't waste your time: use the feature! [#f2]_

Reload JSON Config On Change
----------------------------

Ocelot supports reloading the JSON configuration file on change. For instance, the following will recreate Ocelot internal configuration when the `ocelot.json`_ file is updated manually:

.. code-block:: csharp

    config.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true); // ASP.NET framework version

Please note: as of version `23.2`_, most ``AddOcelot`` methods have optional ``bool?`` arguments aka ``optional`` and ``reloadOnChange``.
So, you can supply these arguments to the internal ``AddJsonFile`` call in the last step of configuring (see `AddOcelotJsonFile <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20AddOcelotJsonFile&type=code>`_) e.g.

.. code-block:: csharp

    config.AddJsonFile(ConfigurationBuilderExtensions.PrimaryConfigFile, optional ?? false, reloadOnChange ?? false);

As you see, in previous versions less than `23.2`_ ``AddOcelot`` extension methods didn't apply the ``reloadOnChange`` arg because it was ``false``.
Finally, we recommend to control reloading by ``AddOcelot`` extension methods instead of usage of the framework ``AddJsonFile`` one e.g.

.. code-block:: csharp

    ConfigureAppConfiguration((hostingContext, config) =>
    {
        config.AddJsonFile(ConfigurationBuilderExtensions.PrimaryConfigFile, optional: false, reloadOnChange: true); // old approach
        var env = hostingContext.HostingEnvironment;
        var mergeTo = MergeOcelotJson.ToFile; // ToMemory
        var folder = "/My/folder";
        FileConfiguration configuration = new(); // read from anywhere and initialize
        config.AddOcelot(env, mergeTo, optional: false, reloadOnChange: true); // with environment and merging type
        config.AddOcelot(folder, env, mergeTo, optional: false, reloadOnChange: true); // with folder, environment and merging type
        config.AddOcelot(configuration, optional: false, reloadOnChange: true); // with configuration object created by your own
        config.AddOcelot(configuration, env, mergeTo, optional: false, reloadOnChange: true); // with configuration object, environment and merging type
    })

It would be useful to look into `the ConfigurationBuilderExtensions class <https://github.com/ThreeMammals/Ocelot/blob/develop/src/Ocelot/DependencyInjection/ConfigurationBuilderExtensions.cs>`_ code and note something to better understand the signatures of the overloaded methods [#f2]_.

Store Configuration in Consul
-----------------------------

The first thing you need to do is install the `NuGet package <https://www.nuget.org/packages/Ocelot.Provider.Consul>`_ that provides `Consul <https://www.consul.io/>`_ support in Ocelot.

.. code-block:: powershell

    Install-Package Ocelot.Provider.Consul

Then you add the following when you register your services Ocelot will attempt to store and retrieve its configuration in Consul KV store.
In order to register Consul services we must call the ``AddConsul()`` and ``AddConfigStoredInConsul()`` extensions using the ``OcelotBuilder`` being returned by ``AddOcelot()`` [#f3]_ like below:

.. code-block:: csharp

    services
        .AddOcelot()
        .AddConsul()
        .AddConfigStoredInConsul();

You also need to add the following to your `ocelot.json`_. This is how Ocelot finds your Consul agent and interacts to load and store the configuration from Consul.

.. code-block:: json

  "GlobalConfiguration": {
    "ServiceDiscoveryProvider": {
      "Host": "localhost",
      "Port": 9500
    }
  }

The team decided to create this feature after working on the Raft consensus algorithm and finding out its super hard.
Why not take advantage of the fact Consul already gives you this! 
We guess it means if you want to use Ocelot to its fullest, you take on Consul as a dependency for now.

This feature has a `3 seconds <https://github.com/search?q=repo%3AThreeMammals%2FOcelot+TimeSpan.FromSeconds%283%29&type=code>`_ TTL cache before making a new request to your local Consul agent.

.. _config-consul-key:

Consul Configuration Key [#f4]_
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

If you are using Consul for configuration (or other providers in the future), you might want to key your configurations: so you can have multiple configurations.

In order to specify the key you need to set the **ConfigurationKey** property in the **ServiceDiscoveryProvider** options of the configuration JSON file e.g.

.. code-block:: json

  "GlobalConfiguration": {
    "ServiceDiscoveryProvider": {
      "Host": "localhost",
      "Port": 9500,
      "ConfigurationKey": "Ocelot_A"
    }
  }

In this example Ocelot will use ``Ocelot_A`` as the key for your configuration when looking it up in Consul.
If you do not set the **ConfigurationKey**, Ocelot will use the string ``InternalConfiguration`` as the key.

Follow Redirects aka HttpHandlerOptions 
---------------------------------------

    Class: `FileHttpHandlerOptions <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20FileHttpHandlerOptions&type=code>`_

Use ``HttpHandlerOptions`` in a Route configuration to set up ``HttpHandler`` behavior:

.. code-block:: json

  "HttpHandlerOptions": {
    "AllowAutoRedirect": false,
    "UseCookieContainer": false,
    "UseTracing": true,
    "MaxConnectionsPerServer": 100
  },

* **AllowAutoRedirect** is a value that indicates whether the request should follow redirection responses.
  Set it ``true`` if the request should automatically follow redirection responses from the downstream resource; otherwise ``false``.
  The default value is ``false``.

* **UseCookieContainer** is a value that indicates whether the handler uses the ``CookieContainer`` property to store server cookies and uses these cookies when sending requests.
  The default value is ``false``.
  Please note, if you use the ``CookieContainer``, Ocelot caches the ``HttpClient`` for each downstream service.
  This means that all requests to that downstream service will share the same cookies. 
  `Issue 274 <https://github.com/ThreeMammals/Ocelot/issues/274>`_ was created because a user noticed that the cookies were being shared.
  The Ocelot team tried to think of a nice way to handle this but we think it is impossible. 
  If you don't cache the clients, that means each request gets a new client and therefore a new cookie container.
  If you clear the cookies from the cached client container, you get race conditions due to inflight requests. 
  This would also mean that subsequent requests don't use the cookies from the previous response!
  All in all not a great situation.
  We would avoid setting **UseCookieContainer** to ``true`` unless you have a really really good reason.
  Just look at your response headers and forward the cookies back with your next request! 

* **MaxConnectionsPerServer** This controls how many connections the internal ``HttpClient`` will open. This can be set at Route or global level.

.. _ssl-errors:

SSL Errors
----------

If you want to ignore SSL warnings (errors), set the following in your Route config:

.. code-block:: json

    "DangerousAcceptAnyServerCertificateValidator": true

**We don't recommend doing this!**
The team suggests creating your own certificate and then getting it trusted by your local (remote) machine, if you can.
For ``https`` scheme this fake validator was requested by `issue 309 <https://github.com/ThreeMammals/Ocelot/issues/309>`_.
For ``wss`` scheme this fake validator was added by `PR 1377 <https://github.com/ThreeMammals/Ocelot/pull/1377>`_. 

As a team, we do not consider it as an ideal solution. From one side, the community wants to have an option to work with self-signed certificates.
But from other side, currently source code scanners detect 2 serious security vulnerabilities because of this fake validator in `20.0`_ release.
The Ocelot team will rethink this unfortunate situation, and it is highly likely that this feature will at least be redesigned or removed completely.

For now, the SSL fake validator makes sense in local development environments when a route has ``https`` or ``wss`` schemes having self-signed certificate for those routes.
There are no other reasons to use the **DangerousAcceptAnyServerCertificateValidator** property at all!

As a team, we highly recommend following these instructions when developing your gateway app with Ocelot:

* **Local development environments**. Use the feature to avoid SSL errors for self-signed certificates in case of ``https`` or ``wss`` schemes.
  We understand that some routes should have dowstream scheme exactly with SSL, because they are also in development, and/or deployed using SSL protocols.
  But we believe that especially for local development, you can switch from ``https`` to ``http`` without any objection since the services are in development and there is no risk of data leakage.

* **Remote development environments**. Everything is the same as for local development. But this case is less strict, you have more options to use real certificates to switch off the feature.
  For instance, you can deploy downstream services to cloud & hosting providers which have own signed certificates for SSL.
  At least your team can deploy one remote web server to host downstream services. Install own certificate or use cloud provider's one.

* **Staging or testing environments**. We do not recommend to use self-signed certificates because web servers should have valid certificates installed.
  Ask your system administrator or DevOps engineers of your team to create valid certificates.

* **Production environments**. **Do not use self-signed certificates at all!**
  System administrators or DevOps engineers must create real valid certificates being signed by hosting or cloud providers.
  **Switch off the feature for all routes!** Remove the **DangerousAcceptAnyServerCertificateValidator** property for all routes in production version of `ocelot.json`_ file!

React to Configuration Changes
------------------------------

Resolve ``IOcelotConfigurationChangeTokenSource`` interface from the DI container if you wish to react to changes to the Ocelot configuration via the :doc:`../features/administration` API or `ocelot.json`_ being reloaded from the disk.
You may either poll the change token's ``IChangeToken.HasChanged`` property, or register a callback with the ``RegisterChangeCallback`` method.

Polling the HasChanged property
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

.. code-block:: csharp

    public class ConfigurationNotifyingService : BackgroundService
    {
        private readonly IOcelotConfigurationChangeTokenSource _tokenSource;
        private readonly ILogger _logger;

        public ConfigurationNotifyingService(IOcelotConfigurationChangeTokenSource tokenSource, ILogger logger)
        {
            _tokenSource = tokenSource;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_tokenSource.ChangeToken.HasChanged)
                {
                    _logger.LogInformation("Configuration updated");
                }
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
    
Registering a callback
^^^^^^^^^^^^^^^^^^^^^^

.. code-block:: csharp

    public class MyDependencyInjectedClass : IDisposable
    {
        private readonly IOcelotConfigurationChangeTokenSource _tokenSource;
        private readonly IDisposable _callbackHolder;

        public MyClass(IOcelotConfigurationChangeTokenSource tokenSource)
        {
            _tokenSource    = tokenSource;
            _callbackHolder = tokenSource.ChangeToken.RegisterChangeCallback(_ => Console.WriteLine("Configuration changed"), null);
        }
        public void Dispose()
        {
            _callbackHolder.Dispose();
        }
    }

DownstreamHttpVersion
---------------------

Ocelot allows you to choose the HTTP version it will use to make the proxy request. It can be set as ``1.0``, ``1.1`` or ``2.0``.

Dependency Injection
--------------------

*Dependency Injection* for this **Configuration** feature in Ocelot is designed to extend and/or control **configuring** of Ocelot core before the stage of building ASP.NET MVC pipeline services.
The main method is :ref:`di-configuration-addocelot` of the `ConfigurationBuilderExtensions`_ class which has a bunch of overloaded versions with corresponding signatures.

Use them in the following ``ConfigureAppConfiguration`` method (**Program.cs** and **Startup.cs**) of your ASP.NET MVC gateway app (minimal web app) to configure Ocelot pipeline and services:

.. code-block:: csharp

    namespace Microsoft.AspNetCore.Hosting;

    public interface IWebHostBuilder
    {
        IWebHostBuilder ConfigureAppConfiguration(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate);
    }

More details you could find in the special ":ref:`di-configuration-overview`" section and in subsequent sections of the :doc:`../features/dependencyinjection` feature.

""""

.. [#f1] ":ref:`config-merging-files`" feature was requested in `issue 296 <https://github.com/ThreeMammals/Ocelot/issues/296>`_, since then we extended it in `issue 1216 <https://github.com/ThreeMammals/Ocelot/issues/1216>`_ (PR `1227 <https://github.com/ThreeMammals/Ocelot/pull/1227>`_) as ":ref:`config-merging-tomemory`" subfeature which was released as a part of the version `23.2`_.
.. [#f2] ":ref:`config-merging-tomemory`" subfeature is based on the ``MergeOcelotJson`` enumeration type with values: ``ToFile`` and ``ToMemory``. The 1st one is implicit by default, and the second one is exactly what you need when merging to memory. See more details on implementations in the `ConfigurationBuilderExtensions`_ class.
.. [#f3] :ref:`di-the-addocelot-method` adds default ASP.NET services to DI container. You could call another extended :ref:`di-addocelotusingbuilder-method` while configuring services to develop your own :ref:`di-custom-builder`. See more instructions in the ":ref:`di-addocelotusingbuilder-method`" section of :doc:`../features/dependencyinjection` feature.
.. [#f4] ":ref:`config-consul-key`" feature was requested in `issue 346 <https://github.com/ThreeMammals/Ocelot/issues/346>`_ as a part of version `7.0.0 <https://github.com/ThreeMammals/Ocelot/releases/tag/7.0.0>`_.

.. _20.0: https://github.com/ThreeMammals/Ocelot/releases/tag/20.0.0
.. _23.2: https://github.com/ThreeMammals/Ocelot/releases/tag/23.2.0
.. _ocelot.json: https://github.com/ThreeMammals/Ocelot/blob/main/test/Ocelot.ManualTest/ocelot.json
.. _ConfigurationBuilderExtensions: https://github.com/ThreeMammals/Ocelot/blob/develop/src/Ocelot/DependencyInjection/ConfigurationBuilderExtensions.cs
