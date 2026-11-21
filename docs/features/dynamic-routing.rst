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

.. _Consul: https://www.consul.io/
.. _ocelot.json: https://github.com/ThreeMammals/Ocelot/blob/main/samples/ServiceDiscovery/ApiGateway/ocelot.json

.. _340: https://github.com/ThreeMammals/Ocelot/issues/340
.. _351: https://github.com/ThreeMammals/Ocelot/pull/351
.. _508: https://github.com/ThreeMammals/Ocelot/pull/508
.. _585: https://github.com/ThreeMammals/Ocelot/issues/585

.. _7.0.1: https://github.com/ThreeMammals/Ocelot/releases/tag/7.0.1
.. _8.0.4: https://github.com/ThreeMammals/Ocelot/releases/tag/8.0.4
.. _24.1: https://github.com/ThreeMammals/Ocelot/releases/tag/24.1.0
.. _25.0: https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0
