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
    "UpstreamPathTemplate": "/",
    "UpstreamHeaderTemplates": {}, // dictionary
    "UpstreamHost": "",
    "UpstreamHttpMethod": [ "Get" ],
    "DownstreamPathTemplate": "/",
    "DownstreamHttpMethod": "",
    "DownstreamHttpVersion": "",
    "DownstreamHttpVersionPolicy": "",
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
      { "Host": "localhost", "Port": 12345 }
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
    },
    "Metadata": {}
  }

The actual Route schema for properties can be found in the C# `FileRoute <https://github.com/ThreeMammals/Ocelot/blob/main/src/Ocelot/Configuration/File/FileRoute.cs>`_ class.
If you're interested in learning more about how to utilize these options, read below!

Multiple Environments
---------------------

Like any other ASP.NET Core project Ocelot supports configuration file names such as ``appsettings.dev.json``, ``appsettings.test.json`` etc.
In order to implement this add the following to you:

.. code-block:: csharp

    ConfigureAppConfiguration((context, config) =>
    {
        var env = context.HostingEnvironment;
        config.SetBasePath(env.ContentRootPath)
            .AddJsonFile("appsettings.json", true, true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true)
            .AddJsonFile("ocelot.json") // primary config file
            .AddJsonFile($"ocelot.{env.EnvironmentName}.json") // environment file
            .AddEnvironmentVariables();
    })

Ocelot will now use the environment specific configuration and fall back to `ocelot.json`_ if there isn't one.

You also need to set the corresponding environment variable which is ``ASPNETCORE_ENVIRONMENT``.
More info on this can be found in the ASP.NET Core docs: `Use multiple environments in ASP.NET Core <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/environments>`_.

.. _config-merging-files:

Merging Configuration Files
---------------------------

This feature allows users to have multiple configuration files to make managing large configurations easier. [#f1]_

Rather than directly adding the configuration e.g., using ``AddJsonFile("ocelot.json")``, you can achieve the same result by invoking ``AddOcelot()`` as shown below:

.. code-block:: csharp

    ConfigureAppConfiguration((context, config) =>
    {
        var env = context.HostingEnvironment;
        config.SetBasePath(env.ContentRootPath)
            .AddJsonFile("appsettings.json", true, true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true)
            .AddOcelot(env) // happy path
            .AddEnvironmentVariables();
    })

In this scenario Ocelot will look for any files that match the pattern ``^ocelot\.(.*?)\.json$`` and then merge these together.
If you want to set the **GlobalConfiguration** property, you must have a file called ``ocelot.global.json``.

The way Ocelot merges the files is basically load them, loop over them, add any **Routes**, add any **AggregateRoutes** and if the file is called ``ocelot.global.json`` add the **GlobalConfiguration** aswell as any **Routes** or **AggregateRoutes**.
Ocelot will then save the merged configuration to a file called `ocelot.json`_ and this will be used as the source of truth while Ocelot is running.

  **Note 1**: Currently, validation occurs only during the final merging of configurations in Ocelot.
  It's essential to be aware of this when troubleshooting issues.
  We recommend thoroughly inspecting the contents of the ``ocelot.json`` file if you encounter any problems.

  **Note 2**: The Merging feature is operational only during the application's startup.
  Consequently, the merged configuration in ``ocelot.json`` remains static post-merging and startup.
  It's important to be aware that the ``ConfigureAppConfiguration`` method is invoked solely during the startup of an ASP.NET web application.
  Once the Ocelot application has started, you cannot call the ``AddOcelot`` method, nor can you employ the merging feature within ``AddOcelot``.
  If you still require on-the-fly updating of the primary configuration file, ``ocelot.json``, please refer to the :ref:`config-react-to-changes` section.
  Additionally, note that merging partial configuration files (such as ``ocelot.*.json``) on the fly using :doc:`../features/administration` API is not currently implemented.

Keep files in a folder
^^^^^^^^^^^^^^^^^^^^^^

You can also give Ocelot a specific path to look in for the configuration files like below:

.. code-block:: csharp

    ConfigureAppConfiguration((context, config) =>
    {
        var env = context.HostingEnvironment;
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

By default, Ocelot writes the merged configuration to disk as `ocelot.json`_ (the primary configuration file) by adding the file to the ASP.NET configuration provider.

If your web server lacks write permissions for the configuration folder, you can instruct Ocelot to use the merged configuration directly from memory.
Here's how:

.. code-block:: csharp

    // It implicitly calls ASP.NET AddJsonStream extension method for IConfigurationBuilder
    // config.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json)));
    config.AddOcelot(context.HostingEnvironment, MergeOcelotJson.ToMemory);

This feature proves exceptionally valuable in cloud environments like Azure, AWS, and GCP, especially when the app lacks sufficient write permissions to save files.
Furthermore, within Docker container environments, permissions can be scarce, necessitating substantial DevOps efforts to enable file write operations.
Therefore, save time by leveraging this feature! [#f2]_

Reload JSON Config On Change
----------------------------

Ocelot supports reloading the JSON configuration file on change.
For instance, the following will recreate Ocelot internal configuration when the `ocelot.json`_ file is updated manually:

.. code-block:: csharp

    config.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true); // ASP.NET framework version

Important Note: Starting from version `23.2`_, most :ref:`di-configuration-addocelot` include optional ``bool?`` arguments, specifically ``optional`` and ``reloadOnChange``.
Therefore, you have the flexibility to provide these arguments when invoking the internal ``AddJsonFile`` method during the final configuration step (see `AddOcelotJsonFile <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20AddOcelotJsonFile&type=code>`_ implementation):

.. code-block:: csharp

    config.AddJsonFile(ConfigurationBuilderExtensions.PrimaryConfigFile, optional ?? false, reloadOnChange ?? false);

As you can see, in versions prior to `23.2`_, the `AddOcelot extension methods <https://github.com/ThreeMammals/Ocelot/blob/23.1.0/src/Ocelot/DependencyInjection/ConfigurationBuilderExtensions.cs#L111>`_  did not apply the ``reloadOnChange`` argument because it was set to ``false``.
We recommend using the ``AddOcelot`` extension methods to control reloading, rather than relying on the framework's ``AddJsonFile`` method.
For example:

.. code-block:: csharp

    ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile(ConfigurationBuilderExtensions.PrimaryConfigFile, optional: false, reloadOnChange: true); // old approach
        var env = context.HostingEnvironment;
        var mergeTo = MergeOcelotJson.ToFile; // ToMemory
        var folder = "/My/folder";
        FileConfiguration configuration = new(); // read from anywhere and initialize
        config.AddOcelot(env, mergeTo, optional: false, reloadOnChange: true); // with environment and merging type
        config.AddOcelot(folder, env, mergeTo, optional: false, reloadOnChange: true); // with folder, environment and merging type
        config.AddOcelot(configuration, optional: false, reloadOnChange: true); // with configuration object created by your own
        config.AddOcelot(configuration, env, mergeTo, optional: false, reloadOnChange: true); // with configuration object, environment and merging type
    })

Examining the code within the `ConfigurationBuilderExtensions class <https://github.com/ThreeMammals/Ocelot/blob/develop/src/Ocelot/DependencyInjection/ConfigurationBuilderExtensions.cs>`_ would be helpful for gaining a better understanding of the signatures of the overloaded methods [#f2]_.

Store Configuration in `Consul`_
--------------------------------

As a developer, if you have enabled :doc:`../features/servicediscovery` with `Consul`_ support in Ocelot, you may choose to manage your configuration saving to the *Consul* `KV store`_.

Beyond the traditional methods of storing configuration in a file vs folder (:ref:`config-merging-files`), or in-memory (:ref:`config-merging-tomemory`), you also have the alternative to utilize the `Consul`_ server's storage capabilities.

For further details on managing Ocelot configurations via a Consul instance, please consult the ":ref:`sd-consul-configuration-in-kv`" section.

Follow Redirects aka HttpHandlerOptions 
---------------------------------------

    Class: `FileHttpHandlerOptions <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20FileHttpHandlerOptions&type=code>`_

Use ``HttpHandlerOptions`` in a Route configuration to set up ``HttpHandler`` behavior:

.. code-block:: json

  "HttpHandlerOptions": {
    "AllowAutoRedirect": false,
    "UseCookieContainer": false,
    "UseTracing": true,
    "MaxConnectionsPerServer": 100,
    "EnableMultipleHttp2Connections": false
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

* **EnableMultipleHttp2Connections** Gets or sets a value that indicates whether additional HTTP/2 connections can be established to the same server. 
    true if additional HTTP/2 connections are allowed to be created; otherwise, false.

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

.. _config-react-to-changes:

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

.. _config-http-version:

DownstreamHttpVersion
---------------------

Ocelot allows you to choose the HTTP version it will use to make the proxy request. It can be set as ``1.0``, ``1.1`` or ``2.0``.

* `HttpVersion Class <https://learn.microsoft.com/en-us/dotnet/api/system.net.httpversion>`_

.. _config-version-policy:

DownstreamHttpVersionPolicy [#f3]_
----------------------------------

This routing property enables the configuration of the ``VersionPolicy`` property within ``HttpRequestMessage`` objects for downstream HTTP requests.
For additional details, refer to the following documentation:

* `HttpRequestMessage.VersionPolicy Property <https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httprequestmessage.versionpolicy>`_
* `HttpVersionPolicy Enum <https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpversionpolicy>`_
* `HttpVersion Class <https://learn.microsoft.com/en-us/dotnet/api/system.net.httpversion>`_

The ``DownstreamHttpVersionPolicy`` option is intricately linked with the :ref:`config-http-version` setting.
Therefore, merely specifying ``DownstreamHttpVersion`` may sometimes be inadequate, particularly if your downstream services or Ocelot logs report HTTP connection errors such as ``PROTOCOL_ERROR``.
In these routes, selecting the precise ``DownstreamHttpVersionPolicy`` value is crucial for the ``HttpVersion`` policy to prevent such protocol errors.

HTTP/2 version policy
^^^^^^^^^^^^^^^^^^^^^

**Given** you aim to ensure a smooth HTTP/2 connection setup for the Ocelot app and downstream services with SSL enabled:

.. code-block:: json

  {
    "DownstreamScheme": "https",
    "DownstreamHttpVersion": "2.0",
    "DownstreamHttpVersionPolicy": "", // empty
    "DangerousAcceptAnyServerCertificateValidator": true,
    "HttpHandlerOptions":{
        "EnableMultipleHttp2Connections": true
    }
  }

**And** you configure global settings to use Kestrel with this snippet:

.. code-block:: csharp

    var builder = WebApplication.CreateBuilder(args);
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        serverOptions.ConfigureEndpointDefaults(listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http2;
        });
    });

**When** all components are set to communicate exclusively via HTTP/2 without TLS (plain HTTP).

**Then** the downstream services may display error messages such as:

.. code-block::

  HTTP/2 connection error (PROTOCOL_ERROR): Invalid HTTP/2 connection preface

To resolve the issue, ensure that ``HttpRequestMessage`` has its ``VersionPolicy`` set to ``RequestVersionOrHigher``.
Therefore, the ``DownstreamHttpVersionPolicy`` should be defined as follows:

.. code-block:: json

  {
    "DownstreamHttpVersion": "2.0",
    "DownstreamHttpVersionPolicy": "RequestVersionOrHigher", // !
    "HttpHandlerOptions":{
        "EnableMultipleHttp2Connections": true
    }
  }

Dependency Injection
--------------------

*Dependency Injection* for this **Configuration** feature in Ocelot is designed to extend and/or control **the configuration** of the Ocelot kernel before the stage of building ASP.NET MVC pipeline services.
The primary methods are :ref:`di-configuration-addocelot` within the `ConfigurationBuilderExtensions`_ class, which offers several overloaded versions with corresponding signatures.

You can utilize these methods in the ``ConfigureAppConfiguration`` method (located in both **Program.cs** and **Startup.cs**) of your ASP.NET MVC gateway app (minimal web app) to configure the Ocelot pipeline and services.

.. code-block:: csharp

    namespace Microsoft.AspNetCore.Hosting;

    public interface IWebHostBuilder
    {
        IWebHostBuilder ConfigureAppConfiguration(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate);
    }

You can find additional details in the dedicated :ref:`di-configuration-overview` section and in subsequent sections related to the :doc:`../features/dependencyinjection` chapter.

.. _config-route-metadata:

Route Metadata
--------------

Ocelot provides various features such as routing, authentication, caching, load balancing, and more. However, some users may encounter situations where Ocelot does not meet their specific needs or they want to customize its behavior. In such cases, Ocelot allows users to add metadata to the route configuration. This property can store any arbitrary data that users can access in middlewares or delegating handlers. By using the metadata, users can implement their own logic and extend the functionality of Ocelot.

Here is an example:

.. code-block:: json

    {
      "Routes": [
          {
              "UpstreamHttpMethod": [ "GET" ],
              "UpstreamPathTemplate": "/posts/{postId}",
              "DownstreamPathTemplate": "/api/posts/{postId}",
              "DownstreamHostAndPorts": [
                  { "Host": "localhost", "Port": 80 }
              ],
              "Metadata": {
                  "api-id": "FindPost",
                  "my-extension/param1": "overwritten-value",
                  "other-extension/param1": "value1",
                  "other-extension/param2": "value2",
                  "tags": "tag1, tag2, area1, area2, func1",
                  "json": "[1, 2, 3, 4, 5]"
              }
          }
      ],
      "GlobalConfiguration": {
          "Metadata": {
              "instance_name": "dc-1-54abcz",
              "my-extension/param1": "default-value"
          }
      }
    }

Now, the route metadata can be accessed through the `DownstreamRoute` object:

.. code-block:: csharp

    public static class OcelotMiddlewares
    {
        public static Task PreAuthenticationMiddleware(HttpContext context, Func<Task> next)
        {
            var downstreamRoute = context.Items.DownstreamRoute();

            if(downstreamRoute?.Metadata is {} metadata)
            {
                var param1 = metadata.GetValueOrDefault("my-extension/param1") ?? throw new MyExtensionException("Param 1 is null");
                var param2 = metadata.GetValueOrDefault("my-extension/param2", "custom-value");

                // working with metadata
            }

            return next();
        }
    }

""""

.. [#f1] ":ref:`config-merging-files`" feature was requested in `issue 296 <https://github.com/ThreeMammals/Ocelot/issues/296>`_, since then we extended it in `issue 1216 <https://github.com/ThreeMammals/Ocelot/issues/1216>`_ (PR `1227 <https://github.com/ThreeMammals/Ocelot/pull/1227>`_) as ":ref:`config-merging-tomemory`" subfeature which was released as a part of version `23.2`_.
.. [#f2] ":ref:`config-merging-tomemory`" subfeature is based on the ``MergeOcelotJson`` enumeration type with values: ``ToFile`` and ``ToMemory``. The 1st one is implicit by default, and the second one is exactly what you need when merging to memory. See more details on implementations in the `ConfigurationBuilderExtensions`_ class.
.. [#f3] ":ref:`config-version-policy`" feature was requested in `issue 1672 <https://github.com/ThreeMammals/Ocelot/issues/1672>`_ as a part of version `23.3`_.

.. _20.0: https://github.com/ThreeMammals/Ocelot/releases/tag/20.0.0
.. _23.2: https://github.com/ThreeMammals/Ocelot/releases/tag/23.2.0
.. _23.3: https://github.com/ThreeMammals/Ocelot/releases/tag/23.3.0
.. _ocelot.json: https://github.com/ThreeMammals/Ocelot/blob/main/test/Ocelot.ManualTest/ocelot.json
.. _ConfigurationBuilderExtensions: https://github.com/ThreeMammals/Ocelot/blob/develop/src/Ocelot/DependencyInjection/ConfigurationBuilderExtensions.cs
.. _Consul: https://www.consul.io/
.. _KV Store: https://developer.hashicorp.com/consul/docs/dynamic-app-config/kv
