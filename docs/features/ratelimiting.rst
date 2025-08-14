Rate Limiting
=============

  What is rate limiting? Ask `Bing <https://www.bing.com/search?q=Rate+Limiting>`_ and ask `Google <https://www.google.com/search?q=Rate+Limiting>`_

  * `What is rate limiting? | Microsoft Cloud | Microsoft Learn <https://learn.microsoft.com/en-us/microsoft-cloud/dev/dev-proxy/concepts/what-is-rate-limiting>`_ 
  * `Rate Limiting pattern | Azure Architecture Center | Microsoft Learn <https://learn.microsoft.com/en-us/azure/architecture/patterns/rate-limiting-pattern>`_
  * `Rate limit an HTTP handler in .NET | .NET | Microsoft Learn <https://learn.microsoft.com/en-us/dotnet/core/extensions/http-ratelimiter>`_

Ocelot implements *rate limiting* for upstream requests to prevent downstream services from being overwhelmed. [#f1]_

By Client's Header
------------------

To configure *rate limiting* for a route, include the following JSON configuration:

.. code-block:: json

  "RateLimitOptions": {
    "ClientWhitelist": [], // array of strings
    "EnableRateLimiting": true,
    "Limit": 1,
    "Period": "1s", // seconds, minutes, hours, days
    "PeriodTimespan": 1 // only seconds
  }

* ``ClientWhitelist``: An array that contains the clients exempt from *rate limiting*.
  For additional details about the ``ClientIdHeader`` option, consult the :ref:`rl-global-configuration` section.
* ``EnableRateLimiting``: This setting activates *rate limiting* for endpoints.
* ``Limit``: This parameter specifies the maximum number of requests a client is permitted to make within a defined ``Period``.
* ``Period``: This parameter specifies the duration during which the limit is applicable, such as ``1s`` (seconds), ``5m`` (minutes), ``1h`` (hours), or ``1d`` (days).
  If the exact ``Limit`` of requests is reached, the excess is immediately blocked, and the ``PeriodTimespan`` begins.
  You must wait for the ``PeriodTimespan`` to elapse before making another request.
  If you exceed the allowed number of requests within the period, beyond what the ``Limit`` permits, the ``QuotaExceededMessage`` will appear in the response, along with the corresponding ``HttpStatusCode``.
* ``PeriodTimespan``: This parameter specifies the time, in **seconds**, after which a retry is allowed.
  During this interval, the ``QuotaExceededMessage`` will be included in the response, along with the corresponding ``HttpStatusCode``.
  Clients are encouraged to refer to the ``Retry-After`` header to determine when subsequent requests can be made.

.. _rl-global-configuration:

Global Configuration
--------------------

  Global options are only accessible in the special :ref:`routing-dynamic` mode.

You can configure the following options in the ``GlobalConfiguration`` section of `ocelot.json`_:

.. code-block:: json

  "GlobalConfiguration": {
    "BaseUrl": "https://api.mybusiness.com",
    "RateLimitOptions": {
      "ClientIdHeader": "MyRateLimiting",
      "DisableRateLimitHeaders": false,
      "HttpStatusCode": 418, // I'm a teapot
      "QuotaExceededMessage": "Customize Tips!",
      "RateLimitCounterPrefix": "ocelot"
    }
  }

.. list-table::
    :widths: 25 75
    :header-rows: 1

    * - *Option*
      - *Description*
    * - ``ClientIdHeader``
      - Specifies the header used to identify clients, with ``ClientId`` set as the default.
    * - ``DisableRateLimitHeaders``
      - Specifies whether the ``X-Rate-Limit`` and ``Retry-After`` headers are disabled.
    * - ``HttpStatusCode``
      - Specifies the HTTP status code returned during *rate limiting*, with a default value of **429** (`Too Many Requests`_).
    * - ``QuotaExceededMessage``
      - Specifies the message displayed when the quota is exceeded. This parameter is optional, and the default message is informative.
    * - ``RateLimitCounterPrefix``
      - Specifies the counter prefix used to construct the *rate limiting* counter cache key.

Notes
-----

1. Global ``RateLimitOptions`` are supported when the :ref:`sd-dynamic-routing` feature is configured with :doc:`../features/servicediscovery`.
   Therefore, if :doc:`../features/servicediscovery` is not set up, only the options for static routes need to be defined to enforce limitations at the route level.
2. Global *rate limiting* options may not be practical as they apply limits to all routes.
   In a microservices architecture, it is unusual to enforce the same limitations across all routes.
   Configuring per-route *rate limiting* could offer a more tailored solution.
   However, global *rate limiting* can be logical if all routes share the same downstream hosts, thereby restricting the usage of a single service.
3. *Rate limiting* is now built into ASP.NET Core 7+, as detailed in the :ref:`rl-ocelot-vs-asp-net` topic below.
   Our team believes that the ASP.NET ``RateLimiter`` facilitates global limitations through its *rate-limiting* policies.


.. _rl-global-rate-limiting:

Global Rate Limiting
--------------------

Ocelot now supports defining Global Rate Limiting rules for groups of routes. These rules are inserted before the existing rate limiting middleware and will add a ``RateLimitRule`` to any route that has no explicit rate limiting configured and whose ``DownstreamPathTemplate`` matches one of the global rule patterns.

Configuration in JSON

In your configuration file (e.g., ``ocelot.json``), add the ``RateLimiting.ByPattern`` array with rules:

.. code-block:: json

  "GlobalConfiguration": {
    "BaseUrl": "https://api.ocelot.net",
    "RateLimiting": {
      "ByPattern": [
        {
          "Pattern": "/api/users/*",
          "Limit": 10,
          "Period": "1m",
          "PeriodTimespan": 1,
          "QuotaExceededMessage": "Global limit exceeded. Try again later."
        },
        {
          "Pattern": "/api/posts/*",
          "Limit": 5,
          "Period": "30s",
          "PeriodTimespan": 30,
          "QuotaExceededMessage": "Too many post requests."
        }
      ]
    }
  }

Fields in each global rule:

.. list-table::
    :widths: 25 75
    :header-rows: 1

    * - *Field*
      - *Description*
    * - ``Pattern``
      - The downstream path template pattern to match (using Ocelot's syntax).
    * - ``Limit``
      - The maximum number of requests allowed per period.
    * - ``Period``
      - A human-readable string representing the time window (e.g., '1m', '30s').
    * - ``PeriodTimespan``
      - The numeric value corresponding to ``Period`` (e.g., 1 for 1 minute).
    * - ``QuotaExceededMessage``
      - The error message returned when the limit is exceeded.

Behavior
--------

1. **Loading Configuration**: Ocelot reads the ``RateLimiting.ByPattern`` array when loading its configuration.

2. **Injecting Rules into Routes**:

   * Before the rate limiting middleware executes, the Configuration Builder iterates over all routes.
   * For each route that does **not** have an explicit rate limiting rule:

     * If its ``DownstreamPathTemplate`` matches the ``Pattern`` of a global rule, a new ``RateLimitRule`` is created with that rule's settings and added to the route.

3. **Middleware Execution**:

   * With the injected rule present, the existing rate limiting middleware applies it like any other rule.
   * If the number of requests exceeds the configured `Limit`, Ocelot returns an HTTP 429 response with the specified ``QuotaExceededMessage``.

  **Note:** There is no need to modify ``RateLimitMiddleware`` itself—adding the rule to the route's configuration automatically includes it in the rate limiting pipeline.

.. _rl-ocelot-vs-asp-net:

Ocelot vs ASP.NET
-----------------

The Ocelot team is considering a redesign of the *Rate Limiting* feature in light of the "`Announcing Rate Limiting for .NET`_" article by Brennan Conroy, published on July 13th, 2022.
As of now, no decision has been made, and the previous version of the feature continues to be part of the `20.0`_ release for .NET 7, and `24.0`_ release for .NET 8/9. [#f2]_

Discover the new features in the ASP.NET Core 7.0 release:

* The `RateLimiter Class <https://learn.microsoft.com/en-us/dotnet/api/system.threading.ratelimiting.ratelimiter>`_, available since ASP.NET Core 7.0
* The `System.Threading.RateLimiting <https://www.nuget.org/packages/System.Threading.RateLimiting>`_ NuGet package
* The `Rate limiting middleware in ASP.NET Core <https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit>`_ article by Arvin Kahbazi, Maarten Balliauw, and Rick Anderson

While it makes sense to retain the old implementation as a built-in feature of Ocelot, we are planning a transition to the new ``RateLimiter`` from the ``Microsoft.AspNetCore.RateLimiting`` namespace.

We encourage you to share your thoughts with us in the `Discussions <https://github.com/ThreeMammals/Ocelot/discussions>`_ of the repository. |octocat|

""""

.. [#f1] Historically, the *Rate Limiting* feature is one of Ocelot's oldest and first features. This feature was introduced in pull request `37`_ by `@geffzhang`_. Many thanks! It was initially released in version `1.3.2`_. The authors were inspired by an `article by @catcherwong`_ to create this documentation.
.. [#f2] Since pull request `37`_ and version `1.3.2`_, the Ocelot team has reviewed and redesigned the feature to ensure stable behavior. The fix for bug `1590`_ (PR `1592`_) was released as part of version `23.3`_.

.. _Announcing Rate Limiting for .NET: https://devblogs.microsoft.com/dotnet/announcing-rate-limiting-for-dotnet/
.. _ocelot.json: https://github.com/ThreeMammals/Ocelot/blob/main//samples/Basic/ocelot.json
.. _@geffzhang: https://github.com/ThreeMammals/Ocelot/commits?author=geffzhang
.. _article by @catcherwong: http://www.c-sharpcorner.com/article/building-api-gateway-using-ocelot-in-asp-net-core-rate-limiting-part-four/
.. _Too Many Requests: https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/429

.. _37: https://github.com/ThreeMammals/Ocelot/pull/37
.. _1590: https://github.com/ThreeMammals/Ocelot/issues/1590
.. _1592: https://github.com/ThreeMammals/Ocelot/pull/1592
.. _1.3.2: https://github.com/ThreeMammals/Ocelot/releases/tag/1.3.2

.. _20.0: https://github.com/ThreeMammals/Ocelot/releases/tag/20.0.0
.. _23.3: https://github.com/ThreeMammals/Ocelot/releases/tag/23.3.0
.. _24.0: https://github.com/ThreeMammals/Ocelot/releases/tag/24.0.0

.. |octocat| image:: https://github.githubassets.com/images/icons/emoji/octocat.png
  :alt: octocat
  :height: 25
  :class: img-valign-middle
