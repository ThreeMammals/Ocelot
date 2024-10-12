Rate Limiting
=============

`What's rate limiting? <https://www.bing.com/search?q=Rate+Limiting>`_

* `Rate limiting | Wikipedia <https://en.wikipedia.org/wiki/Rate_limiting>`_ 
* `Rate Limiting pattern | Azure Architecture Center | Microsoft Learn <https://learn.microsoft.com/en-us/azure/architecture/patterns/rate-limiting-pattern>`_
* `Rate Limiting | Ask Google <https://www.google.com/search?q=Rate+Limiting>`_

Ocelot Own Implementation
-------------------------

Ocelot provides *rate limiting* for upstream requests to prevent downstream services from becoming overwhelmed. [#f1]_

Rate Limit by Client's Header
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

To implement *rate limiting* for a Route, you need to incorporate the following JSON configuration:

.. code-block:: json

  "RateLimitOptions": {
    "ClientWhitelist": [], // array of strings
    "EnableRateLimiting": true,
    "Period": "1s", // seconds, minutes, hours, days
    "PeriodTimespan": 1, // only seconds
    "Limit": 1
  }

* ``ClientWhitelist``: An array containing the whitelisted clients. Clients listed here will be exempt from rate limiting.
  For more information on the ``ClientIdHeader`` option, refer to the :ref:`rl-global-configuration` section.
* ``EnableRateLimiting``: This setting enables rate limiting on endpoints.
* ``Period``: This parameter defines the duration for which the limit is applicable, such as ``1s`` (seconds), ``5m`` (minutes), ``1h`` (hours), and ``1d`` (days).
  If you reach the exact ``Limit`` of requests, the excess occurs immediately, and the ``PeriodTimespan`` begins.
  You must wait for the ``PeriodTimespan`` duration to pass before making another request.
  Should you exceed the number of requests within the period more than the ``Limit`` permits, the ``QuotaExceededMessage`` will appear in the response, accompanied by the ``HttpStatusCode``.
* ``PeriodTimespan``: This parameter indicates the time in **seconds** after which a retry is permissible.
  During this interval, the ``QuotaExceededMessage`` will appear in the response, accompanied by an ``HttpStatusCode``.
  Clients are advised to consult the ``Retry-After`` header to determine the timing of subsequent requests.
* ``Limit``: This parameter defines the upper limit of requests a client is allowed to make within a specified ``Period``.

.. _rl-global-configuration:

Global Configuration
^^^^^^^^^^^^^^^^^^^^

  Global options are only accessible in the special :ref:`routing-dynamic` mode.

You can set the following in the ``GlobalConfiguration`` section of `ocelot.json`_:

.. code-block:: json

  "GlobalConfiguration": {
    "BaseUrl": "https://api.mybusiness.com",
    "RateLimitOptions": {
      "DisableRateLimitHeaders": false,
      "QuotaExceededMessage": "Customize Tips!",
      "HttpStatusCode": 418, // I'm a teapot
      "ClientIdHeader": "MyRateLimiting"
    }
  }


.. list-table::
    :widths: 35 65
    :header-rows: 1

    * - *Property*
      - *Description*
    * - ``DisableRateLimitHeaders``
      - Determines if the ``X-Rate-Limit`` and ``Retry-After`` headers are disabled
    * - ``QuotaExceededMessage``
      - Defines the message displayed when the quota is exceeded. It is optional and the default message is informative.
    * - ``HttpStatusCode``
      - Indicates the HTTP status code returned during *rate limiting*. The default value is **429** (`Too Many Requests`_).
    * - ``ClientIdHeader``
      - Specifies the header used to identify clients, with ``ClientId`` as the default.

Notes
"""""

1. Global ``RateLimitOptions`` are supported when the :ref:`sd-dynamic-routing` feature is configured with :doc:`../features/servicediscovery`.
   Hence, if :doc:`../features/servicediscovery` is not set up, only the options for static routes need to be defined to impose limitations at the route level.
2. Global *Rate Limiting* options may not be practical because they impose limits on all routes.
   It's reasonable to assert that in a Microservices architecture, it's an unusual approach to apply the same limitations to all routes.
   Configuring per-route limiting could be a more tailored solution.
   Global *Rate Limiting* is logical if all routes share the same downstream hosts, thus limiting the usage of a single service.
3. *Rate Limiting* is now built-in with ASP.NET Core 7, as discussed in the following topic below.
   Our team holds the view that the ASP.NET ``RateLimiter`` enables global limitations through its rate limiting policies.

Future and ASP.NET Implementation
---------------------------------

The Ocelot team is contemplating a redesign of the *Rate Limiting* feature following the `Announcing Rate Limiting for .NET`_ by Brennan Conroy on July 13th, 2022.
Currently, no decision has been made, and the previous version of the feature remains part of the `20.0`_ release for .NET 7. [#f2]_

Discover the new features in the ASP.NET Core 7.0 release:

* The `RateLimiter Class <https://learn.microsoft.com/en-us/dotnet/api/system.threading.ratelimiting.ratelimiter>`_, available since ASP.NET Core 7.0
* The `System.Threading.RateLimiting <https://www.nuget.org/packages/System.Threading.RateLimiting>`_ NuGet package
* The `Rate limiting middleware in ASP.NET Core <https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit>`_ article by Arvin Kahbazi, Maarten Balliauw, and Rick Anderson

While it makes sense to retain the old implementation as a built-in feature of Ocelot, we are planning to transition to the new Rate Limiter from the ``Microsoft.AspNetCore.RateLimiting`` namespace.

We invite you to share your thoughts with us in the `Discussions <https://github.com/ThreeMammals/Ocelot/discussions>`_ space of the repository. |octocat|

""""

.. [#f1] Historically, the *"Ocelot Own Rate Limiting"* feature is one of the oldest and first features of Ocelot. This feature was delivered in PR `37`_ by `@geffzhang`_ on GitHub. Many thanks! It was initially released in version `1.3.2`_. The authors were inspired by `@catcherwong article`_ to write this documentation.
.. [#f2] Since PR `37`_ and version `1.3.2`_, the Ocelot team has reviewed and redesigned the feature to provide stable behavior. The fix for bug `1590`_ (PR `1592`_) was released as part of version `23.3`_.

.. _Announcing Rate Limiting for .NET: https://devblogs.microsoft.com/dotnet/announcing-rate-limiting-for-dotnet/
.. _ocelot.json: https://github.com/ThreeMammals/Ocelot/blob/main/test/Ocelot.ManualTest/ocelot.json
.. _@geffzhang: https://github.com/ThreeMammals/Ocelot/commits?author=geffzhang
.. _@catcherwong article: http://www.c-sharpcorner.com/article/building-api-gateway-using-ocelot-in-asp-net-core-rate-limiting-part-four/
.. _Too Many Requests: https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/429
.. _37: https://github.com/ThreeMammals/Ocelot/pull/37
.. _1590: https://github.com/ThreeMammals/Ocelot/issues/1590
.. _1592: https://github.com/ThreeMammals/Ocelot/pull/1592
.. _1.3.2: https://github.com/ThreeMammals/Ocelot/releases/tag/1.3.2
.. _20.0: https://github.com/ThreeMammals/Ocelot/releases/tag/20.0.0
.. _23.3: https://github.com/ThreeMammals/Ocelot/releases/tag/23.3.0
.. |octocat| image:: https://github.githubassets.com/images/icons/emoji/octocat.png
  :alt: octocat
  :width: 23
