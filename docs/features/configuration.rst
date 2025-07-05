.. _ocelot.json: https://github.com/ThreeMammals/Ocelot/blob/main/samples/Basic/ocelot.json
.. _Program: https://github.com/ThreeMammals/Ocelot/blob/main/samples/Configuration/Program.cs
.. _ConfigurationBuilderExtensions: https://github.com/ThreeMammals/Ocelot/blob/develop/src/Ocelot/DependencyInjection/ConfigurationBuilderExtensions.cs
.. _Consul: https://www.consul.io/
.. _KV Store: https://developer.hashicorp.com/consul/docs/dynamic-app-config/kv

Configuration
=============

An example configuration can be found here in `ocelot.json`_.
There are two major sections to the configuration: an array of ``Routes`` and a ``GlobalConfiguration`` sections:

.. code-block:: json

  {
    "Routes": [],
    "GlobalConfiguration": {}
  }

From the :doc:`../introduction/gettingstarted` chapter and its :ref:`getstarted-configuration` section, you may already know that there are four total configuration sections:

.. code-block:: json

  {
    "Routes": [], // static routes
    "DynamicRoutes": [],
    "Aggregates": [], // BFF
    "GlobalConfiguration": {}
  }

.. list-table::
    :widths: 25 75
    :header-rows: 1

    * - *Section*
      - *Description*
    * - ``Routes`` with :ref:`config-route-schema`
      - The static objects that tell Ocelot how to treat an upstream request.
        Once static routes have been loaded during gateway startup, in general, they cannot be changed during the lifetime of the app instance, with a few exceptional use cases.
    * - ``DynamicRoutes`` with :ref:`config-dynamic-route-schema`
      - This section enables dynamic routing when using a :doc:`../features/servicediscovery` provider.
        Please refer to the :ref:`routing-dynamic` docs for more details.
    * - ``Aggregates`` with :ref:`config-aggregate-route-schema`
      - This section allows specifying aggregated routes that compose multiple normal routes and map their responses into one JSON object.
        It allows you to start implementing a *Back-end For a Front-end* (BFF) type architecture with Ocelot.
        Please refer to the :doc:`../features/aggregation` chapter for more details.
    * - ``GlobalConfiguration`` with :ref:`config-global-configuration-schema`
      - This section is a bit hacky and allows overrides of static route-specific settings.
        It is useful if you do not want to manage lots of route-specific settings.

To fully understand all configuration capabilities, we recommend reading all sections below.

.. _config-route-schema:

Route Schema
------------

.. _FileRoute: https://github.com/ThreeMammals/Ocelot/blob/main/src/Ocelot/Configuration/File/FileRoute.cs

    Class: `FileRoute`_

Here is the complete route configuration, also known as the *"route schema,"* of top-level properties.
You do not need to set all of these things, but this is everything that is available at the moment.

.. code-block:: json

    {
      "AddClaimsToRequest": {}, // dictionary
      "AddHeadersToRequest": {}, // dictionary
      "AddQueriesToRequest": {}, // dictionary
      "AuthenticationOptions": {}, // object
      "ChangeDownstreamPathTemplate": {}, // dictionary
      "DangerousAcceptAnyServerCertificateValidator": false,
      "DelegatingHandlers": [], // array of strings
      "DownstreamHeaderTransform": {}, // dictionary
      "DownstreamHostAndPorts": [], // array of FileHostAndPort
      "DownstreamHttpMethod": "",
      "DownstreamHttpVersion": "",
      "DownstreamHttpVersionPolicy": "",
      "DownstreamPathTemplate": "",
      "DownstreamScheme": "",
      "FileCacheOptions": {}, // object
      "HttpHandlerOptions": {}, // object
      "Key": "",
      "LoadBalancerOptions": {}, // object
      "Metadata": {}, // dictionary
      "Priority": 1, // integer
      "QoSOptions": {}, // object
      "RateLimitOptions": {}, // object
      "RequestIdKey": "",
      "RouteClaimsRequirement": {}, // dictionary
      "RouteIsCaseSensitive": false,
      "SecurityOptions": {}, // object
      "ServiceName": "",
      "ServiceNamespace": "",
      "Timeout": 0, // nullable integer
      "UpstreamHeaderTemplates": {}, // dictionary
      "UpstreamHeaderTransform": {}, // dictionary
      "UpstreamHost": "",
      "UpstreamHttpMethod": [], // array of strings
      "UpstreamPathTemplate": ""
    },

The actual route schema with all the properties can be found in the C# `FileRoute`_ class.

.. _config-dynamic-route-schema:

Dynamic Route Schema
--------------------

.. _FileDynamicRoute: https://github.com/ThreeMammals/Ocelot/blob/main/src/Ocelot/Configuration/File/FileDynamicRoute.cs

    Class: `FileDynamicRoute`_

Here is the complete dynamic route configuration, also known as the *"dynamic route schema,"* of top-level properties.

.. code-block:: json

    {
      "DownstreamHttpVersion": "",
      "DownstreamHttpVersionPolicy": "",
      "Metadata": {}, // dictionary
      "RateLimitRule": {},
      "ServiceName": "",
      "Timeout": 0 // nullable integer
    }

The actual dynamic route schema with all the properties can be found in the C# `FileDynamicRoute`_ class.

.. _config-aggregate-route-schema:

Aggregate Route Schema
----------------------

.. _FileAggregateRoute: https://github.com/ThreeMammals/Ocelot/blob/main/src/Ocelot/Configuration/File/FileAggregateRoute.cs

    Class: `FileAggregateRoute`_

Here is the complete aggregated route configuration, also known as the *"aggregate route schema,"* of top-level properties.

.. code-block:: json

    {
      "Aggregator": "",
      "Priority": 1, // integer
      "RouteIsCaseSensitive": false,
      "RouteKeys": [], // array of strings
      "RouteKeysConfig": [], // array of AggregateRouteConfig
      "UpstreamHeaderTemplates": {}, // dictionary
      "UpstreamHost": "",
      "UpstreamHttpMethod": [], // array of strings
      "UpstreamPathTemplate": ""
    }

The actual aggregated route schema with all the properties can be found in the C# `FileAggregateRoute`_ class.

.. _config-global-configuration-schema:

Global Configuration Schema
---------------------------

.. _FileGlobalConfiguration: https://github.com/ThreeMammals/Ocelot/blob/main/src/Ocelot/Configuration/File/FileGlobalConfiguration.cs

    Class: `FileGlobalConfiguration`_

Here is the complete global configuration, also known as the *"global configuration schema,"* of top-level properties.

.. code-block:: json

    {
      "BaseUrl": "",
      "CacheOptions": {},
      "DownstreamHttpVersion": "",
      "DownstreamHttpVersionPolicy": "",
      "DownstreamScheme": "",
      "HttpHandlerOptions": {},
      "LoadBalancerOptions": {},
      "MetadataOptions": {},
      "QoSOptions": {},
      "RateLimitOptions": {},
      "RequestIdKey": "",
      "SecurityOptions": {},
      "ServiceDiscoveryProvider": {},
      "Timeout": 0 // nullable integer
    }

The actual global configuration schema with all the properties can be found in the C# `FileGlobalConfiguration`_ class.

.. _config-overview:

Configuration Overview
----------------------

:doc:`../features/dependencyinjection` of the *Configuration* feature in Ocelot allows you to extend, manage, and build Ocelot Core *configuration* **before** the stage of building ASP.NET Core services.

To configure the Ocelot Core and services, use the following abstract program-structure, which must be presented in your `Program`_:

1. **Create application builder**: The ``Microsoft.AspNetCore.Builder.WebApplication`` has three overloaded versions of the ``CreateBuilder()`` methods.
   Our recommendation is to utilize arguments possibly coming from terminal sessions into an app host; thus, use the ``CreateBuilder(args)`` method.

  .. code-block:: csharp

      var builder = WebApplication.CreateBuilder(args);

2. **Set up the configuration builder**: Utilize the ``WebApplicationBuilder.Configuration`` property, which returns a ``ConfigurationManager`` object implementing the target ``IConfigurationBuilder`` interface.

  .. code-block:: csharp

      builder.Configuration.AddOcelot(...);

3. **Forward configuration to the Ocelot builder**: The ``Ocelot.DependencyInjection.ServiceCollectionExtensions`` class has three overloaded versions of the ``AddOcelot(IServiceCollection)`` methods, which return an ``IOcelotBuilder`` object.

  .. code-block:: csharp

      builder.Services.AddOcelot(builder.Configuration);

4. **Finish the app setup**, add middlewares, and finally run the application: Let's write the final algorithm.

  .. code-block:: csharp

      var builder = WebApplication.CreateBuilder(args); // step 1
      builder.Configuration.AddOcelot(...); // step 2
      builder.Services.AddOcelot(builder.Configuration); // step 3

      // Step 4
      var app = builder.Build();
      await app.UseOcelot();
      await app.RunAsync();

For comprehensive documentation of configuration DI-extensions, please refer to the :ref:`di-configuration-overview` section in the :doc:`../features/dependencyinjection` chapter.

Multiple Environments
---------------------

Like any other ASP.NET Core project Ocelot supports configuration file names such as ``appsettings.dev.json``, ``appsettings.test.json`` etc.
In order to implement this add the following to you:

.. code-block:: csharp
  :emphasize-lines: 4,5,7

    var builder = WebApplication.CreateBuilder(args);
    builder.Configuration
        .SetBasePath(builder.Environment.ContentRootPath)
        .AddJsonFile("ocelot.json") // primary config file
        .AddJsonFile($"ocelot.{builder.Environment.EnvironmentName}.json");
    builder.Services
        .AddOcelot(builder.Configuration);

Ocelot will now use the environment specific configuration and fall back to `ocelot.json`_ if there isn't one.
Another version of the configuration above, which is based on configuration providers, is the following:

.. code-block:: csharp
  :emphasize-lines: 4,6,7,9

    var builder = WebApplication.CreateBuilder(args);
    builder.Configuration
        .SetBasePath(builder.Environment.ContentRootPath)
        .AddOcelot() // single ocelot.json file without environment one
        // or
        .AddOcelot(builder.Environment)
        .AddJsonFile($"ocelot.{builder.Environment.EnvironmentName}.json");
    builder.Services
        .AddOcelot(builder.Configuration);

You also need to set the corresponding ``ASPNETCORE_ENVIRONMENT`` variable.

    **Note 1**: More info on configuration can be found in the ASP.NET Core documentation:

    * `Use multiple environments in ASP.NET Core <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/environments>`_
    * `Configuration in ASP.NET Core <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/>`_

    **Note 2**: Calling the following configuration methods is rudimentary in ASP.NET Core because of internal encapsulation in the default builder, aka ``CreateBuilder(args)`` method.

    .. code-block:: csharp
      :emphasize-lines: 3,4,5

        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration
            .AddJsonFile("appsettings.json", true, true) // not required
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, true) // not required
            .AddEnvironmentVariables() // not required
            // ...

    This is explained in the `Default application configuration sources <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-9.0#default-application-configuration-sources>`_ docs; thus, remove these optional methods.

.. _config-merging-files:

Merging Files [#f1]_
--------------------

  **Sample**: `Ocelot.Samples.Configuration <https://github.com/ThreeMammals/Ocelot/blob/main/samples/Configuration/>`_

This feature allows users to have multiple configuration files to make managing large configurations easier.

Rather than directly adding the configuration e.g., using ``AddJsonFile("ocelot.json")``, you can achieve the same result by invoking ``AddOcelot()`` as shown below:

.. code-block:: csharp
  :emphasize-lines: 3

    builder.Configuration
        .SetBasePath(builder.Environment.ContentRootPath)
        .AddOcelot(builder.Environment); // will skip environment file

In this scenario, Ocelot will look for any files that match the pattern ``^ocelot\.(.*?)\.json$`` as the regular expression and then merge these together.
The environment file will be skipped aka ``ocelot.{builder.Environment.EnvironmentName}.json``.
If you want to set the ``GlobalConfiguration`` property, you must have a file called ``ocelot.global.json``.

The way Ocelot merges the files is basically load them, loop over them, skip environment file, add any ``Routes``, add any ``AggregateRoutes`` and if the file is called ``ocelot.global.json`` add the ``GlobalConfiguration`` aswell as any ``Routes`` or ``AggregateRoutes``.
Ocelot will then save the merged configuration to a file called `ocelot.json`_ and this will be used as the source of truth while Ocelot is running.

  **Note 1**: Currently, validation occurs only during the final merging of configurations in Ocelot.
  It's essential to be aware of this when troubleshooting issues.
  We recommend thoroughly inspecting the contents of the ``ocelot.json`` file if you encounter any problems.

  **Note 2**: The Merging feature is operational only during the application's startup.
  Consequently, the merged configuration in ``ocelot.json`` remains static post-merging and startup.
  Once the Ocelot application has started, you cannot call the ``AddOcelot`` method, nor can you employ the merging feature within ``AddOcelot``.
  If you still require on-the-fly updating of the primary configuration file, ``ocelot.json``, please refer to the :ref:`config-react-to-changes` section.
  Additionally, note that merging partial configuration files (such as ``ocelot.*.json``) on the fly using :doc:`../features/administration` API is not currently implemented.

  **Note 3**: An alternative to static merged configurations could be the construction of the ``FileConfiguration`` object before passing it as an argument to the :ref:`di-configuration-addocelot-methods` method.
  Refer to the :ref:`config-build-from-scratch` subsection for details.

Keep files in a folder
^^^^^^^^^^^^^^^^^^^^^^

You can also give Ocelot a specific path to look in for the configuration files as shown below:

.. code-block:: csharp
  :emphasize-lines: 3

    builder.Configuration
        .SetBasePath(builder.Environment.ContentRootPath)
        .AddOcelot("/my/folder", builder.Environment); // happy path

Ocelot needs the ``builder.Environment`` so it knows to exclude any environment-specific files from the merging algorithm, such as ``ocelot.{builder.Environment.EnvironmentName}.json``.

.. _config-merging-tomemory:

Merging files to memory [#f2]_
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

By default, Ocelot writes the merged configuration to disk as `ocelot.json`_ (the primary configuration file) by adding the file to the ASP.NET configuration provider.

If your web server lacks write permissions for the configuration folder, you can instruct Ocelot to use the merged configuration directly from memory.
Here's how:

.. code-block:: csharp
  :emphasize-lines: 5

    builder.Configuration
        .SetBasePath(builder.Environment.ContentRootPath)
        // It implicitly calls ASP.NET AddJsonStream extension method for IConfigurationBuilder
        // .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json)));
        .AddOcelot(builder.Environment, MergeOcelotJson.ToMemory);

This feature proves exceptionally valuable in cloud environments like Azure, AWS, and GCP, especially when the app lacks sufficient write permissions to save files.
Furthermore, within Docker container environments, permissions can be scarce, necessitating substantial DevOps efforts to enable file write operations.
Therefore, save time by leveraging this feature!

Reload On Change
----------------

Ocelot supports reloading the JSON configuration file on change.
For instance, the following will recreate Ocelot internal configuration when the `ocelot.json`_ file is updated manually:

.. code-block:: csharp
  :emphasize-lines: 3

    builder.Configuration
        .SetBasePath(builder.Environment.ContentRootPath)
        .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true) // ASP.NET framework version

.. _break: http://break.do

  **Note**: Starting from version `23.2`_, most :ref:`di-configuration-addocelot-methods` include optional ``bool?`` arguments, specifically ``optional`` and ``reloadOnChange``.
  Therefore, you have the flexibility to provide these arguments when invoking the native `AddJsonFile method <https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.jsonconfigurationextensions.addjsonfile?view=net-9.0-pp#microsoft-extensions-configuration-jsonconfigurationextensions-addjsonfile(microsoft-extensions-configuration-iconfigurationbuilder-system-string-system-boolean-system-boolean)>`_ during the final configuration step (see `AddOcelotJsonFile <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20AddOcelotJsonFile&type=code>`_ implementation).

We recommend using the :ref:`di-configuration-addocelot-methods` to control reloading, rather than relying on the framework's ``AddJsonFile`` method.
For example:

.. code-block:: csharp
  :emphasize-lines: 4,13-16

    // Old solution based on native framework functionality
    builder.Configuration
        .SetBasePath(builder.Environment.ContentRootPath)
        .AddJsonFile(ConfigurationBuilderExtensions.PrimaryConfigFile, optional: false, reloadOnChange: true);

    var config = builder.Configuration;
    var env = builder.Environment;
    var mergeTo = MergeOcelotJson.ToFile; // ToMemory
    var folder = "/My/folder";
    var configuration = new FileConfiguration(); // read from anywhere and initialize

    // Advanced solutions based on Ocelot functionality
    config.AddOcelot(env, mergeTo, optional: false, reloadOnChange: true); // with environment and merging type
    config.AddOcelot(folder, env, mergeTo, optional: false, reloadOnChange: true); // with folder, environment and merging type
    config.AddOcelot(configuration, optional: false, reloadOnChange: true); // with configuration object created by your own
    config.AddOcelot(configuration, env, mergeTo, optional: false, reloadOnChange: true); // with configuration object, environment and merging type

Examining the code within the ``ConfigurationBuilderExtensions`` class would be helpful for gaining a better understanding of the signatures of the overloaded :ref:`di-configuration-addocelot-methods`.

.. _config-react-to-changes:

React to Changes
----------------

Resolve ``IOcelotConfigurationChangeTokenSource`` interface from the DI container if you wish to react to changes to the Ocelot configuration via the :ref:`administration-api` or `ocelot.json`_ being reloaded from the disk.

You may either poll the change token's ``IChangeToken.HasChanged`` property, or register a callback with the ``RegisterChangeCallback`` method.

  **How to poll** is explained here:

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
                      _logger.LogInformation("Configuration has changed");
                  }
                  await Task.Delay(1000, stoppingToken);
              }
          }
      }

  **How to register a callback** is explained here:

  .. code-block:: csharp

      public sealed class MyConfigurationNotifying : IDisposable
      {
          private readonly IOcelotConfigurationChangeTokenSource _tokenSource;
          private readonly IDisposable _callbackHolder;

          public MyConfigurationNotifying(IOcelotConfigurationChangeTokenSource tokenSource)
          {
              _tokenSource = tokenSource;
              _callbackHolder = tokenSource.ChangeToken
                  .RegisterChangeCallback(_ => Console.WriteLine("Configuration has changed"), null);
          }

          public void Dispose() => _callbackHolder.Dispose();
      }

Store in `Consul`_
------------------

As a developer, if you have enabled :doc:`../features/servicediscovery` with `Consul`_ support in Ocelot, you may choose to manage your configuration saving to the *Consul* `KV store`_.

Beyond the traditional methods of storing configuration in a file vs folder (:ref:`config-merging-files`), or in-memory (:ref:`config-merging-tomemory`), you also have the alternative to utilize the `Consul`_ server's storage capabilities.

For further details on managing Ocelot configurations via a Consul instance, please consult the ":ref:`sd-consul-configuration-in-kv`" section.

.. _config-build-from-scratch:

Build From Scratch
------------------

  Class: `FileConfiguration <https://github.com/ThreeMammals/Ocelot/blob/develop/src/Ocelot/Configuration/File/FileConfiguration.cs>`_

Storing, reading, and writing static configurations may have limitations.
Therefore, for more flexible and advanced scenarios the ``FileConfiguration`` object can be built from scratch in C# code of Ocelot application startup.
Additionally after reading static configuration from various sources such as, remote file systems, remote storages or cloudages, you can rewrite options to the configuration.

Ocelot does not provide a fluent syntax to build configuration on fly as other products do.
However, it is possible to inject a ``FileConfiguration`` object during Ocelot startup using the :ref:`di-configuration-addocelot-methods` with a special parameter:

.. code-block:: csharp

    public static IConfigurationBuilder AddOcelot(this IConfigurationBuilder builder, FileConfiguration fileConfiguration, /* optional */);

The method above will deserialize the object to disk.
If you prefer to keep the configuration in memory, the following method includes the ``MergeOcelotJson`` parameter:

.. code-block:: csharp

    public static IConfigurationBuilder AddOcelot(this IConfigurationBuilder builder, FileConfiguration fileConfiguration, IWebHostEnvironment env, MergeOcelotJson mergeTo, /* optional */);

In summary, the final .NET 8+ solution should be written in `Program`_ using `top-level statements <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/program-structure/top-level-statements>`_:

.. code-block:: csharp
  :emphasize-lines: 8,13,14

    using Ocelot.Configuration.File;
    using Ocelot.DependencyInjection;
    using Ocelot.Middleware;

    var builder = WebApplication.CreateBuilder(args);

    // Build Ocelot's configuration object on the fly:
    var config = new FileConfiguration(); // create new or read static state from anywhere
    // ... initialize or rewrite props: add routes, global config, etc.

    builder.Configuration
        .SetBasePath(builder.Environment.ContentRootPath)
        .AddOcelot(config) // MergeOcelotJson.ToFile : writing config JSON back to disk
        .AddOcelot(config, builder.Environment, MergeOcelotJson.ToMemory); // merging to memory
    builder.Services
        .AddOcelot(builder.Configuration);

    var app = builder.Build();
    await app.UseOcelot();
    await app.RunAsync();

As a final step, you could add shutdown logic to save the complete configuration back to the storage, deserializing it to JSON format.

``HttpHandlerOptions`` 
----------------------

  | Class: `FileHttpHandlerOptions <https://github.com/ThreeMammals/Ocelot/blob/develop/src/Ocelot/Configuration/File/FileHttpHandlerOptions.cs>`_
  | MS Learn: `SocketsHttpHandler Class <https://learn.microsoft.com/en-us/dotnet/api/system.net.http.socketshttphandler>`_

This route configuration section allows for following HTTP redirects, for instance, via the boolean ``AllowAutoRedirect`` option.
These options can be set at the route or global level.

Use ``HttpHandlerOptions`` in a route configuration to set up `HttpMessageHandler <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20HttpMessageHandler&type=code>`_ behavior based on a ``SocketsHttpHandler`` instance:

.. code-block:: json

  "HttpHandlerOptions": {
    "AllowAutoRedirect": false,
    "MaxConnectionsPerServer": 2147483647, // max value
    "PooledConnectionLifetimeSeconds": null, // integer or null
    "UseCookieContainer": false,
    "UseProxy": false,
    "UseTracing": false
  }

.. list-table::
    :widths: 25 75
    :header-rows: 1

    * - *Option*
      - *Description*
    * - | ``AllowAutoRedirect``
        | default: ``false``
      - This value indicates whether the request should follow `Redirection messages <https://developer.mozilla.org/en-US/docs/Web/HTTP/Status#redirection_messages>`_ (HTTP 3xx status codes).
        Set it ``true`` if the request should automatically follow redirection responses from the downstream resource; otherwise ``false``.
    * - | ``MaxConnectionsPerServer``
        | default: ``2147483647``, maximum integer
      - This controls how many connections the internal ``HttpMessageInvoker`` will open to a single :ref:`hosting-gotchas-iis`/:ref:`hosting-gotchas-kestrel` server.
    * - | ``PooledConnectionLifetimeSeconds``
        | default: ``120`` seconds
      - This controls how long a connection can be in the pool to be considered reusable.
        Also refer to the **1st note** below!
    * - | ``UseCookieContainer``
        | default: ``false``
      - This indicates whether the handler uses the ``CookieContainer`` property to store server cookies and uses these cookies when sending requests.
        Also refer to the **2nd note** below!
    * - | ``UseProxy``
        | default: ``false``
      - Refer to MS Learn: `UseProxy Property <https://learn.microsoft.com/en-us/dotnet/api/system.net.http.socketshttphandler.useproxy>`_
    * - | ``UseTracing``
        | default: ``false``
      - This enables :doc:`../features/tracing` feature in Ocelot.
        Also refer to the **3rd note** below!

.. _break2: http://break.do

    **Note 1**: If the ``PooledConnectionLifetimeSeconds`` option is not defined, the default value is ``120`` seconds, which is hardcoded in the `HttpHandlerOptionsCreator <https://github.com/ThreeMammals/Ocelot/blob/develop/src/Ocelot/Configuration/Creator/HttpHandlerOptionsCreator.cs>`_ class as the ``DefaultPooledConnectionLifetimeSeconds`` constant.

    **Note 2**: If you use the ``CookieContainer``, Ocelot caches the ``HttpMessageInvoker`` for each downstream service.
    This means that all requests to that downstream service will share the same cookies. 
    Issue `274 <https://github.com/ThreeMammals/Ocelot/issues/274>`_ was created because a user noticed that the cookies were being shared.
    The Ocelot team tried to think of a nice way to handle this but we think it is impossible. 
    If you don't cache the clients, that means each request gets a new client and therefore a new cookie container.
    If you clear the cookies from the cached client container, you get race conditions due to inflight requests. 
    This would also mean that subsequent requests don't use the cookies from the previous response!
    All in all not a great situation.
    We would avoid setting ``UseCookieContainer`` to ``true`` unless you have a really really good reason.
    Just look at your response headers and forward the cookies back with your next request! 

    **Note 3**: ``UseTracing`` option adds a tracing ``DelegatingHandler`` (aka ``Ocelot.Requester.ITracingHandler``) after obtaining it from ``ITracingHandlerFactory``, encapsulating the ``Ocelot.Logging.ITracer`` service of DI-container.

.. _ssl-errors:

SSL Errors
----------

If you want to ignore SSL warnings (errors), set the following in your route configuration:

.. code-block:: json

    "DangerousAcceptAnyServerCertificateValidator": true

**We don't recommend doing this!**
The team suggests creating your own certificate and then getting it trusted by your local (or remote) machine, if you can.
For ``https`` scheme, this fake validator was requested by issue `309 <https://github.com/ThreeMammals/Ocelot/issues/309>`_.
For ``wss`` scheme, this fake validator was added by PR `1377 <https://github.com/ThreeMammals/Ocelot/pull/1377>`_. 

  **Note**: As a team, we do not consider it an ideal solution.
  On one hand, the community wants to have an option to work with self-signed certificates.
  But on the other hand, currently, source code scanners detect two serious security vulnerabilities because of this fake validator in version `20.0`_ and higher.
  The Ocelot team will rethink this unfortunate situation, and it is highly likely that this feature will at least be redesigned or removed completely.

For now, the SSL fake validator makes sense in local development environments when a route has ``https`` or ``wss`` schemes with self-signed certificates for those routes.
There are no other reasons to use the ``DangerousAcceptAnyServerCertificateValidator`` property at all!

As a team, we highly recommend following these instructions when developing your gateway app with Ocelot:

* **Local development environments**: Use this feature to avoid SSL errors for self-signed certificates in the case of ``https`` or ``wss`` schemes.
  We understand that some routes should have the downstream scheme exactly with SSL, because they are also in development and/or deployed using SSL protocols.
  However, we believe that, especially for local development, you can switch from ``https`` to ``http`` without any objection since the services are in development and there is no risk of data leakage.

* **Remote development environments**: Everything is the same as for local development.
  However, this case is less strict; you have more options to use real certificates to switch off the feature.
  For instance, you can deploy downstream services to cloud and hosting providers that have their own signed certificates for SSL.
  At least your team can deploy one remote web server to host downstream services. Install your own certificate or use the cloud provider's one.

* **Staging or testing environments**: We do not recommend using self-signed certificates because web servers should have valid certificates installed.
  Ask your system administrator or DevOps engineers to create valid certificates.

* **Production environments**: **Do not use self-signed certificates at all!**
  System administrators or DevOps engineers must create real valid certificates signed by hosting or cloud providers.
  **Switch off the feature for all routes!**
  Remove the ``DangerousAcceptAnyServerCertificateValidator`` property for all routes in the production version of the `ocelot.json`_ file!

.. _config-http-version:

``DownstreamHttpVersion``
-------------------------

  MS Learn: `HttpVersion Class <https://learn.microsoft.com/en-us/dotnet/api/system.net.httpversion>`_

Ocelot allows you to choose the HTTP version it will use to make the proxy request. It can be set as ``1.0``, ``1.1``, or ``2.0``.

.. _config-version-policy:

``DownstreamHttpVersionPolicy`` [#f3]_
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

  Enum: `HttpVersionPolicy <https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpversionpolicy>`_

This routing property enables the configuration of the ``VersionPolicy`` property within ``HttpRequestMessage`` objects for downstream HTTP requests.
For additional details, refer to the following documentation:

* `HttpRequestMessage.VersionPolicy Property <https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httprequestmessage.versionpolicy>`_
* `HttpVersionPolicy Enum <https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpversionpolicy>`_
* `HttpVersion Class <https://learn.microsoft.com/en-us/dotnet/api/system.net.httpversion>`_

The ``DownstreamHttpVersionPolicy`` option is intricately linked with the :ref:`config-http-version` setting.
Therefore, merely specifying ``DownstreamHttpVersion`` may sometimes be inadequate, particularly if your downstream services or Ocelot logs report HTTP connection errors such as ``PROTOCOL_ERROR``.
In these routes, selecting the precise ``DownstreamHttpVersionPolicy`` value is crucial for the ``HttpVersion`` policy to prevent such protocol errors.

HTTP2 version policy
^^^^^^^^^^^^^^^^^^^^

**Given** you aim to ensure a smooth HTTP/2 connection setup for the Ocelot app and downstream services with SSL enabled:

.. code-block:: json

  {
    "DownstreamScheme": "https",
    "DownstreamHttpVersion": "2.0",
    "DownstreamHttpVersionPolicy": "", // empty or not defined
    "DangerousAcceptAnyServerCertificateValidator": true
  }

**And** you configure global settings to use :ref:`hosting-gotchas-kestrel` with this snippet:

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
    "DownstreamHttpVersionPolicy": "RequestVersionOrHigher" // !
  }

Dependency Injection
--------------------

  Class: `ConfigurationBuilderExtensions`_

*Dependency Injection* for this *Configuration* feature in Ocelot is designed to extend and/or control the configuration of the Ocelot Core before the stage of building ASP.NET Core pipeline services.
The primary methods are :ref:`di-configuration-addocelot-methods` within the ``ConfigurationBuilderExtensions`` class, which offers several overloaded versions with corresponding signatures.
You can utilize these methods in the `Program`_.cs file of your gateway app to configure the Ocelot pipeline and services.

Find additional details in the dedicated :ref:`di-configuration-overview` section and in subsequent sections related to the :doc:`../features/dependencyinjection` chapter.

.. _config-route-metadata:

Extend with ``Metadata``
------------------------

  Feature: :doc:`../features/metadata` [#f4]_

The ``Metadata`` options can store any arbitrary data that users can access in middlewares, delegating handlers, etc.
By using the *metadata*, users can implement their own logic and extend the functionality of Ocelot.

The :doc:`../features/metadata` feature is designed to extend both the static :ref:`config-route-schema` and :ref:`config-dynamic-route-schema`.
Global *metadata* must be defined inside the ``MetadataOptions`` section.

The following example demonstrates practical usage of this feature:

.. code-block:: json
  :emphasize-lines: 10,21-22

  {
    "Routes": [
      {
        // other opts...
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
      // other opts...
      "MetadataOptions": {
        // other metadata opts...
        "Metadata": {
          "instance_name": "dc-1-54abcz",
          "my-extension/param1": "default-value"
        }
      }
    }
  }

.. _break3: http://break.do

  **Note**: Route *metadata* prevails over global *metadata* from the ``GlobalConfiguration`` section.
  Therefore, if the same key data are defined both at the route and global levels, the route *metadata* overrides the global ones.

Now, the route *metadata* can be accessed through the `DownstreamRoute <https://github.com/search?q=repo%3AThreeMammals%2FOcelot+DownstreamRoute%28%29&type=code>`_ object:

.. code-block:: csharp
  :emphasize-lines: 8,9

  using Ocelot.Metadata;

  public static class OcelotMiddlewares
  {
      public static Task PreAuthenticationMiddleware(HttpContext context, Func<Task> next)
      {
          var route = context.Items.DownstreamRoute();
          var param1 = route.GetMetadata<string>("my-extension/param1") ?? throw new ArgumentNullException("my-extension/param1");
          var param2 = route.GetMetadata<string>("other-extension/param2", "default-value");
          // Working with metadata...
          return next();
      }
  }

For comprehensive documentation, please refer to the :doc:`../features/metadata` chapter.

.. _config-timeout:

``Timeout``
-----------

[#f5]_ This feature is designed as part of the ``MessageInvokerPool``, which contains cached ``HttpMessageInvoker`` objects per route.
Each created ``HttpMessageInvoker`` encapsulates an ``HttpMessageHandler``, specifically a ``SocketsHttpHandler`` instance, which serves as the base handler for the request pipeline.
This pipeline also includes all user-defined :doc:`../features/delegatinghandlers`.
Finally, both the :doc:`../features/delegatinghandlers` and the base ``SocketsHttpHandler`` are wrapped by Ocelot's custom ``TimeoutDelegatingHandler``, which provides the internal timeout functionality.

  **Note**: This design is subject to future review because ``TimeoutDelegatingHandler`` overrides/mimics the default timeout properties of ``SocketsHttpHandler``, as well as the behavior of ``HttpMessageInvoker`` as a controller for ``HttpMessageHandler`` objects.

To configure timeouts (in seconds) at different levels, choose the appropriate level and provide the corresponding JSON configuration:

- **A route timeout** can be easily defined using the following JSON, according to the :ref:`config-route-schema`:

  .. code-block:: json

    {
      // upstream props
      // downstream props
      "Timeout": 3 // seconds
    }

  Please note that the route-level timeout takes precedence over the global timeout.
  The same configuration applies to *dynamic routes*, according to the :ref:`config-dynamic-route-schema`.

- **A global configuration timeout** can be defined using the following JSON, according to the :ref:`config-global-configuration-schema`:

  .. code-block:: json

    {
      // routes...
      "GlobalConfiguration": {
        // other props
        "Timeout": 60 // seconds, 1 minute
      }
    }

  Please note that the global timeout is substituted into a route if the route-level timeout is not defined, and it takes precedence over the absolute :ref:`config-default-timeout`.
  Additionally, the global timeout may be omitted in the JSON configuration in favor of the absolute :ref:`config-default-timeout`, which is also configurable via a property of the C# static class.

- **A** :doc:`../features/qualityofservice` **timeout** can be defined according to the QoS :ref:`qos-configuration-schema` and the QoS :ref:`qos-timeout-strategy`:

  .. code-block:: json

    "QoSOptions": {
      "TimeoutValue": 5000 // milliseconds
    }

  Please note, the *Quality of Service* timeout takes precedence over both route-level and global timeouts, which are ignored when QoS is enabled.
  Additionally, avoid defining both timeouts in the same route, as the QoS timeout (``TimeoutValue``) has higher priority than the route-level timeout.
  Therefore, the following route configuration is not recommended:

  .. code-block:: json

    {
      // route props...
      "Timeout": 3, // seconds
      "QoSOptions": {
        "TimeoutValue": 5000 // milliseconds
      }
    }

  So, ``Timeout`` will be ignored in favor of ``TimeoutValue``.
  Moreover, because the 3-second duration is shorter than 5000 milliseconds, you may observe warning messages in the logs that begin with the following sentence:

  .. code-block:: text

    Route '/xxx' has Quality of Service settings (QoSOptions) enabled, but either the route Timeout or the QoS TimeoutValue is misconfigured: ...

  For more details about this warning, refer to the QoS :ref:`qos-notes` (see Note 4).
  Your next recommended action is to completely remove the ``Timeout`` property.

.. _break4: http://break.do

  **Note 1**: Both ``Timeout`` and ``TimeoutValue`` are nullable positive integers, with a minimum valid value of ``1``.
  Values in the range ``(−∞, 0]`` are treated as "no value" and will be automatically converted to the absolute :ref:`config-default-timeout`, effectively ignoring the property.

  **Note 2**: The unit of measurement for ``Timeout`` is seconds, whereas ``TimeoutValue`` (used in QoS) is measured in milliseconds.

.. _config-default-timeout:

Default timeout
^^^^^^^^^^^^^^^
.. _DownstreamRoute.DefTimeout: https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20DownstreamRoute.DefTimeout&type=code
.. _DownstreamRoute.DefaultTimeoutSeconds: https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20DownstreamRoute.DefaultTimeoutSeconds&type=code

Timeout values defined at different levels in the JSON configuration can serve as fallback defaults for other levels.

- The absolute timeout (also known as ``DownstreamRoute.DefaultTimeoutSeconds``) defaults to 90 seconds (as defined by the ``DownstreamRoute.DefTimeout`` constant).
  It acts as the default timeout when neither route-level nor global timeouts are defined.
- The global configuration timeout, if not defined, also defaults to ``DownstreamRoute.DefaultTimeoutSeconds``.
  If defined, it serves as the default timeout for all routes.
- The Quality of Service (QoS) global timeout acts as the default timeout for all routes where QoS is enabled.

To configure the absolute timeout (currently 90 seconds, as defined by the `DownstreamRoute.DefTimeout`_ constant),
assign the desired number of seconds to the `DownstreamRoute.DefaultTimeoutSeconds`_ static property in your `Program`_ class:

.. code-block:: csharp

  DownstreamRoute.DefaultTimeoutSeconds = 3; // seconds, value must be >= 3

However, keep in mind that the absolute timeout has the lowest priority—therefore, route-level and global timeouts will override this C# property if they are defined.

""""

.. [#f1] The ":ref:`config-merging-files`" feature was requested in issue `296`_, since then we extended it in issue `1216`_ (PR `1227`_) as ":ref:`config-merging-tomemory`" subfeature which was released as a part of version `23.2`_.
.. [#f2] The ":ref:`config-merging-tomemory`" feature is based on the `MergeOcelotJson <https://github.com/ThreeMammals/Ocelot/blob/main/src/Ocelot/DependencyInjection/MergeOcelotJson.cs>`_ enumeration type with values: ``ToFile`` and ``ToMemory``. The 1st one is implicit by default, and the second one is exactly what you need when merging to memory. See more details on implementations in the `ConfigurationBuilderExtensions`_ class.
.. [#f3] The ":ref:`config-version-policy`" feature was requested in issue `1672`_ as a part of version `23.3`_.
.. [#f4] The ":ref:`config-route-metadata`" feature was requested in issues `738`_ and `1990`_, and it was released as part of version `23.3`_.
.. [#f5] The initial draft design of the :ref:`config-timeout` feature was implemented in pull request `1824`_ as ``TimeoutDelegatingHandler`` (released in version `23.0`_), but this version supported only the built-in `default timeout of 90 seconds`_.
  The full :ref:`config-timeout` feature was requested in issue `1314`_, implemented in pull request `2073`_, and officially released as part of version `24.1`_.

.. _default timeout of 90 seconds: https://github.com/ThreeMammals/Ocelot/blob/24.0.0/src/Ocelot/Requester/MessageInvokerPool.cs#L38
.. _296: https://github.com/ThreeMammals/Ocelot/issues/296
.. _738: https://github.com/ThreeMammals/Ocelot/issues/738
.. _1216: https://github.com/ThreeMammals/Ocelot/issues/1216
.. _1227: https://github.com/ThreeMammals/Ocelot/pull/1227
.. _1314: https://github.com/ThreeMammals/Ocelot/issues/1314
.. _1672: https://github.com/ThreeMammals/Ocelot/issues/1672
.. _1824: https://github.com/ThreeMammals/Ocelot/pull/1824
.. _1990: https://github.com/ThreeMammals/Ocelot/issues/1990
.. _2073: https://github.com/ThreeMammals/Ocelot/pull/2073

.. _20.0: https://github.com/ThreeMammals/Ocelot/releases/tag/20.0.0
.. _23.0: https://github.com/ThreeMammals/Ocelot/releases/tag/23.0.0
.. _23.2: https://github.com/ThreeMammals/Ocelot/releases/tag/23.2.0
.. _23.3: https://github.com/ThreeMammals/Ocelot/releases/tag/23.3.0
.. _24.1: https://github.com/ThreeMammals/Ocelot/releases/tag/24.1.0
