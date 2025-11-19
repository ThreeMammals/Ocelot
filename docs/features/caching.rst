.. _Program: https://github.com/ThreeMammals/Ocelot/blob/main/samples/Basic/Program.cs

Caching
=======

[#f1]_ Ocelot currently supports caching on the URL of the downstream service and setting a TTL in seconds to expire the cache.
Users can also clear the cache for a specific region by using Ocelot's :ref:`administration-api`.

Ocelot utilizes some very rudimentary caching at the moment provider by the `CacheManager <https://github.com/MichaCo/CacheManager>`_ project.
This is an amazing project that is solving a lot of caching problems. We would recommend using this package to cache with Ocelot. 

The following example shows how to add *CacheManager* to Ocelot so that you can do output caching. 

Install
-------

First of all, add the following `Ocelot.Cache.CacheManager <https://www.nuget.org/packages/Ocelot.Cache.CacheManager>`_ package:

.. code-block:: powershell

    Install-Package Ocelot.Cache.CacheManager

This will give you access to the Ocelot cache manager extension methods.
The second step is to add the following to your `Program`_:

.. code-block:: csharp

    using Ocelot.Cache.CacheManager;

    builder.Services
        .AddOcelot(builder.Configuration)
        .AddCacheManager(x => x.WithDictionaryHandle());

``CacheOptions`` Schema
-----------------------

.. _FileCacheOptions: https://github.com/ThreeMammals/Ocelot/blob/main/src/Ocelot/Configuration/File/FileCacheOptions.cs

  Class: `FileCacheOptions`_

The following is the full *caching* configuration, used in both the :ref:`config-route-schema` and the :ref:`config-dynamic-route-schema`.
Not all of these options need to be configured; however, the ``TtlSeconds`` option is mandatory.

.. code-block:: json

  "CacheOptions": {
    "TtlSeconds": 1, // nullable integer
    "Region": "", // string
    "Header": "", // string
    "EnableContentHashing": false // nullable boolean
  }

.. list-table::
  :widths: 25 75
  :header-rows: 1

  * - *Option*
    - *Description*
  * - ``TtlSeconds``
    - Time-To-Live (TTL) in seconds for the cached downstream response, i.e., the absolute expiration timeout starting from when the item is added to the cache.
      This option is required. If undefined, it defaults to 0 (zero), which disables caching.
  * - ``Region``
    - Specifies the cache region to be cleared via Ocelot's :ref:`administration-api`.
      See: ``DELETE {adminPath}/outputcache/{region}``
  * - ``Header``
    - Specifies the header name used for native Ocelot caching control, defaulting to the special ``OC-Cache-Control`` header.
      If the header is present, its value is included in the cache key constructed by the ``ICacheKeyGenerator`` service.
      Varying header values result in different cache keys, effectively invalidating the cache.

  * - ``EnableContentHashing``
    - Toggles inclusion of request body hashing in the cache key.
      Disabled by default (``false``) due to potential performance impact.
      Recommended for POST/PUT routes where request body affects response.
      Refer to the :ref:`EnableContentHashing option <caching-enablecontenthashing-option>` section.

The actual ``CacheOptions`` schema with all the properties can be found in the C# `FileCacheOptions`_ class.

Configuration
-------------

Finally, in order to use caching on a route in your route configuration add these sections:

.. code-block:: json

    "CacheOptions": {
      "TtlSeconds": 15,
      "Region": "europe-central",
      "Header": "OC-Cache-Control",
      "EnableContentHashing": false // my route has GET verb only, assigning 'true' for requests with body: POST, PUT etc.
    },
    // Warning! FileCacheOptions section is deprecated! -> use CacheOptions
    "FileCacheOptions": {
      "TtlSeconds": 15,
      "Region": "europe-central",
      "Header": "OC-Cache-Control",
      "EnableContentHashing": false // my route has GET verb only, assigning 'true' for requests with body: POST, PUT etc.
    }

* In this example, ``TtlSeconds`` is set to 15, which means the cache will expire 15 seconds after the response is stored.
* The ``Region`` property specifies a cache region. Cache entries within a region can be cleared by calling Ocelot's :ref:`administration-api`.
* If a header name is defined in the ``Header`` property, its value is retrieved from the ``HttpRequest`` headers.
  If the header is present, its value is included in the cache key constructed by the ``ICacheKeyGenerator`` service.
  Varying header values result in different cache keys, effectively invalidating the cache.
* Finally, ``EnableContentHashing`` is disabled due to the current route using the ``GET`` verb, which does not include a request body.

.. _24.1: https://github.com/ThreeMammals/Ocelot/releases/tag/24.1.0
.. _25.0: https://github.com/ThreeMammals/Ocelot/milestone/12
.. warning::
  According to the static :ref:`config-route-schema`, the ``FileCacheOptions`` section has been deprecated!

  The `old schema <https://github.com/ThreeMammals/Ocelot/blob/24.1.0/src/Ocelot/Configuration/File/FileRoute.cs#L86-L88>`_ ``FileCacheOptions`` section is deprecated in version `24.1`_!
  Use ``CacheOptions`` instead of ``FileCacheOptions``! Note that ``FileCacheOptions`` will be removed in version `25.0`_!
  For backward compatibility in version `24.1`_, the ``FileCacheOptions`` section takes precedence over the ``CacheOptions`` section.

.. _caching-enablecontenthashing-option:

``EnableContentHashing`` option [#f2]_
--------------------------------------

Previously, in versions prior to `23.0`_, the request body was used to compute the cache key.
However, due to potential performance issues arising from request body hashing, it has been disabled by default.
Clearly, this constitutes a breaking change and presents challenges for users who require cache key calculations that consider the request body (e.g., for the POST method).
To address this issue, it is recommended to enable the option either at the route level or globally in the ":ref:`Global Configuration <caching-global-configuration>`" section:

.. code-block:: json

    "CacheOptions": {
      // ...
      "EnableContentHashing": true
    }

.. rubric:: Ocelot Team Recommendation

Although the community raised concerns about backward compatibility in issue `2234`_, Ocelot team maintains that *caching* performance takes precedence over backward compatibility when migrating from versions prior to `23.0`_.
The proposed option clarifies that ``POST`` requests should **not** be cached; only ``GET`` requests are eligible for caching.
Therefore, ``POST`` and ``GET`` verbs must be separated into distinct routes:

* POST routes with *caching* disabled
* GET routes with *caching* enabled

.. _caching-global-configuration:

Global Configuration [#f3]_
---------------------------

Copying route-level properties for each static route is no longer necessary, as version `23.3`_ allows these values to be set in the ``GlobalConfiguration`` section.
This convenience applies to ``Header`` and ``Region`` as well.
However, if no global ``TtlSeconds`` value is defined, this option must still be explicitly set per route to enable caching.
As a result, the final configuration for static routes might look like:

.. code-block:: json
  :emphasize-lines: 5-7, 12, 18-21

  {
    "Routes": [
      {
        "Key": "R0", // optional
        "CacheOptions": {
          "TtlSeconds": 60 // 1-minute short-term caching
        },
        // ...
      },
      {
        "Key": "R1", // this route is part of a group
        "CacheOptions": {}, // optional due to grouping
        // ...
      }
    ],
    "GlobalConfiguration": {
      "BaseUrl": "https://ocelot.net",
      "CacheOptions": {
        "RouteKeys": ["R1",], // if undefined or empty array, opts will apply to all routes
        "TtlSeconds": 300 // enable global caching for a duration of 5 minutes
      },
      // ...
    }
  }

Dynamic routes were not supported in versions prior to `24.1`_.
Starting with version `24.1`_, global *cache options* for :ref:`Dynamic Routing <routing-dynamic>` were introduced.
These global options may also be overridden in the ``DynamicRoutes`` configuration section, as defined by the :ref:`config-dynamic-route-schema`.

.. code-block:: json
  :emphasize-lines: 6-8, 17-20

  {
    "DynamicRoutes": [
      {
        "Key": "", // optional
        "ServiceName": "my-service",
        "CacheOptions": {
          "TtlSeconds": 60 // 1-minute short-term caching
        }
      }
    ],
    "GlobalConfiguration": {
      "BaseUrl": "https://ocelot.net",
      "DownstreamScheme": "http",
      "ServiceDiscoveryProvider": {
        // required section for dynamic routing
      },
      "CacheOptions": {
        "RouteKeys": [], // or null, no grouping, thus opts apply to all dynamic routes
        "TtlSeconds": 300 // enable global caching for a duration of 5 minutes
      }
    }
  }

In this configuration, a 5-minute *caching* duration is applied to all implicit dynamic routes.
However, for the "my-service" service, the *caching* TTL has been explicitly reduced from 5 minutes to 1 minute.

.. note::

  1. If the ``RouteKeys`` option is not defined or the array is empty in the global ``CacheOptions``, the global options will apply to all routes.
  If the array contains route keys, it defines a single group of routes to which the global options apply.
  Routes excluded from this group must specify their own route-level ``CacheOptions``.

  2. Prior to version `23.3`_, global ``CacheOptions`` were not available.
  Starting with version `24.1`_, global configuration is supported for both static and dynamic routes.

.. Sample
.. -----

.. If you look at the example `here <https://github.com/ThreeMammals/Ocelot/blob/main/test/Ocelot.ManualTest/Program.cs>`_ you can see how the cache manager is setup and then passed into the Ocelot ``AddCacheManager`` configuration method.
.. You can use any settings supported by the **CacheManager** package and just pass them in.

Custom Caching
--------------

If you want to add your own caching method, implement the following interfaces and register them in DI e.g.

.. code-block:: csharp

    builder.Services
        .AddSingleton<IOcelotCache<CachedResponse>, MyCache>();

* ``IOcelotCache<CachedResponse>`` this is for output caching.
* ``IOcelotCache<FileConfiguration>`` this is for caching the file configuration if you are calling something remote to get your config such as Consul.

Roadmap
-------

Please dig into the Ocelot source code to find more.
We would really appreciate it if anyone wants to implement `Redis <https://redis.io/>`_, `Memcached <http://www.memcached.org/>`_ etc.
Please, open a new `Show and tell <https://github.com/ThreeMammals/Ocelot/discussions/categories/show-and-tell>`_ thread in `Discussions <https://github.com/ThreeMammals/Ocelot/discussions>`_ space of the repository.

""""

.. [#f1] Historically, *Caching* is one of Ocelot's earliest features, first introduced in version `1.1`_ on February 2, 2017, the initial release of Ocelot.
  The "Clear cache region via :ref:`administration-api`" feature was first delivered in pull request `109`_ and released in version `1.4.8`_.
.. [#f2] The ":ref:`EnableContentHashing option <caching-enablecontenthashing-option>`" feature was requested in issue `2059`_ and released in version `23.3`_.
.. [#f3] :ref:`Global Configuration <caching-global-configuration>` for static routes was first introduced in pull request `2058`_ and released in version `23.3`_.
  Support for dynamic routes was added in pull request `2331`_ and delivered in version `24.1`_.

.. _109: https://github.com/ThreeMammals/Ocelot/pull/109
.. _2058: https://github.com/ThreeMammals/Ocelot/pull/2058
.. _2059: https://github.com/ThreeMammals/Ocelot/issues/2059
.. _2234: https://github.com/ThreeMammals/Ocelot/issues/2234
.. _2331: https://github.com/ThreeMammals/Ocelot/pull/2331

.. _1.1: https://github.com/ThreeMammals/Ocelot/releases/tag/1.1.0
.. _1.4.8: https://github.com/ThreeMammals/Ocelot/releases/tag/1.4.8
.. _23.0: https://github.com/ThreeMammals/Ocelot/releases/tag/23.0.0
.. _23.3: https://github.com/ThreeMammals/Ocelot/releases/tag/23.3.0
.. _24.1: https://github.com/ThreeMammals/Ocelot/releases/tag/24.1.0
