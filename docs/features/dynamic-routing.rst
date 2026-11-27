Dynamic Routing
===============
.. contents:: Table of Contents
   :depth: 2
   :local:

.. _dr-about:

About [#f1]_
------------

The idea is to enable *dynamic routing* mode when using a :doc:`../features/servicediscovery` provider.
In this mode, Ocelot uses the first segment of the upstream path to look up the downstream service via the :doc:`../features/servicediscovery` provider.

An example of this would be calling Ocelot with a URL like

* ``https://api.ocelot.net/product/products``

Ocelot will take the first segment of the path, which is ``product``, and use it as a key to look up the service in :ref:`sd-consul`.
If :ref:`sd-consul-provider` returns a service, Ocelot will request it using the host and port provided by `Consul`_, appending the remaining path segments—in this case, ``products``—to construct final downstream URL:

* ``http://hostfromconsul:portfromconsul/products``

Ocelot will append any query string to the downstream URL as usual.

.. warning::
  To enable *dynamic routing*, the `ocelot.json`_ configuration must contain no static routes in the ``Routes`` collection!
  Currently, dynamic routes and static routes cannot be mixed.

  .. code-block:: json

    {
      "Routes": [], // must be empty to enable dynamic routing!
      "DynamicRoutes": [],
      "GlobalConfiguration": {}
    }

  Additionally, you need to specify the details of the *service discovery* provider as outlined below, along with the downstream ``http(s)`` scheme under ``DownstreamScheme``.

For instance, when exposing Ocelot publicly over HTTPS while routing to internal services over HTTP, your configuration may resemble the following:

  .. code-block:: json
    :emphasize-lines: 3-5

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

.. _dr-dynamic-route-schema:

Dynamic Route Schema
--------------------

  Class: :class:`FileDynamicRoute`

The :ref:`config-global-configuration-schema` contains the global options for dynamic routes.
Each service can additionally override these options through a ``DynamicRoutes`` collection.
Here is the complete dynamic route configuration, also known as the *"dynamic route schema,"* of top-level properties.

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

The actual dynamic route schema with all the properties can be found in the C# `FileDynamicRoute <https://github.com/ThreeMammals/Ocelot/blob/develop/src/Configuration/File/FileDynamicRoute.cs>`_ class.

.. note::
  1. The `old dynamic route schema <https://github.com/ThreeMammals/Ocelot/blob/24.1.0/src/Ocelot/Configuration/File/FileDynamicRoute.cs#L8-L9>`_ ``RateLimitRule`` section is deprecated in version `24.1`_!
  Use ``RateLimitOptions`` instead of ``RateLimitRule``! Note that ``RateLimitRule`` will be removed in version `25.0`_!
  For backward compatibility in version `24.1`_, the ``RateLimitRule`` section takes precedence over the ``RateLimitOptions`` section.

  2. In addition to the global ``ServiceDiscoveryProvider`` section, the :ref:`config-global-configuration-schema` includes configurable options such as ``DownstreamScheme``, ``CacheOptions``, ``HttpHandlerOptions``, ``LoadBalancerOptions``, ``QoSOptions``, ``RateLimitOptions``, and ``Timeout``.
  These options are applicable to all dynamic routes, globally.
  Moreover, starting with version `24.1`_, the :ref:`dr-dynamic-route-schema` also supports these options for overriding global settings.

.. _dr-configuration:

Configuration [#f2]_
--------------------

Ocelot also allows configuration of a ``DynamicRoutes`` collection consisting of :ref:`dr-dynamic-route-schema` objects.
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
  1. The ``ServiceNamespace`` option was introduced in version `24.1`_ to enable precise overrides for the :doc:`../features/kubernetes` providers.
  If ``ServiceNamespace`` is left empty or undefined, only **one** dynamic route with the same ``ServiceName`` may be defined in the ``DynamicRoutes`` collection.

  2. The following options were **not** supported in versions prior to `24.1`_ for overriding globally configured options:
  ``AuthenticationOptions``, ``CacheOptions``, ``HttpHandlerOptions``, ``LoadBalancerOptions``, ``QoSOptions``, ``RateLimitOptions``, ``ServiceNamespace``, and ``Timeout``.

Route Provider Design
---------------------
  | Interface: :class:`IDownstreamRouteProvider`
  | Class: :class:`DiscoveryDownstreamRouteFinder`

The ``DiscoveryDownstreamRouteFinder`` creates each dynamic route from the :ref:`dr-dynamic-route-schema` described above.
It is the special ``IDownstreamRouteProvider`` for dynamic route creation and uses the full service name, including its namespace when present, as the :doc:`load-balancing <../features/loadbalancer>` key.

The injected ``IDynamicRouting`` router service produces the dynamic routing artifacts used to create the route:

* service namespace
* service name
* downstream path

This separation allows applications to change how service names and downstream paths are detected without replacing the route provider.

To summarize, the route provider injects ``IDynamicRouting``, asks it for the service namespace, service name, and downstream path, then creates a dynamic route from the global configuration and any matching dynamic route schema overrides.

.. seealso::
  For curious developers, it is worth understanding the new :class:`IDynamicRouting` abstraction and how it is used by the :class:`DiscoveryDownstreamRouteFinder` (a route provider) to create dynamic routes:
  search for `IDynamicRouting <https://github.com/search?q=repo%3AThreeMammals%2FOcelot+IDynamicRouting&type=code>`_.

  Note that the :class:`IDynamicRouting` service was introduced in version `26.0 <https://github.com/ThreeMammals/Ocelot/releases/tag/26.0.0>`_ to support multiple router types,
  currently including the default :ref:`dr-url-path-router` and the new :ref:`Multiple Segment URL Path Router <dr-multiple-segment-url-path-router>`.

  Need a custom router? The community is welcome to contribute additional router types from the :ref:`dr-roadmap`.
  Unfortunately, the Ocelot team has run out of development capacity.
  Help us build a magic dynamic routing in Ocelot: submit a pull request or let's discuss your idea in the `Discussions <https://github.com/ThreeMammals/Ocelot/discussions/categories/ideas>`_ before it ships.

.. _dr-url-path-router:

URL Path Router
---------------
.. _TwoSegmentsUrlPathDynamicRouter: https://github.com/search?q=repo%3AThreeMammals%2FOcelot+TwoSegmentsUrlPathDynamicRouter&type=code

  | Interface: :class:`IDynamicRouting`
  | Class: :class:`UrlPathDynamicRouter`

The default *dynamic routing* type is the *URL path router*. It detects the service name from *the first segment* of the upstream URL path.
For example, ``/product/products`` resolves ``product`` as the service name and forwards ``/products`` as the downstream path.
The first segment may also contain a namespace separated from the service name by a dot, such as ``/namespace.product/products``.

The default implementation is :class:`UrlPathDynamicRouter`, which implements :class:`IDynamicRouting`.
The :class:`DiscoveryDownstreamRouteFinder` uses this service to obtain the service name and the downstream path while creating dynamic routes.
It is registered in the dependency injection container by ``AddOcelotServiceDiscovery()``, which is called by :doc:`../features/dependencyinjection` when configuring Ocelot.

Applications can replace the default implementation with the ``IServiceCollection`` or ``IOcelotBuilder`` APIs:

.. code-block:: csharp

  // via IServiceCollection
  services.AddOcelotDynamicRouting<MyUrlPathDynamicRouter>();

  // via IOcelotBuilder
  builder.Services
      .AddOcelot(builder.Configuration) // -> IOcelotBuilder
      .AddDynamicRouting<MyUrlPathDynamicRouter>();

To create a custom URL path router, inherit from :class:`UrlPathDynamicRouter` and override the methods that determine the downstream path and service name.
The acceptance tests contain `TwoSegmentsUrlPathDynamicRouter`_ as an example that combines two URL path segments into one service name.
The following is only the customization shape; the routing implementation is application-specific:

.. code-block:: csharp

  public class MyUrlPathDynamicRouter(IHttpContextAccessor contextAccessor)
      : UrlPathDynamicRouter(contextAccessor)
  {
      protected override string DownstreamPath(string upstreamUrlPath)
          => "implement me!" // Return the path after the custom service-name segments

      public override ServiceInfo GetServiceName(string upstreamUrlPath)
          => new("implement me!"); // Return the service name and optional namespace
  }

.. role:: underline
   :class: underline
.. _dr-multiple-segment-url-path-router:

Multiple Segment URL Path Router [#f3]_
---------------------------------------
  | Interface: :class:`IDynamicRouting`
  | Class: :class:`MultipleSegmentUrlPathDynamicRouter`

The *multiple-segment URL path router* is a dynamic routing type that extracts the service name from multiple segments at the beginning of the upstream path.
:underline:`It supports service names that contain slashes` (``/``).
For example, ``/authorization/api$/path`` resolves ``authorization/api`` as the service name and forwards ``/path`` downstream.

The built-in :class:`MultipleSegmentUrlPathDynamicRouter` class extends :class:`UrlPathDynamicRouter`.
It is therefore still used by :class:`DiscoveryDownstreamRouteFinder` through the :class:`IDynamicRouting` abstraction, while overriding service-name and downstream-path detection for multiple segments.
It would be registered in the same way as other custom dynamic routers:

.. code-block:: csharp

  // via IServiceCollection
  services.AddOcelotDynamicRouting<MultipleSegmentUrlPathDynamicRouter>();

  // via IOcelotBuilder
  builder.Services
      .AddOcelot(builder.Configuration) // -> IOcelotBuilder
      .AddDynamicRouting<MultipleSegmentUrlPathDynamicRouter>();

To create a custom *multiple-segment URL path router*, inherit from :class:`MultipleSegmentUrlPathDynamicRouter` and override the methods that define the service-name delimiter or service-name mapping.
The following shows the customization shape without prescribing the implementation:

.. code-block:: csharp

  public class MyMultipleSegmentRouter(IHttpContextAccessor contextAccessor)
      : MultipleSegmentUrlPathDynamicRouter(contextAccessor)
  {
      protected override char ServiceNameDelimiter => ':'; // optional delimiter override
      protected override int? Segments => 2; // optional segment-count override

      public override ServiceInfo GetServiceName(string upstreamUrlPath)
          => base.GetServiceName(upstreamUrlPath); // Wanna be a super star? Just utilize base implementation!

      protected override string DownstreamPath(string upstreamUrlPath)
          => base.DownstreamPath(upstreamUrlPath); // Don't hack this software! Just utilize base implementation!
  }

.. note::
  Do not override mentioned methods unless you have a good reason to do so.

The default delimiter is ``$``. It can be configured through options:

.. code-block:: csharp

  // via IServiceCollection
  services.AddOcelotDynamicRouting<MultipleSegmentUrlPathDynamicRouter>(options => options.Delimiter = ':');

  // via IOcelotBuilder
  builder.Services
      .AddOcelot(builder.Configuration) // -> IOcelotBuilder
      .AddDynamicRouting<MultipleSegmentUrlPathDynamicRouter>(options => options.Delimiter = ':');

Choose a delimiter that is not alphanumeric and is not ``/``.
When the delimiter is a reserved URL-path character, encode it in the request URL according to URL standards.
For example, a ``#`` delimiter should be sent as ``%23`` so that it remains part of the path instead of becoming a fragment marker.

.. _MultipleSegmentUrlPathDynamicRouterOptions: https://github.com/ThreeMammals/Ocelot/blob/develop/src/ServiceDiscovery/MultipleSegmentUrlPathDynamicRouter.cs

Operation modes
^^^^^^^^^^^^^^^

The router supports two mutually exclusive operation modes:

* **Delimiter mode** locates the service-name boundary by the first selected delimiter character.
  The default delimiter is ``$``; applications can configure the ``Delimiter`` option or override ``ServiceNameDelimiter``.
* **Segments mode** counts a fixed number of slash-separated path segments after the initial slash and uses the following slash as the boundary.
  Configure the nullable ``Segments`` option with a positive integer, or override the protected virtual ``Segments`` property.
  When neither is configured, the protected property returns ``DefaultSegments`` (``0``), so the router remains in *Delimiter* mode.

When header management is enabled, a valid non-negative segment header controls mode selection for the request: a positive value activates *Segments* mode and zero activates *Delimiter* mode.
Otherwise, an overridden ``Segments`` value takes priority over the configured ``Segments`` value.
Any positive effective ``Segments`` value activates *Segments* mode and the router ignores both delimiter options and the delimiter management header.
If the effective value is ``null``, zero, or negative, the router remains in *Delimiter* mode.

For example, this configuration treats ``authorization/api`` as the service name in ``/authorization/api/path`` and forwards ``/path``:

.. code-block:: csharp

  services.AddOcelotDynamicRouting<MultipleSegmentUrlPathDynamicRouter>(options =>
  {
      options.Delimiter = ':'; // ignored while Segments mode is active
      options.Segments = 2;
  });

If a positive segment count extends beyond the slash-delimited path segments, or a selected delimiter is not present, the router falls back to the default first-segment routing behavior.

.. note::
  When header management is enabled, request-time values can temporarily select either mode.
  In particular, the special zero value for ``Ocelot-DynamicRouting-UrlPath-Segments`` switches a configured *Segments* mode to *Delimiter* mode for that request.
  See :ref:`dr-management-headers` for the complete fallback rules.

.. _dr-management-headers:

Management headers
^^^^^^^^^^^^^^^^^^
  * ``Ocelot-DynamicRouting-UrlPath-Segments``
  * ``Ocelot-DynamicRouting-UrlPath-Delimiter``

Header management is disabled by default: the router ignores management headers and uses its configured or overridden operation mode.
To enable per-request control, set ``IsHeaderManagement`` to ``true``:

.. code-block:: csharp

  // via IServiceCollection
  services.AddOcelotDynamicRouting<MultipleSegmentUrlPathDynamicRouter>(isHeaderManagement: true);

  // via IOcelotBuilder, with default fallback delimiter '$'
  builder.Services
      .AddOcelot(builder.Configuration) // -> IOcelotBuilder
      .AddDynamicRouting<MultipleSegmentUrlPathDynamicRouter>(true);

  // via IOcelotBuilder, with a configured fallback delimiter
  builder.Services
      .AddOcelot(builder.Configuration) // -> IOcelotBuilder
      .AddDynamicRouting<MultipleSegmentUrlPathDynamicRouter>(options =>
      {
          options.Delimiter = ':';
          options.IsHeaderManagement = true;
      });

When enabled, the following headers take priority over compile-time or overridden values:

* ``Ocelot-DynamicRouting-UrlPath-Segments`` specifies a positive integer: the number of slash-separated path segments belonging to the service name.
  The slash after the last counted segment marks the boundary to the downstream path.
* ``Ocelot-DynamicRouting-UrlPath-Delimiter`` specifies the delimiter character in the path.
  The header can be percent-encoded; if its decoded value contains more than one character, only the first is used.

The positive segment count has priority over the delimiter header when both are supplied.
A positive value selects *Segments* mode, so the delimiter header is ignored.
The special zero value selects *Delimiter* mode and disables the configured or overridden segment count for that request.
For example:

.. code-block:: http

  GET /authorization/api/users/downstream-path?query HTTP/1.1
  Host: ocelot.net
  Ocelot-DynamicRouting-UrlPath-Segments: 3
  Ocelot-DynamicRouting-UrlPath-Delimiter: %23

This request resolves ``authorization/api/users`` as the service name and forwards ``/downstream-path?query`` downstream.
Another request can use a different segment count, or omit it and use the delimiter header instead:

.. code-block:: http

  GET /authorization/api%23/downstream-path?query HTTP/1.1
  Host: ocelot.net
  Ocelot-DynamicRouting-UrlPath-Delimiter: %23

This resolves ``authorization/api`` and forwards ``/downstream-path?query``.
Headers can therefore vary per request when service names cover different numbers of segments.

A request can also switch from configured *Segments* mode to *Delimiter* mode:

.. code-block:: http

  GET /authorization/api%23/downstream-path?query HTTP/1.1
  Host: ocelot.net
  Ocelot-DynamicRouting-UrlPath-Segments: 0
  Ocelot-DynamicRouting-UrlPath-Delimiter: %23

The zero value ignores the configured or overridden segment count, so this request resolves ``authorization/api`` as the service name by using the ``#`` delimiter and forwards ``/downstream-path?query`` downstream.

.. note::
  If the segment header is missing, non-numeric, outside the integer range, or negative, the router falls back to its overridden or configured segment count.
  If that effective value is positive, *Segments* mode remains active and the delimiter header is ignored.
  A valid zero segment header selects *Delimiter* mode instead and the delimiter header is applied.
  If the delimiter header is missing, empty, or decodes to a value beginning with ``/``, it uses the configured or overridden delimiter instead.
  A configured or overridden ``/`` delimiter is replaced by the default ``$``.
  If a positive segment count has no corresponding slash in the path, or the selected delimiter is not found, the router falls back to first-segment routing.
  When the matching slash or delimiter is at the end of the path, the downstream path is ``/``.

.. caution::
  Enabling header management allows clients to change routing per request, which can reduce stability if headers are invalid or unexpected.
  Enable it only for known, trusted clients that supply valid header values; otherwise leave it disabled for predictable configuration-based routing.

.. _dr-roadmap:

Roadmap
-------

The first-segment :ref:`dr-url-path-router` has been available for many years.
The :ref:`Multiple Segment URL Path Router <dr-multiple-segment-url-path-router>` is available starting with version `26.0`_.

The broader goal of dynamic routing is to allow the service name to be detected from service discovery artifacts or from HTTP request data, rather than only from the URL path.
The community may benefit from additional dynamic router types such as:

* :doc:`../features/servicediscovery` provider data routers
* Query-string parameter routers
* Header routers
* Host-name routers
* Port routers
* Magic routers, etc.

These desired router types are tracked in issue `1188`_ (see this `issue comment <https://github.com/ThreeMammals/Ocelot/issues/1188#issuecomment-1655872759>`_).

"""" 

.. [#f1] The ":ref:`Dynamic Routing <dr-about>`" feature was requested in issue `340`_ (pull request `351`_) and released in version `7.0.1`_.
  Later, the new ``DynamicRoutes`` :doc:`../features/configuration` section was introduced in pull request `508`_ and released in version `8.0.4`_.
.. [#f2] The :ref:`Configuration <dr-configuration>` feature of :doc:`../features/dynamic-routing` was requested in issue `585`_, then significantly redeveloped and released in version `24.1`_.
.. [#f3] The ":ref:`Multiple Segment URL Path Router <dr-multiple-segment-url-path-router>`" was developed for issue `1188`_ in pull request `1198`_ and released in version `26.0`_.

.. _Consul: https://www.consul.io/
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
