Rate Limiting
=============

  Feature label: `Rate Limiting <https://github.com/ThreeMammals/Ocelot/labels/Rate%20Limiting>`_

  Handy articles:

  * `What is rate limiting? | Microsoft Cloud | Microsoft Learn <https://learn.microsoft.com/en-us/microsoft-cloud/dev/dev-proxy/concepts/what-is-rate-limiting>`_ 
  * `Rate Limiting pattern | Azure Architecture Center | Microsoft Learn <https://learn.microsoft.com/en-us/azure/architecture/patterns/rate-limiting-pattern>`_
  * `Rate limit an HTTP handler in .NET | .NET | Microsoft Learn <https://learn.microsoft.com/en-us/dotnet/core/extensions/http-ratelimiter>`_
  * `Rate limiting middleware in ASP.NET Core | Microsoft Learn <https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit>`_

Ocelot implements *rate limiting* [#f1]_ for upstream requests to prevent downstream services from being overwhelmed.

.. _rl-schema:

``RateLimitOptions`` Schema
---------------------------

.. _FileRateLimitByHeaderRule: https://github.com/ThreeMammals/Ocelot/blob/main/src/Ocelot/Configuration/File/FileRateLimitByHeaderRule.cs
.. _FileGlobalRateLimitByHeaderRule: https://github.com/ThreeMammals/Ocelot/blob/main/src/Ocelot/Configuration/File/FileGlobalRateLimitByHeaderRule.cs
.. _503 Service Unavailable: https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Status/503

  Class: `FileRateLimitByHeaderRule`_

As you may already know from the :doc:`../features/configuration` chapter and the :ref:`config-route-schema` and :ref:`config-dynamic-route-schema` sections, there is a special ``RateLimitOptions`` object schema for routes:

.. code-block:: json

  "RateLimitOptions": {
    // rule, partition by
    "ClientIdHeader": "",
    "ClientWhitelist": [""],
    // management opts
    "EnableRateLimiting": true,
    "EnableHeaders": true,
    // algorithm
    "Limit": 1,
    "Period": "",
    "Wait": "",
    // extended opts
    "StatusCode": 1,
    "QuotaMessage": "",
    "KeyPrefix": ""
  }

Additionally, the :ref:`config-global-configuration-schema` allows configuring global *Rate Limiting* options.

  **Note 1**: The complete route-level ``RateLimitOptions`` schema, including all available properties, is defined in the C# `FileRateLimitByHeaderRule`_ class.
  The global ``RateLimitOptions`` schema includes an additional ``RouteKeys`` array option, which allows grouping routes to which the global options will apply (refer to the C# `FileGlobalRateLimitByHeaderRule`_ class for details).
  If the ``RouteKeys`` option is not defined in the global ``RateLimitOptions``, the global settings will apply to all routes.

  **Note 2**: You do not need to set all of these options due to default values, but the following rule options are required: ``Limit`` and ``Period``.
  If these required options are undefined and no global configuration is present, Ocelot will fail to start due to an internally generated validation error, which will be visible in the logs.

  **Note 3**: Several :ref:`deprecated options <rl-deprecated-options>` originating from version `24.0`_ and earlier (see `old schema`_) are retained for one release cycle.
  Both introduced and :ref:`deprecated options <rl-deprecated-options>` are detailed in the :ref:`rl-configuration` table below.

.. _rl-configuration:

Configuration
-------------

A complete configuration consists of both route-level and global *Rate Limiting*.
You can configure the following options in the ``GlobalConfiguration`` section of `ocelot.json`_:

.. code-block:: json

  "Routes": [
    {
      "Key": "R1",
      "RateLimitOptions": {
        "ClientWhitelist": ["ocelot-client1-preshared-key"],
        "Limit": 1000,
        "Period": "20s", // (milli)seconds, minutes, hours, days
        "Wait": "1.5m" // (milli)seconds, minutes, hours, days
        "StatusCode": 418, // I'm a teapot -> this is special status
        "QuotaMessage": "Out of coffee! Our bar can only serve up to {0} cups of coffee every {1}. In the meantime, why not grab some tea and relax for Retry-After seconds until we're ready to serve again?"
      }
    }
  ],
  "GlobalConfiguration": {
    "BaseUrl": "https://api.ocelot.net",
    "RateLimitOptions": {
      "RouteKeys": ["R1"], // if undefined or empty array, opts will apply to all routes
      "ClientIdHeader": "Oc-Client", // std (default) header name
      "Limit": 100,
      "Period": "30s", // ms, s, m, h, d
      "Wait": "1m", // ms, s, m, h, d
      "StatusCode": 429, // Too Many Requests -> standard status
      "QuotaMessage": "Ocelot API calls quota exceeded! Maximum admitted {0} per {1}.", // standard template with 2 parameters
      "KeyPrefix": "ocelot-rate-limiting" // for caching key
    }
  }

.. list-table::
  :widths: 25 75
  :header-rows: 1

  * - :ref:`Schema <rl-schema>` Option
    - Description
  * - ``ClientIdHeader``
    - Specifies the header used to identify clients, with "Oc-Client" set as the default.
  * - ``ClientWhitelist``
    - An array that contains the clients exempt from *rate limiting*.
  * - ``EnableRateLimiting``
    - Enables or disables rate limiting. Defaults to ``true`` (enabled).
  * - ``EnableHeaders``
    - Specifies whether the ``X-RateLimit-*`` and ``Retry-After`` headers are enabled. If undefined, defaults to ``true`` (enabled).
  * - ``Limit``
    - The maximum number of requests a client can make within a given time ``Period``.
  * - ``Period``
    - Rate limiting period (fixed window) can be expressed as milliseconds (1ms), as seconds (1s), minutes (1m), hours (1h), or days (1d).
      If the exact ``Limit`` of requests is reached (quota exceeded\*), the request is immediately blocked, and if ``Wait`` is defined, a waiting period begins.
  * - ``Wait``
    - Rate limiting wait window (no servicing period) can be expressed as milliseconds (1ms), as seconds (1s), minutes (1m), hours (1h), or days (1d).
      This option can have shorter or longer durations compared to the fixed window duration specified as ``Period``.
      The waiting interval either extends or shortens the Quota Exceeded period\*, which typically ends after the fixed window elapses.
  * - ``StatusCode``
    - The rejection status code returned during the Quota Exceeded period\*.
      Default value: 429 (`Too Many Requests`_).
  * - ``QuotaMessage``
    - Specifies the message displayed when the quota is exceeded.
      The value to be used as the formatter for the Quota Exceeded\* response message.
      If none specified the default will be informative.
  * - ``KeyPrefix``
    - The counter prefix, used to compose the rate limiting counter caching key to be used by the ``IRateLimitStorage`` service.
      Default value: "Ocelot.RateLimiting"

.. admonition:: "Quota Exceeded period" term

  The **Quota Exceeded period** refers to the ``Wait`` window, if defined, or the remaining duration of the fixed ``Period`` following the moment the request ``Limit`` is exceeded.
  During this time, the configured rejection ``StatusCode`` is returned, and the formatted ``QuotaMessage`` is written to the response body.
  To determine when this period ends, clients should inspect the ``Retry-After`` header, which provides a floating-point value indicating the number of seconds until the next allocated fixed window begins.
  The ``X-RateLimit-*`` headers are included in the response during the *Quota Exceeded period*, provided that headers are enabled via the ``EnableHeaders`` option.

.. _break: http://break.do

  **Note 1**: If the ``RouteKeys`` option is not defined or the array is empty in the global ``RateLimitOptions``, the global settings will apply to all routes.
  If the array contains route keys, it defines a single group of routes to which the global options apply.
  Routes excluded from this group must specify their own route-level ``RateLimitOptions``.

  **Note 2**: The string values for the ``Period`` and ``Wait`` options must contain a floating-point number followed by one of the supported time units: 'ms', 's', 'm', 'h', or 'd'.
  If no unit is specified, the value defaults to milliseconds. For example, "333.5" is interpreted as 333 milliseconds and 500 microseconds (equivalent to "333.5ms").
  The floating-point component may be omitted; for example, "10.0s" is equivalent to "10s".
  These values are parsed dynamically at runtime, so the required ``Period`` option in `ocelot.json`_ is validated early through fluent validation when the Ocelot app starts.
  If an invalid value is provided, the *Rate Limiting* middleware will throw a ``FormatException``, which is logged accordingly.

.. _rl-deprecated-options:

Deprecated options [#f2]_
^^^^^^^^^^^^^^^^^^^^^^^^^

.. warning::

  Here are the deprecated options from the `old schema`_:

  .. list-table::
    :widths: 30 70
    :header-rows: 1

    * - *Deprecated and Introduced Options*
      - *Description*
    * - ``DisableRateLimitHeaders`` and ``EnableHeaders``
      - Specifies whether the ``X-RateLimit-*`` and ``Retry-After`` headers are disabled.
    * - ``PeriodTimespan`` and ``Wait``
      - This parameter specifies the time, in **seconds**, after which a retry is allowed.
        During this interval, the ``QuotaExceededMessage`` will be included in the response, along with the corresponding ``HttpStatusCode``.
        Clients are encouraged to refer to the ``Retry-After`` header to determine when subsequent requests can be made.
    * - ``HttpStatusCode`` and ``StatusCode``
      - Specifies the HTTP status code returned during *rate limiting*, with a default value of **429** (`Too Many Requests`_).
    * - ``QuotaExceededMessage`` and ``QuotaMessage``
      - Specifies the message displayed when the quota is exceeded. This option is optional, and the default message is informative.
    * - ``RateLimitCounterPrefix`` and ``KeyPrefix``
      - Specifies the counter prefix used to construct the *rate limiting* counter cache key.

Notes
-----

.. note::

  1. Prior to version `24.1`_, global options were only accessible in the special :ref:`Dynamic Routing <routing-dynamic>` mode.
  Since version `24.1`_, global configuration has been available for both static and dynamic routes.
  As a team, we would consider the idea of implementing such a global configuration for aggregated routes.
  However, an aggregated route is essentially a combination of static routes.

  2. Global *rate limiting* options may not be practical as they apply limits to all routes.
  In a microservices architecture, it is unusual to enforce the same limitations across all routes.
  Configuring per-route *rate limiting* could offer a more tailored solution.
  However, global *rate limiting* can be logical if all routes share the same downstream hosts, thereby restricting the usage of a single service or a single product.

  3. The ``DisableRateLimitHeaders`` option is deprecated as of version `24.1`_.
  Use ``EnableHeaders`` instead, applying boolean value negation as needed.
  If ``DisableRateLimitHeaders`` is defined, it takes precedence; otherwise, ``EnableHeaders`` will be used.
  Do not define both options.
  This setting is retained for backward compatibility but is subject to change.
  Therefore, the ``DisableRateLimitHeaders`` option will be removed in the upcoming major release, version `25.0`_.
  The same applies to other :ref:`deprecated options <rl-deprecated-options>`.

  4. Ocelot's own *rate limiting* does not utilize built-in ASP.NET Core features, so it is not based on the "`Rate limiting middleware in ASP.NET Core`_" described in the :ref:`rl-roadmap` below.
  The Ocelot team believes that the ASP.NET Core rate limiting middleware enables global limitations through its rate-limiting policies.

.. _rl-algorithms:

Algorithms
----------

The currently implemented rate limiter algorithms in Ocelot are:

- **Fixed window**: Based on the ``Period`` option, without the ``Wait`` option (previously known as the deprecated ``PeriodTimespan``).
- **Hybrid fixed window**: The combination of ``Period`` and ``Wait`` enables fixed-window-like behavior with additional control over the duration and handling of the *"Quota Exceeded period"*.

Historically, Ocelot's rate limiting algorithm was a hybrid, combining the classic "fixed window" approach with a waiting no-service period.
Since version `24.1`_, the hybrid algorithm has been split into two distinct algorithms, allowing the classic "fixed window" to be used independently.

To understand the terminology, please refer to the Handy Articles listed at the beginning of this chapter.
For beginners, here is a quick link: `Announcing ASP.NET Core rate limiting algorithms`_.
For professionals, we recommend reading the official Microsoft Learn article "`Rate limiting middleware in ASP.NET Core`_", especially the `Rate Limiter Algorithms`_ section, and/or searching the internet for additional resources.

  **Note 1**: Ocelot's own rate limiter does not implement other classic algorithms such as "Sliding Window", "Token Bucket", or "Concurrency".
  However, these algorithms are outlined in the :ref:`rl-roadmap`.

  **Note 2**: Ocelot's own rate limiter does not manage concurrent HTTP requests via a queue.
  Therefore, all concurrency handling and decision-making should be implemented on the client side using classic retry patterns to ensure quality of service.
  The management strategy is deliberately simple: *First-In means First Wins*.
  If the first request acquires a virtual lease from the limiting quota and the quota is immediately exceeded, the second request will be rejected with a 429 `Too Many Requests`_ response.

.. _Announcing ASP.NET Core rate limiting algorithms: https://devblogs.microsoft.com/dotnet/announcing-rate-limiting-for-dotnet/#what-is-rate-limiting:~:text=There%20are%20multiple%20different%20rate%20limiting%20algorithms%20to%20control%20the%20flow%20of%20requests.
.. _Rate limiter algorithms: https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-9.0#rate-limiter-algorithms
.. _Rate limiting middleware in ASP.NET Core: https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit

Rules (Partitions)
------------------

.. _API Key partition: https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-9.0#by-api-key

Ocelot's rate limiting *rule* is a superset of the configuration options used to set up rate-limited access to a route.
It enables partitioned rate limiting by processing the following artifacts through distinct stages: the client's identifier, a dedicated partition counter (quota), rate limiter algorithms, and the quota-exceeded response behavior.

By Client's Header
^^^^^^^^^^^^^^^^^^

  | Class: `FileRateLimitByHeaderRule`_
  | JSON: :ref:`rl-schema`

Currently, Ocelot's own rate limiting middleware supports and processes only the *"By Client's Header"* rule (partition), commonly referred to as the "`API Key partition`_" in ASP.NET Core terminology.
Ocelot's rate limiting architecture provides dedicated subpartitions for each route, each with an independent counter for the rate limiter algorithm.
Therefore, when client traffic enters the Ocelot pipeline, the current request is processed as follows:

1. Ocelot identifies the route by matching the URL path to the upstream route path, and allows the rate limiting middleware to process the client as part of the route partition.
2. Ocelot's rate limiting middleware creates the client's identity based on the configured *"By Client's Header"* rule and assigns a dedicated rate limiter counter to that client.
3. The rate limiting middleware executes the configured rate limiter algorithm, specifically the (hybrid) fixed window. Refer to the currently implemented :ref:`rl-algorithms` for details.
4. If the quota is exceeded, the rate limiting middleware returns appropriate "Quota Exceeded period" artifacts in the response, such as the status code, body message, and headers including ``Retry-After``.

.. note::
  If the client is not successfully identified, the rate limiting middleware blocks the request with a `503 Service Unavailable`_ status and writes an appropriate error message to the response body.
  Possible reasons for an empty identity include a missing header or an invalid ``ClientIdHeader`` value, as explained in the warning below.
  Whitelisted clients (defined via the ``ClientWhitelist`` option) are processed without limitation.

.. warning::
  Ocelot's rate limiting middleware is not responsible for validating API keys, also known as client header values, to be read from the configured header (``ClientIdHeader`` option).
  Users and developers must register these header values as pre-shared API keys on Ocelot's side and ensure they are validated before handing control over to the ``RateLimitingMiddleware``.

  We recommend implementing a custom middleware to validate API keys (client header values) and injecting it into the Ocelot pipeline using the :doc:`../features/middlewareinjection` feature.
  Specifically, the ``PreErrorResponderMiddleware`` (position 3) should be overridden, as it is invoked before the ``RateLimitingMiddleware`` at position 10.
  A more advanced solution may involve using the ``SecurityMiddleware`` at position 7, but in this case, users must implement their own ``ISecurityPolicy`` service and replace it in the :doc:`../features/dependencyinjection` (DI) container.
  To understand the Ocelot pipeline and its middleware positions, refer to the ":ref:`mi-ocelot-pipeline-builder`" documentation.

.. _rl-roadmap:

Roadmap
-------

  | Feature label: `Rate Limiting`_
  | Development history: `Rate Limiting <https://github.com/ThreeMammals/Ocelot/pulls?q=is%3Apr+label%3A%22Rate+Limiting%22>`__ [#f3]_

- **Rules**: The Ocelot team is considering a redesign of the *Rate Limiting* feature in light of the "`Announcing Rate Limiting for .NET`_" article by Brennan Conroy, published on July 13th, 2022.

  .. note::
    Discover the new rate limiting functionality in ASP.NET Core:

    * The `RateLimiter Class <https://learn.microsoft.com/en-us/dotnet/api/system.threading.ratelimiting.ratelimiter>`_, available since ASP.NET Core 7.0
    * The `System.Threading.RateLimiting <https://www.nuget.org/packages/System.Threading.RateLimiting>`_ NuGet package
    * The `Rate limiting middleware in ASP.NET Core`_ article by Arvin Kahbazi, Maarten Balliauw, and Rick Anderson

  As of now, the decision has been made to retain Ocelot's own `RateLimitingMiddleware`_ and extend it with an additional rule that will reference the attached ASP.NET Core rate limiting policy.
  This new rule is highly likely to be delivered in version `25.0`_, following the opening of pull request `2188`_.

- **Algorithms**:
  In addition to the currently implemented hybrid "Fixed window" algorithm, which is built into Ocelot, the team plans to introduce other industry-standard algorithms, such as "Sliding window", "Token bucket", and "Concurrency, with priority given to "Sliding window" as the first.
  These lightweight algorithms should be easily configurable via JSON by end users who are not .NET developers, in order to avoid writing additional C# source code.
  Other interesting algorithms are welcome for discussion.

We encourage you to share your thoughts with us in the `Discussions <https://github.com/ThreeMammals/Ocelot/discussions>`_ of the repository. |octocat|
Filter the current discussions by the `Rate Limiting <https://github.com/ThreeMammals/Ocelot/discussions?discussions_q=label%3A%22Rate+Limiting%22>`__ label.

""""

.. [#f1] Historically, the *Rate Limiting* feature is one of Ocelot's oldest and first features. This feature was introduced in pull request `37`_ and it was initially released in version `1.3.2`_.
.. [#f2] Several :ref:`deprecated options <rl-deprecated-options>` originating from version `24.0`_ and earlier (see `old schema`_) are retained for one release cycle.
  They are likely to be removed in the upcoming major release, version `25.0`_, which will include a significant upgrade to the *Rate Limiting* feature (refer to the :ref:`rl-roadmap`).
  The Ocelot team plans to implement an automatic configuration upgrade mechanism to support backward compatibility.
  However, we recommend reviewing the updated schema and beginning to adopt the new options.
.. [#f3] Since pull request `37`_ and version `1.3.2`_, the Ocelot team has reviewed and redesigned the *Rate Limiting* feature.
  A fix for bug `1590`_ (pull request `1592`_) was released as part of version `23.3`_ to ensure stable behavior.
  Global :ref:`rl-configuration` support was introduced in pull request `2294`_ and delivered in version `24.1`_.

.. _Announcing Rate Limiting for .NET: https://devblogs.microsoft.com/dotnet/announcing-rate-limiting-for-dotnet/
.. _ocelot.json: https://github.com/ThreeMammals/Ocelot/blob/main//samples/Basic/ocelot.json
.. _article by @catcherwong: http://www.c-sharpcorner.com/article/building-api-gateway-using-ocelot-in-asp-net-core-rate-limiting-part-four/
.. _Too Many Requests: https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/429
.. _old schema: https://github.com/ThreeMammals/Ocelot/blob/24.0.0/src/Ocelot/Configuration/File/FileRateLimitOptions.cs
.. _RateLimitingMiddleware: https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20RateLimitingMiddleware&type=code

.. _37: https://github.com/ThreeMammals/Ocelot/pull/37
.. _1590: https://github.com/ThreeMammals/Ocelot/issues/1590
.. _1592: https://github.com/ThreeMammals/Ocelot/pull/1592
.. _2188: https://github.com/ThreeMammals/Ocelot/pull/2188
.. _2294: https://github.com/ThreeMammals/Ocelot/pull/2294
.. _1.3.2: https://github.com/ThreeMammals/Ocelot/releases/tag/1.3.2
.. _20.0: https://github.com/ThreeMammals/Ocelot/releases/tag/20.0.0
.. _23.3: https://github.com/ThreeMammals/Ocelot/releases/tag/23.3.0
.. _24.0: https://github.com/ThreeMammals/Ocelot/releases/tag/24.0.0
.. _24.1: https://github.com/ThreeMammals/Ocelot/releases/tag/24.1.0
.. _25.0: https://github.com/ThreeMammals/Ocelot/milestone/13

.. |octocat| image:: https://github.githubassets.com/images/icons/emoji/octocat.png
  :alt: octocat
  :height: 25
  :class: img-valign-middle
