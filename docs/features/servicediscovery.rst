Service Discovery
=================

Ocelot allows you to specify a *service discovery* provider, which it uses to determine the host and port for the downstream service to which it forwards requests.
Currently, this feature is only supported in the ``GlobalConfiguration`` section.
This means the same *service discovery* provider is applied to all routes where a ``ServiceName`` is specified at the route level.

.. _sd-consul:

Consul
------

.. _Consul: https://www.consul.io/
.. _Ocelot.Provider.Consul: https://www.nuget.org/packages/Ocelot.Provider.Consul

  | Package: `Ocelot.Provider.Consul`_
  | Namespace: ``Ocelot.Provider.Consul``

The first step is to install `the package <https://www.nuget.org/packages/Ocelot.Provider.Consul>`_, which adds `Consul`_ support to Ocelot:

.. code-block:: powershell

    Install-Package Ocelot.Provider.Consul

To register *Consul* services, invoke the ``AddConsul()`` extension method using the ``OcelotBuilder`` returned by ``AddOcelot()`` [#f1]_.
Include the following code in your `Program`_:

.. code-block:: csharp

  using Ocelot.Provider.Consul;

  builder.Services
      .AddOcelot(builder.Configuration)
      .AddConsul(); // or .AddConsul<T>()

Currently, there are two types of *Consul* service discovery providers: ``Consul`` and ``PollConsul``.
The default provider is ``Consul``.
If the ``ConsulProviderFactory`` cannot read, understand, or parse the ``Type`` property of the ``ServiceProviderConfiguration`` object, a :ref:`sd-consul-provider` instance is created by the factory.

Explore these types of *service discovery* providers and learn about their differences in the subsections: :ref:`sd-consul-provider` and :ref:`sd-pollconsul-provider`.

  **Note**: We have made the :ref:`sd-consul-provider` the default *service discovery* provider in Ocelot.

.. _sd-consul-configuration-in-kv:

Configuration in `KV Store`_
^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Add the following when registering your services. Ocelot will attempt to store and retrieve its :doc:`../features/configuration` in the *Consul* `KV Store`_:

.. code-block:: csharp
  :emphasize-lines: 4

  builder.Services
      .AddOcelot(builder.Configuration)
      .AddConsul()
      .AddConfigStoredInConsul();

You also need to add the following to your `ocelot.json`_ file.
This allows Ocelot to locate your *Consul* agent and handle configuration loading and storage from *Consul*.

.. code-block:: json

  "GlobalConfiguration": {
    "ServiceDiscoveryProvider": {
      "Host": "localhost",
      "Port": 9500
    }
  }

The team decided to create this feature after working on the `Raft consensus <https://github.com/ThreeMammals/Ocelot.Provider.Rafty>`_ algorithm and realizing how challenging it was.
Why not take advantage of the fact that `Consul`_ already provides this functionality?
We believe this means that, to use Ocelot to its fullest potential, you currently need to adopt *Consul* as a dependency.

  **Note**: This feature has a `3-second TTL`_ cache before it makes a new request to your local *Consul* agent.

.. _sd-consul-configuration-key:

Configuration Key [#f2]_
^^^^^^^^^^^^^^^^^^^^^^^^

If you are using *Consul* for :doc:`../features/configuration` (or other providers in the future), you may want to assign keys to your configurations.
This allows you to manage multiple configurations.

In order to specify the key, you need to set the ``ConfigurationKey`` property in the ``ServiceDiscoveryProvider`` options of the configuration JSON file.
For example:

.. code-block:: json
  :emphasize-lines: 5

  "GlobalConfiguration": {
    "ServiceDiscoveryProvider": {
      "Host": "localhost",
      "Port": 9500,
      "ConfigurationKey": "Ocelot_A"
    }
  }

In this example, Ocelot will use ``Ocelot_A`` as the key for your configuration when looking it up in *Consul*.
If you do not set the ``ConfigurationKey``, Ocelot will default to using the string ``InternalConfiguration`` as the key.

.. _sd-consul-provider:

``Consul`` Provider
^^^^^^^^^^^^^^^^^^^

  Class: `Ocelot.Provider.Consul.Consul <https://github.com/search?q=repo%3AThreeMammals%2FOcelot+Consul&type=code>`_

The following is required in the ``GlobalConfiguration`` section.
The ``ServiceDiscoveryProvider`` property is mandatory.
If you do not specify a host and port, the default `Consul`_ values will be used.

  **Note**: The ``Scheme`` option defaults to HTTP. This was introduced in pull request `1154`_ and defaults to ``http`` to avoid introducing a breaking change.

.. code-block:: json
  :emphasize-lines: 5

  "ServiceDiscoveryProvider": {
    "Scheme": "https",
    "Host": "localhost",
    "Port": 8500,
    "Type": "Consul"
  }

In the future, we may add a feature that allows route-specific configuration.

To instruct Ocelot that a route should use the *service discovery* provider for its host and port, you need to specify the ``ServiceName`` and the load balancer you wish to use for downstream requests.
Currently, Ocelot supports the `RoundRobin <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20RoundRobin&type=code>`_ and `LeastConnection <https://github.com/search?q=repo%3AThreeMammals%2FOcelot+LeastConnection&type=code>`_ algorithms.
If no load balancer is specified, Ocelot will not perform load balancing for requests.

.. code-block:: json

  {
    "ServiceName": "product",
    "LoadBalancerOptions": {
      "Type": "LeastConnection"
    }
  }

When set up, Ocelot will look up the downstream host and port from the *service discovery* provider and balance requests across available services.

.. _sd-pollconsul-provider:

``PollConsul`` Provider
^^^^^^^^^^^^^^^^^^^^^^^

  Class: `Ocelot.Provider.Consul.PollConsul <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20PollConsul&type=code>`_

A lot of users have requested a feature where Ocelot *polls Consul* for the latest service information instead of doing so per request.
If you want Ocelot to *poll Consul* for the latest services, rather than relying on the default behavior (per request), you need to configure the following options:

.. code-block:: json
  :emphasize-lines: 4-5

  "ServiceDiscoveryProvider": {
    "Host": "localhost",
    "Port": 8500,
    "Type": "PollConsul",
    "PollingInterval": 100 // ms
  }

The polling interval, measured in milliseconds, specifies how frequently Ocelot calls `Consul`_ for service configuration updates.

  **Note**: There are trade-offs to consider.
  If you *poll Consul*, Ocelot may not detect if a service is down, depending on your polling interval.
  This could result in more errors compared to retrieving the latest services per request.
  The impact largely depends on the volatility of your services.
  For most users, this is unlikely to be a significant concern, and polling may offer a slight performance improvement over querying `Consul`_ per request (as a sidecar agent).
  However, if you are communicating with a remote `Consul`_ agent, polling provides a more noticeable performance improvement.

Service Definition
^^^^^^^^^^^^^^^^^^

Your services need to be added to Consul in a manner similar to the example below (C# style, but hopefully it makes sense).
The key point to note is to avoid including ``http`` or ``https`` in the ``Address`` field.
We have received feedback regarding issues with the scheme being included in the ``Address``.
After reviewing the "`Agents Overview <https://developer.hashicorp.com/consul/docs/agent>`_" and "`Define services <https://developer.hashicorp.com/consul/docs/services/usage/define-services>`_" documentation, we believe the **scheme** should not be included.

In C#

.. code-block:: csharp

    new AgentService()
    {
        ID = "some-id",
        Service = "some-service-name",
        Address = "localhost",
        Port = 8080,
    }

Or, in JSON

.. code-block:: json

  "Service": {
    "ID": "some-id",
    "Service": "some-service-name",
    "Address": "localhost",
    "Port": 8080
  }

ACL Token
^^^^^^^^^

If you are using `ACL <https://developer.hashicorp.com/consul/commands/acl/token>`_ with *Consul*, Ocelot supports adding the ``X-Consul-Token`` header.
To enable this functionality, you must add the following option:

.. code-block:: json
  :emphasize-lines: 5

  "ServiceDiscoveryProvider": {
    "Host": "localhost",
    "Port": 8500,
    "Type": "Consul",
    "Token": "my-token"
  }

Ocelot will add this token to the *Consul* client it uses for making requests, and this token will be applied to all subsequent requests.

.. _sd-consul-service-builder:

Consul Service Builder [#f3]_
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

  | Interface: ``IConsulServiceBuilder``
  | Implementation: ``DefaultConsulServiceBuilder``

The Ocelot community has consistently reported issues with *Consul* services, both in the past and present, such as connectivity problems due to varying *Consul* agent definitions.
Some DevOps engineers prefer grouping services as *Consul* `catalog nodes`_ by customizing the assignment of hostnames to node names, while others prioritize defining agent services using pure IP addresses as hosts, which is linked to the `954`_-bug dilemma.

Since version `13.5.2`_, the process for constructing the downstream host and port in pull request `909`_ has been changed to prioritize the node name as the host over the agent service address IP.
This may raise some criticism from the community.

Version `23.3`_ introduced a customization feature that enables control over the service-building process through the ``DefaultConsulServiceBuilder`` class.
This class includes virtual methods that developers and DevOps teams can override to suit their specific requirements.

The current logic in the ``DefaultConsulServiceBuilder`` class is as follows:

.. code-block:: csharp

  protected virtual string GetDownstreamHost(ServiceEntry entry, Node node)
      => node != null ? node.Name : entry.Service.Address;

Some DevOps engineers choose to disregard node names, opting for abstract identifiers instead of actual hostnames.
However, our team strongly recommends assigning real hostnames or IP addresses to node names, considering this a best practice.
If this approach does not align with your needs, or if you prefer not to invest time in detailing nodes for downstream services, you could define agent services without node names.
In such cases, within a *Consul* setup, you would need to override the behavior of the ``DefaultConsulServiceBuilder`` class.
For further information, refer to the ":ref:`sd-addconsul-generic-method`" section below.

.. _sd-addconsul-generic-method:

``AddConsul<T>`` method
"""""""""""""""""""""""

  Signature: ``IOcelotBuilder AddConsul<TServiceBuilder>(this IOcelotBuilder builder)``

Overriding the ``DefaultConsulServiceBuilder`` behavior involves two steps:
creating a new class that inherits from the ``IConsulServiceBuilder`` interface, and injecting this new behavior into the DI container using the ``AddConsul<TServiceBuilder>()`` helper.
However, the fastest and most streamlined approach is to inherit directly from the ``DefaultConsulServiceBuilder`` class, as it provides greater flexibility.

First, define a new service-building class:

.. code-block:: csharp

  using Ocelot.Logging;
  using Ocelot.Provider.Consul;
  using Ocelot.Provider.Consul.Interfaces;

  public class MyConsulServiceBuilder : DefaultConsulServiceBuilder
  {
      public MyConsulServiceBuilder(IHttpContextAccessor contextAccessor, IConsulClientFactory clientFactory, IOcelotLoggerFactory loggerFactory)
          : base(contextAccessor, clientFactory, loggerFactory) { }

      // Use the agent service IP address as the downstream hostname
      protected override string GetDownstreamHost(ServiceEntry entry, Node node)
          => entry.Service.Address;
  }

Next, inject the new behavior into the DI container, as shown in the Ocelot-Consul setup:

.. code-block:: csharp

  builder.Services
      .AddOcelot(builder.Configuration)
      .AddConsul<MyConsulServiceBuilder>();

Refer to the repository's `acceptance test`_ for further examples.

.. _sd-eureka:

Eureka [#f4]_
-------------

.. _Steeltoe: https://steeltoe.io
.. _Pivotal: https://pivotal.io/platform
.. _Eureka: https://www.nuget.org/packages/Steeltoe.Discovery.Eureka
.. _Ocelot.Provider.Eureka: https://www.nuget.org/packages/Ocelot.Provider.Eureka

  | Package: `Ocelot.Provider.Eureka`_
  | Namespace: ``Ocelot.Provider.Eureka``

This feature supports the Netflix `Eureka`_ *service discovery* provider.
The primary reason for this is that it is a key product of `Steeltoe`_, which is associated with `Pivotal`_.
Now, enough of the background!

The first step is to install `the package <https://www.nuget.org/packages/Ocelot.Provider.Eureka>`__ that provides `Eureka`_ support for Ocelot:

.. code-block:: powershell

    Install-Package Ocelot.Provider.Eureka

Next, add the following to your `Program <https://github.com/ThreeMammals/Ocelot/blob/main/samples/Eureka/ApiGateway/Program.cs>`__:

.. code-block:: csharp

  using Ocelot.Provider.Eureka;

  builder.Services
      .AddOcelot(builder.Configuration)
      .AddEureka();

Finally, to enable this setup, include the following in your `ocelot.json <https://github.com/ThreeMammals/Ocelot/blob/main/samples/Eureka/ApiGateway/ocelot.json>`__ file:

.. code-block:: json

  "ServiceDiscoveryProvider": {
    "Type": "Eureka"
  }

Following the guide `here <https://docs.steeltoe.io/>`_, you may also need to add some configurations to `appsettings.json <https://github.com/ThreeMammals/Ocelot/blob/main/samples/Eureka/ApiGateway/appsettings.json>`_.
For example, the JSON below informs the `Steeltoe`_ / `Pivotal`_ services where to locate the service discovery server and whether the service should register with it:

.. code-block:: json

  "eureka": {
    "client": {
      "serviceUrl": "http://localhost:8761/eureka/",
      "shouldRegisterWithEureka": false,
      "shouldFetchRegistry": true
    }
  }

If ``shouldRegisterWithEureka`` is set to ``false``, ``shouldFetchRegistry`` will default to ``true``, so you do not need to set it explicitly; however, it has been included here for clarity.

Ocelot will now register all necessary services during startup and, if the JSON above is provided, it will register itself with *Eureka*.
One of the services polls *Eureka* every 30 seconds (default) to retrieve the latest service state and persists this information in memory.
When Ocelot requests a given service, it retrieves the data from memory, minimizing performance issues.

If not explicitly specified in `ocelot.json <https://github.com/ThreeMammals/Ocelot/blob/main/samples/Eureka/ApiGateway/ocelot.json>`__, Ocelot will use the scheme (``http``, ``https``) set in *Eureka*.

.. _sd-service-fabric:

Service Fabric
--------------

.. _Service Fabric: https://azure.microsoft.com/en-us/products/service-fabric/
.. _Microsoft.ServiceFabric: https://www.nuget.org/packages/Microsoft.ServiceFabric

If you have services deployed in Azure `Service Fabric`_, you typically use the naming service to access them.

Please refer to the :doc:`../features/servicefabric` chapter for the complete *essential* documentation.

  **Note**: Currently, the ``ServiceFabric`` *service discovery* provider is tightly coupled with Ocelot core interfaces, making it a part of Ocelot Core and implemented as the ``ServiceFabricServiceDiscoveryProvider`` class.
  At present, there is no Ocelot extension package that integrates with the `Microsoft.ServiceFabric`_ package or any other relevant package.
  However, the Ocelot team plans to address this in future development, as we believe `Service Fabric`_ is an essential and popular product in the .NET and Azure development world.
  If anyone in the Ocelot community is a professional Azure developer with extensive `Service Fabric`_ experience, please contact our development team directly via GitHub or email.

.. _sd-dynamic-routing:

Dynamic Routing [#f5]_
----------------------

The idea is to enable *dynamic routing* mode when using a *service discovery* provider.
In this mode, Ocelot uses the first segment of the upstream path to look up the downstream service via the *service discovery* provider.

An example of this would be calling Ocelot with a URL like

* ``https://api.ocelot.net/product/products``

Ocelot will take the first segment of the path, which is ``product``, and use it as a key to look up the service in :ref:`sd-consul`.
If :ref:`sd-consul-provider` returns a service, Ocelot will request it using the host and port provided by `Consul`_, appending the remaining path segments—in this case, ``products``—to construct final downstream URL:

* ``http://hostfromconsul:portfromconsul/products``

Ocelot will append any query string to the downstream URL as usual.

.. note::
  To enable *dynamic routing*, the `ocelot.json`_ configuration must contain no static routes in the ``Routes`` collection!
  Currently, dynamic routes and static routes cannot be mixed.
  Additionally, you need to specify the details of the *service discovery* provider as outlined above, along with the downstream ``http(s)`` scheme under ``DownstreamScheme``.

  In addition to the global ``ServiceDiscoveryProvider`` section, the :ref:`config-global-configuration-schema` includes configurable options such as ``CacheOptions``, ``RateLimitOptions``, ``QoSOptions``, ``LoadBalancerOptions``, ``HttpHandlerOptions``, and ``DownstreamScheme``.
  These options are applicable to all dynamic routes, globally.
  However, since the :ref:`config-dynamic-route-schema` does not support these options (except for ``LoadBalancerOptions`` and ``RateLimitOptions``), they are not applied in *dynamic routing* mode.
  Therefore, it is not possible to override global options using dynamic route-level settings.
  To reiterate, the only options fully supported by both static and dynamic routes are ``LoadBalancerOptions`` and ``RateLimitOptions``.

For instance, when exposing Ocelot publicly over HTTPS while routing to internal services over HTTP, your configuration may resemble the following:

  .. code-block:: json

    {
      "Routes": [], // must be empty to enable dynamic routing!
      "DynamicRoutes": [
        // overriding goes here
      ],
      "GlobalConfiguration": {
        "BaseUrl": "https://api.ocelot.net",
        "DownstreamScheme": "http", // default scheme for all internal services, no SSL
        "ServiceDiscoveryProvider": {
          "Host": "localhost", // if Consul is hosted on the same machine as Ocelot
          "Port": 8500,
          "Type": "Consul",
          "Namespace": "" // not supported for Consul, but supported for Kubernetes
        },
        "RateLimitOptions": {
          "ClientIdHeader": "Oc-DynamicRouting-Client",
          "QuotaMessage": "No Quota!",
          "StatusCode": 499 // special shared status
        },
        "QoSOptions": {
          "ExceptionsAllowedBeforeBreaking": 2,
          "DurationOfBreak": 333,
          "TimeoutValue": 3000 // ms
        },
        "LoadBalancerOptions": {
          "Type": "LeastConnection"
        },
        "HttpHandlerOptions": {
          "AllowAutoRedirect": false,
          "UseCookieContainer": false,
          "UseTracing": false
        }
      }
    }

.. _sd-dynamic-routing-configuration:

Configuration
^^^^^^^^^^^^^

Ocelot also allows configuration of a ``DynamicRoutes`` collection consisting of :ref:`config-dynamic-route-schema` objects.
This enables overriding ``RateLimitOptions`` for each downstream service, along with other schema-level overrides.
Dynamic route options are particularly useful when there are multiple services—such as a 'product' service and a 'search' service—and stricter rate limits need to be applied to one over the other.
The final configuration looks like:

  .. code-block:: json

    {
      "DynamicRoutes": [
        {
          "ServiceName": "product",
          "ServiceNamespace": "", // not supported for Consul, but supported for Kubernetes
          "RateLimitOptions": {
            "Limit": 5,
            "Period": "1s",
            "Wait": "1.5s" // hybrid fixed window
          }
        },
        {
          "ServiceName": "notification",
          "RateLimitOptions": {
            "EnableRateLimiting": false // notification service is unlimited!
          },
          "LoadBalancerOptions": {
            "Type": "LeastConnection" // switch from RoundRobin to LeastConnection
          }
        }
      ],
      "GlobalConfiguration": {
        "BaseUrl": "https://api.ocelot.net",
        "DownstreamScheme": "http",
        "ServiceDiscoveryProvider": {
          "Host": "localhost",
          "Port": 8500,
          "Type": "Consul",
          "Namespace": "" // not supported for Consul, but supported for Kubernetes
        },
        "RateLimitOptions": {
          "ClientIdHeader": "Oc-DynamicRouting-Client",
          "ClientWhitelist": ["ocelot-client1-preshared-key"],
          "Limit": 5,
          "Period": "10s", // fixed window
          "QuotaExceededMessage": "No Quota!",
          "HttpStatusCode": 499 // special shared status
        },
        "LoadBalancerOptions": {
          "Type": "RoundRobin"
        }
      }
    }

This configuration means that when a request is sent to Ocelot at ``/product/*``, *dynamic routing* is activated, and Ocelot applies the rate limiting rules defined for the 'product' service in the ``DynamicRoutes`` section, as described in the :doc:`../features/ratelimiting` documentation.
The 'notification' service is unlimited because rate limiting is disabled. All other services use the global ``RateLimitOptions``.

.. warning::
  Dynamic route ``RateLimitRule`` option is deprecated!

  The `old schema <https://github.com/ThreeMammals/Ocelot/blob/24.0.0/src/Ocelot/Configuration/File/FileDynamicRoute.cs>`_ ``RateLimitRule`` section is deprecated in version `24.1`_!
  Use ``RateLimitOptions`` instead of ``RateLimitRule``! Note that ``RateLimitRule`` will be removed in version `25.0`_!
  For backward compatibility in version `24.1`_, the ``RateLimitRule`` section takes precedence over the ``RateLimitOptions`` section.

.. _break: http://break.do

  **Note**: The ``ServiceNamespace`` option was introduced in version `24.1`_ to enable precise overrides for the :doc:`../features/kubernetes` providers.
  If ``ServiceNamespace`` is left empty or undefined, only one dynamic route with the same ``ServiceName`` may be defined in the ``DynamicRoutes`` collection.

.. _sd-custom-providers:

Custom Providers
----------------

Ocelot also enables you to create a custom *Service Discovery* implementation by implementing the ``IServiceDiscoveryProvider`` interface, as demonstrated in the following example:

.. code-block:: csharp

  public class MyServiceDiscoveryProvider : IServiceDiscoveryProvider
  {
      private readonly IServiceProvider _serviceProvider;
      private readonly ServiceProviderConfiguration _config;
      private readonly DownstreamRoute _downstreamRoute;

      public MyServiceDiscoveryProvider(IServiceProvider serviceProvider, ServiceProviderConfiguration config, DownstreamRoute downstreamRoute)
      {
          _serviceProvider = serviceProvider;
          _config = config;
          _downstreamRoute = downstreamRoute;
      }

      public Task<List<Service>> GetAsync()
      {
          var services = new List<Service>();
          // ...
          // Add service(s) to the list matching the _downstreamRoute
          return services;
      }
  }

And set its class name as the provider type in `ocelot.json`_:

.. code-block:: json

  "GlobalConfiguration": {
    "ServiceDiscoveryProvider": {
      "Type": "MyServiceDiscoveryProvider"
    }
  }
  
Finally, in the `Program`_, register a ``ServiceDiscoveryFinderDelegate`` to initialize and return the provider:

.. code-block:: csharp

  ServiceDiscoveryFinderDelegate serviceDiscoveryFinder = (provider, config, route)
      => new MyServiceDiscoveryProvider(provider, config, route);
  builder.Services
      .AddSingleton(serviceDiscoveryFinder)
      .AddOcelot(builder.Configuration);

.. _sd-sample:

Sample
------

To offer a basic template for a :ref:`sd-custom-providers`, we have created a sample:

  | Project: `samples <https://github.com/ThreeMammals/Ocelot/tree/main/samples>`_ / `ServiceDiscovery <https://github.com/ThreeMammals/Ocelot/tree/main/samples/ServiceDiscovery>`_
  | Solution: `Ocelot.Samples.ServiceDiscovery.sln <https://github.com/ThreeMammals/Ocelot/blob/main/samples/ServiceDiscovery/Ocelot.Samples.ServiceDiscovery.sln>`_

This solution includes the following projects:

- :ref:`sd-api-gateway`
- :ref:`sd-downstream-service`

The solution is ready for deployment. All services are fully configured, with ports and hosts prepared for immediate use (when running in Visual Studio).
Complete instructions for running this solution can be found in the `README.md <https://github.com/ThreeMammals/Ocelot/blob/main/samples/ServiceDiscovery/README.md>`_ file.

.. _sd-downstream-service:

DownstreamService
^^^^^^^^^^^^^^^^^

This project provides a single downstream service that can be reused across :ref:`sd-api-gateway` routes.
It includes multiple ``launchSettings.json`` profiles to support your preferred launch and hosting scenarios, such as Visual Studio sessions, Kestrel console hosting, and Docker deployments.

.. _sd-api-gateway:

ApiGateway
^^^^^^^^^^

This project includes a custom *Service Discovery* provider and contains only route(s) to :ref:`sd-downstream-service` services in the `ocelot.json`_ file.
You are free to add more routes!

The main source code for the custom provider is located in the `ServiceDiscovery <https://github.com/ThreeMammals/Ocelot/tree/main/samples/ServiceDiscovery/ApiGateway/ServiceDiscovery>`__ folder, specifically in the ``MyServiceDiscoveryProvider`` and ``MyServiceDiscoveryProviderFactory`` classes.
Feel free to design and develop these classes to suit your needs!

Additionally, the cornerstone of this custom provider is the `Program`_ code, where you can select from simple or more complex design and implementation options:

  .. code-block:: csharp

    // Perform initialization from application configuration or hardcode/choose the best option.
    bool easyWay = true;
    if (easyWay)
    {
        // Design #1: Define a custom finder delegate to instantiate a custom provider 
        // under the default factory (ServiceDiscoveryProviderFactory).
        builder.Services
            .AddSingleton<ServiceDiscoveryFinderDelegate>((serviceProvider, config, downstreamRoute)
                => new MyServiceDiscoveryProvider(serviceProvider, config, downstreamRoute));
    }
    else
    {
        // Design #2: Abstract from the default factory (ServiceDiscoveryProviderFactory) and FinderDelegate,
        // and create your own factory by implementing the IServiceDiscoveryProviderFactory interface.
        builder.Services
            .RemoveAll<IServiceDiscoveryProviderFactory>()
            .AddSingleton<IServiceDiscoveryProviderFactory, MyServiceDiscoveryProviderFactory>();

        // This will not be called but is required for internal validators. It's also a handy workaround.
        builder.Services
            .AddSingleton<ServiceDiscoveryFinderDelegate>((serviceProvider, config, downstreamRoute) => null);
    }
    builder.Services
        .AddOcelot(builder.Configuration);

The "easy way" (lite design #1) involves designing only the provider class and specifying the ``ServiceDiscoveryFinderDelegate`` object for the default ``ServiceDiscoveryProviderFactory`` in the Ocelot core.

A more complex design #2 involves developing both the provider and provider factory classes.
Once this is done, you need to add the ``IServiceDiscoveryProviderFactory`` interface to the DI container and remove the default ``ServiceDiscoveryProviderFactory`` class.
Note that in this case, the Ocelot core will not use the ``ServiceDiscoveryProviderFactory`` by default.
Additionally, you do not need to specify ``"Type": "MyServiceDiscoveryProvider"`` in the ``ServiceDiscoveryProvider`` global options.
However, you can retain this ``Type`` option to maintain compatibility between both designs.

""""

.. [#f1] The :ref:`di-services-addocelot-method` adds default ASP.NET services to the DI container. You can call another extended :ref:`di-addocelotusingbuilder-method` while configuring services to develop your own :ref:`di-custom-builder`. See more instructions in the ":ref:`di-addocelotusingbuilder-method`" section of the :doc:`../features/dependencyinjection` feature.
.. [#f2] The ":ref:`sd-consul-configuration-key`" feature was requested in issue `346`_ and introduced in version `7.0.0`_.
.. [#f3] The customization of ":ref:`sd-consul-service-builder`" was implemented as part of bug fix `954`_, and the feature was delivered in version `23.3`_.
.. [#f4] The :ref:`sd-eureka` feature, requested in issue `262`_ to add support for the Netflix `Eureka`_ *service discovery* provider, was released in version `5.5.4`_.
.. [#f5] The ":ref:`Dynamic Routing <sd-dynamic-routing>`" feature was requested in issue `340`_ (pull request `351`_) and released in version `7.0.1`_.
  Later, the new ``DynamicRoutes`` :doc:`../features/configuration` section was introduced in pull request `508`_ and released in version `8.0.4`_.

.. _ocelot.json: https://github.com/ThreeMammals/Ocelot/blob/main/samples/ServiceDiscovery/ApiGateway/ocelot.json
.. _Program: https://github.com/ThreeMammals/Ocelot/blob/main/samples/ServiceDiscovery/ApiGateway/Program.cs
.. _KV Store: https://developer.hashicorp.com/consul/docs/dynamic-app-config/kv
.. _3-second TTL: https://github.com/search?q=repo%3AThreeMammals%2FOcelot+TimeSpan.FromSeconds%283%29&type=code
.. _catalog nodes: https://developer.hashicorp.com/consul/api-docs/catalog#list-nodes
.. _acceptance test: https://github.com/search?q=repo%3AThreeMammals%2FOcelot+ShouldReturnServiceAddressByOverriddenServiceBuilderWhenThereIsANode+WithConsulServiceBuilder&type=code

.. _262: https://github.com/ThreeMammals/Ocelot/issues/262
.. _340: https://github.com/ThreeMammals/Ocelot/issues/340
.. _346: https://github.com/ThreeMammals/Ocelot/issues/346
.. _351: https://github.com/ThreeMammals/Ocelot/pull/351
.. _508: https://github.com/ThreeMammals/Ocelot/pull/508
.. _909: https://github.com/ThreeMammals/Ocelot/pull/909
.. _954: https://github.com/ThreeMammals/Ocelot/issues/954
.. _1154: https://github.com/ThreeMammals/Ocelot/pull/1154

.. _5.5.4: https://github.com/ThreeMammals/Ocelot/releases/tag/5.5.4
.. _7.0.0: https://github.com/ThreeMammals/Ocelot/releases/tag/7.0.0
.. _7.0.1: https://github.com/ThreeMammals/Ocelot/releases/tag/7.0.1
.. _8.0.4: https://github.com/ThreeMammals/Ocelot/releases/tag/8.0.4
.. _13.5.2: https://github.com/ThreeMammals/Ocelot/releases/tag/13.5.2
.. _23.3: https://github.com/ThreeMammals/Ocelot/releases/tag/23.3.0
.. _24.1: https://github.com/ThreeMammals/Ocelot/releases/tag/24.1.0
.. _25.0: https://github.com/ThreeMammals/Ocelot/milestone/12
