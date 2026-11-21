Dynamic Routing
===============
.. contents:: Table of Contents
   :depth: 2
   :local:

.. _dynamic-routing:

Dynamic Routing [#f1]_
----------------------

The idea is to enable *dynamic routing* mode when using a *service discovery* provider.
In this mode, Ocelot uses the first segment of the upstream path to look up the downstream service via the *service discovery* provider.

An example of this would be calling Ocelot with a URL like

* ``https://api.ocelot.net/product/products``

Ocelot will take the first segment of the path, which is ``product``, and use it as a key to look up the service in :ref:`sd-consul`.
If :ref:`sd-consul-provider` returns a service, Ocelot will request it using the host and port provided by `Consul`_, appending the remaining path segments—in this case, ``products``—to construct final downstream URL:

* ``http://hostfromconsul:portfromconsul/products``

Ocelot will append any query string to the downstream URL as usual.

Dynamic Route Schema
--------------------

The :ref:`config-global-configuration-schema` contains the global options for dynamic routes.
Each service can additionally override these options through a ``DynamicRoutes`` collection.
Here is the complete dynamic route configuration, also known as the *"dynamic route schema,"* of top-level properties.

.. _FileDynamicRoute: https://github.com/ThreeMammals/Ocelot/blob/develop/src/Configuration/File/FileDynamicRoute.cs

    Class: `FileDynamicRoute`_

.. code-block:: json

    {
      "AuthenticationOptions": {},
      "CacheOptions": {},
      "DownstreamHttpVersion": "",
      "DownstreamHttpVersionPolicy": "",
      "HttpHandlerOptions": {},
      "LoadBalancerOptions": {},
      "Metadata": {}, // dictionary
      "QoSOptions": {},
      "RateLimitRule": {}, // deprecated! -> use RateLimitOptions
      "RateLimitOptions": {},
      "ServiceName": "",
      "ServiceNamespace": "",
      "Timeout": 0 // nullable integer
    }

The actual dynamic route schema with all the properties can be found in the C# `FileDynamicRoute`_ class.

  **Note 1**: The `old dynamic route schema <https://github.com/ThreeMammals/Ocelot/blob/24.1.0/src/Configuration/File/FileDynamicRoute.cs#L8-L9>`_ ``RateLimitRule`` section is deprecated in version `24.1`_!
  Use ``RateLimitOptions`` instead of ``RateLimitRule``! Note that ``RateLimitRule`` will be removed in version `25.0`_!
  For backward compatibility in version `24.1`_, the ``RateLimitRule`` section takes precedence over the ``RateLimitOptions`` section.

  **Note 2**: The following options were not supported in versions prior to `24.1`_ for overriding globally configured options: ``AuthenticationOptions``, ``CacheOptions``, ``HttpHandlerOptions``, ``LoadBalancerOptions``, ``QoSOptions``, ``RateLimitOptions``, ``ServiceNamespace``, and ``Timeout``.

Route Provider Design
---------------------

The :ref:`DiscoveryDownstreamRouteFinder <dynamic-routing-route-finder>` creates each dynamic route from the :ref:`config-dynamic-route-schema` described above.
It is the special ``IDownstreamRouteProvider`` for dynamic route creation and uses the full service name, including its namespace when present, as the load-balancing key.

The injected ``IDynamicRouting`` router service produces the dynamic routing artifacts used to create the route:

* service namespace
* service name
* downstream path

This separation allows applications to change how service names and downstream paths are detected without replacing the route provider.

.. _dynamic-routing-route-finder:

``DiscoveryDownstreamRouteFinder``
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

The route provider injects ``IDynamicRouting``, asks it for the service namespace, service name, and downstream path, then creates a dynamic route from the global configuration and any matching dynamic route schema overrides.

URL Path Router
---------------

The default dynamic routing type is the *URL path router*. It detects the service name from the first segment of the upstream URL path.
For example, ``/product/products`` resolves ``product`` as the service name and forwards ``/products`` as the downstream path.
The first segment may also contain a namespace separated from the service name by a dot, such as ``/namespace.product/products``.

The default implementation is `UrlPathDynamicRouting`_, which implements ``IDynamicRouting``.
The :ref:`dynamic-routing-route-finder` uses this service to obtain the service name and the downstream path while creating dynamic routes.
It is registered in the dependency injection container by ``AddOcelotServiceDiscovery()``, which is called by :doc:`../features/dependencyinjection` when configuring Ocelot.

Applications can replace the default implementation with the ``ServiceCollectionExtensions`` or ``IOcelotBuilder`` APIs:

.. code-block:: csharp

    services.AddOcelotDynamicRouting<MyUrlPathDynamicRouting>();

    builder.Services
        .AddOcelot(builder.Configuration)
        .AddDynamicRouting<MyUrlPathDynamicRouting>();

To create a custom URL path router, inherit from `UrlPathDynamicRouting`_ and override the methods that determine the downstream path and service name.
The acceptance tests contain `TwoSegmentsUrlPathDynamicRouting`_ as an example that combines two URL path segments into one service name.
The following is only the customization shape; the routing implementation is application-specific:

.. code-block:: csharp

    public class MyUrlPathDynamicRouting(IHttpContextAccessor contextAccessor)
        : UrlPathDynamicRouting(contextAccessor)
    {
        protected override string DownstreamPath(string upstreamUrlPath)
        {
            // Return the path after the custom service-name segments.
        }

        public override ServiceInfo GetServiceName(string upstreamUrlPath)
        {
            // Return the service name and optional namespace.
        }
    }

Multiple Segment URL Path Router
--------------------------------

The *multiple segment URL path router* is a custom URL path dynamic routing type that detects a service name from multiple segments at the beginning of the upstream path.
For example, ``/authorization/api$/path`` resolves ``authorization/api`` as the service name and forwards ``/path`` downstream.

The built-in `MultipleSegmentUrlPathDynamicRouting`_ class extends `UrlPathDynamicRouting`.
It is therefore still used by ``DiscoveryDownstreamRouteFinder`` through the ``IDynamicRouting`` abstraction, while overriding service-name and downstream-path detection for multiple segments.
It is registered in the same way as other custom dynamic routers:

.. code-block:: csharp

    services.AddOcelotDynamicRouting<MultipleSegmentUrlPathDynamicRouting>();

    builder.Services
        .AddOcelot(builder.Configuration)
        .AddDynamicRouting<MultipleSegmentUrlPathDynamicRouting>();

To create a custom multiple-segment URL path router, inherit from `MultipleSegmentUrlPathDynamicRouting`_ and override the methods that define the service-name delimiter or service-name mapping.
The following shows the customization shape without prescribing the implementation:

.. code-block:: csharp

    public class MyMultipleSegmentRouting(IHttpContextAccessor contextAccessor)
        : MultipleSegmentUrlPathDynamicRouting(contextAccessor)
    {
        protected override char ServiceNameDelimiter => ':';

        public override ServiceInfo GetServiceName(string upstreamUrlPath)
        {
            // Parse the custom service-name segment sequence.
        }

        protected override string DownstreamPath(string upstreamUrlPath)
        {
            // Return the path after the custom service-name segment sequence.
        }
    }

The default delimiter is ``$``. It can be configured through `MultipleSegmentUrlPathDynamicRoutingOptions`_:

.. code-block:: csharp

    services.AddOcelotDynamicRouting<MultipleSegmentUrlPathDynamicRouting>(
        options => options.Delimiter = ':');

    builder.Services
        .AddOcelot(builder.Configuration)
        .AddDynamicRouting<MultipleSegmentUrlPathDynamicRouting>(
            options => options.Delimiter = ':');

Choose a delimiter that is not alphanumeric and is not ``/``.
When the delimiter is a reserved URL-path character, encode it in the request URL according to URL standards.
For example, a ``#`` delimiter should be sent as ``%23`` so that it remains part of the path instead of becoming a fragment marker.

.. _MultipleSegmentUrlPathDynamicRoutingOptions: https://github.com/ThreeMammals/Ocelot/blob/develop/src/ServiceDiscovery/MultipleSegmentUrlPathDynamicRouting.cs

Roadmap
-------

The first-segment URL path router has been available for many years.
Multiple-segment URL path routing is available starting with version `26.0`_ [#f3]_.

The broader goal of dynamic routing is to allow the service name to be detected from service discovery artifacts, rather than only from the URL path.
The community may benefit from additional dynamic router types such as:

* Query-string parameter routers
* Header routers
* Host-name routers

These possibilities are tracked in issue `1188`_, including the discussion about routing from Consul tags and other service discovery artifacts in this `issue comment`_.

.. warning::

  To enable *dynamic routing*, the `ocelot.json`_ configuration must contain no static routes in the ``Routes`` collection!
  Currently, dynamic routes and static routes cannot be mixed.
  Additionally, you need to specify the details of the *service discovery* provider as outlined above, along with the downstream ``http(s)`` scheme under ``DownstreamScheme``.

.. note::

  In addition to the global ``ServiceDiscoveryProvider`` section, the :ref:`config-global-configuration-schema` includes configurable options such as ``DownstreamScheme``, ``CacheOptions``, ``HttpHandlerOptions``, ``LoadBalancerOptions``, ``QoSOptions``, ``RateLimitOptions``, and ``Timeout``.
  These options are applicable to all dynamic routes, globally.
  Moreover, starting with version `24.1`_, the :ref:`config-dynamic-route-schema` also supports these options for overriding global settings.

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
        "CacheOptions": {
          "TtlSeconds": 300 // 5 minutes
        },
        "HttpHandlerOptions": {
          "AllowAutoRedirect": false,
          "UseCookieContainer": false,
          "UseTracing": false
        },
        "LoadBalancerOptions": {
          "Type": "LeastConnection"
        },
        "QoSOptions": {
          "MinimumThroughput": 2,
          "BreakDuration": 333,
          "Timeout": 3000 // ms
        },
        "RateLimitOptions": {
          "ClientIdHeader": "Oc-DynamicRouting-Client",
          "QuotaMessage": "No Quota!",
          "StatusCode": 499 // special shared status
        }
      }
    }

.. _dynamic-routing-configuration:

Configuration [#f2]_
^^^^^^^^^^^^^^^^^^^^

Ocelot also allows configuration of a ``DynamicRoutes`` collection consisting of :ref:`config-dynamic-route-schema` objects.
This enables overriding ``RateLimitOptions`` for each downstream service, along with other schema-level overrides.
Dynamic route options are particularly useful when there are multiple services—such as a '``product``' service and a '``search``' service—and stricter rate limits need to be applied to one over the other.
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
          "CacheOptions": {
            "TtlSeconds": 0 // disable cache for notifying
          },
          "HttpHandlerOptions": {
            "UseTracing": false // disable tracing
          },
          "LoadBalancerOptions": {
            "Type": "LeastConnection" // switch from RoundRobin to LeastConnection
          },
          "RateLimitOptions": {
            "EnableRateLimiting": false // notification service is unlimited!
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
        "CacheOptions": {
          "TtlSeconds": 300 // 5 minutes
        },
        "HttpHandlerOptions": {
          "PooledConnectionLifetimeSeconds": 600, // change the default value from 2 minutes to 10 minutes
          "UseTracing": true // enable tracing globally
        },
        "LoadBalancerOptions": {
          "Type": "RoundRobin"
        },
        "RateLimitOptions": {
          "ClientIdHeader": "Oc-DynamicRouting-Client",
          "ClientWhitelist": ["ocelot-client1-preshared-key"],
          "Limit": 5,
          "Period": "10s", // fixed window
          "QuotaMessage": "No Quota!",
          "StatusCode": 499 // special shared status
        }
      }
    }

This configuration means that when a request is sent to Ocelot at ``/product/*``, *dynamic routing* is activated, and Ocelot applies the rate limiting rules defined for the '``product``' service in the ``DynamicRoutes`` section, as described in the :doc:`../features/ratelimiting` documentation.
The '``notification``' service is unlimited because both caching, tracing, and rate limiting are disabled.
All other services use the global ``RateLimitOptions`` along with the other specified options.

.. warning::
  Dynamic route ``RateLimitRule`` option is deprecated!

  The `old schema <https://github.com/ThreeMammals/Ocelot/blob/24.0.0/src/Ocelot/Configuration/File/FileDynamicRoute.cs>`_ ``RateLimitRule`` section is deprecated in version `24.1`_!
  Use ``RateLimitOptions`` instead of ``RateLimitRule``! Note that ``RateLimitRule`` will be removed in version `25.0`_!
  For backward compatibility in version `24.1`_, the ``RateLimitRule`` section takes precedence over the ``RateLimitOptions`` section.

.. note::

  The ``ServiceNamespace`` option was introduced in version `24.1`_ to enable precise overrides for the :doc:`../features/kubernetes` providers.
  If ``ServiceNamespace`` is left empty or undefined, only **one** dynamic route with the same ``ServiceName`` may be defined in the ``DynamicRoutes`` collection.

"""" 

.. [#f1] The ":doc:`../features/dynamic-routing`" feature was requested in issue `340`_ (pull request `351`_) and released in version `7.0.1`_.
  Later, the new ``DynamicRoutes`` :doc:`../features/configuration` section was introduced in pull request `508`_ and released in version `8.0.4`_.
.. [#f2] The :ref:`Configuration <dynamic-routing-configuration>` feature of :doc:`../features/dynamic-routing` was requested in issue `585`_, then significantly redeveloped and released in version `24.1`_.
.. [#f3] Multiple-segment URL path routing was developed for issue `1188`_ in pull request `1198`_ and released in version `26.0`_.

.. _Consul: https://www.consul.io/
.. _UrlPathDynamicRouting: https://github.com/ThreeMammals/Ocelot/blob/develop/src/ServiceDiscovery/UrlPathDynamicRouting.cs
.. _MultipleSegmentUrlPathDynamicRouting: https://github.com/ThreeMammals/Ocelot/blob/develop/src/ServiceDiscovery/MultipleSegmentUrlPathDynamicRouting.cs
.. _TwoSegmentsUrlPathDynamicRouting: https://github.com/ThreeMammals/Ocelot/blob/develop/acceptance/ServiceDiscovery/TwoSegmentsUrlPathDynamicRouting.cs
.. _ocelot.json: https://github.com/ThreeMammals/Ocelot/blob/main/samples/ServiceDiscovery/ApiGateway/ocelot.json

.. _1188: https://github.com/ThreeMammals/Ocelot/issues/1188
.. _1198: https://github.com/ThreeMammals/Ocelot/pull/1198

.. _340: https://github.com/ThreeMammals/Ocelot/issues/340
.. _351: https://github.com/ThreeMammals/Ocelot/pull/351
.. _508: https://github.com/ThreeMammals/Ocelot/pull/508
.. _585: https://github.com/ThreeMammals/Ocelot/issues/585

.. _7.0.1: https://github.com/ThreeMammals/Ocelot/releases/tag/7.0.1
.. _8.0.4: https://github.com/ThreeMammals/Ocelot/releases/tag/8.0.4
.. _24.1: https://github.com/ThreeMammals/Ocelot/releases/tag/24.1.0
.. _25.0: https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0
.. _26.0: https://github.com/ThreeMammals/Ocelot/releases/tag/26.0.0
.. _issue comment: https://github.com/ThreeMammals/Ocelot/issues/1188#issuecomment-1655872759
