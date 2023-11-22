Rate Limiting
=============

Ocelot Own Implementation
-------------------------

Ocelot supports rate limiting of upstream requests so that your downstream services do not become overloaded.

The authors of this feature were inspired by `@catcherwong article <http://www.c-sharpcorner.com/article/building-api-gateway-using-ocelot-in-asp-net-core-rate-limiting-part-four/>`_ to finally write this documentation.
This feature was added by `@geffzhang <https://github.com/ThreeMammals/Ocelot/commits?author=geffzhang>`_ on GitHub! Thanks very much!

To get rate limiting working for a Route you need to add the following JSON to it: 

.. code-block:: json

  "RateLimitOptions": {
    "ClientWhitelist": [],
    "EnableRateLimiting": true,
    "Period": "1s",
    "PeriodTimespan": 1,
    "Limit": 1
  }

* **ClientWhitelist** - This is an array that contains the whitelist of the client.
  It means that the client in this array will not be affected by the rate limiting.
* **EnableRateLimiting** - This value specifies enable endpoint rate limiting.
* **Period** - This value specifies the period that the limit applies to, such as ``1s``, ``5m``, ``1h``, ``1d`` and so on.
  If you make more requests in the period than the limit allows then you need to wait for **PeriodTimespan** to elapse before you make another request.
* **PeriodTimespan** - This value specifies that we can retry after a certain number of seconds.
* **Limit** - This value specifies the maximum number of requests that a client can make in a defined period.

You can also set the following in the **GlobalConfiguration** part of **ocelot.json**:

.. code-block:: json

  "GlobalConfiguration": {
    "BaseUrl": "https://api.mybusiness.com",
    "RateLimitOptions": {
      "DisableRateLimitHeaders": false,
      "QuotaExceededMessage": "Customize Tips!",
      "HttpStatusCode": 123,
      "ClientIdHeader": "Test"
    }
  }

* **DisableRateLimitHeaders** - This value specifies whether ``X-Rate-Limit`` and ``Retry-After`` headers are disabled.
* **QuotaExceededMessage** - This value specifies the exceeded message.
* **HttpStatusCode** - This value specifies the returned HTTP status code when rate limiting occurs.
* **ClientIdHeader** - Allows you to specifiy the header that should be used to identify clients. By default it is ``ClientId``

Future and ASP.NET Core Implementation
--------------------------------------

The Ocelot team considers to redesign *Rate Limiting* feature,
because of `Announcing Rate Limiting for .NET <https://devblogs.microsoft.com/dotnet/announcing-rate-limiting-for-dotnet/>`_ by Brennan Conroy on July 13th, 2022.
There is no decision at the moment, and the old version of the feature is included as a part of release `20.0 <https://github.com/ThreeMammals/Ocelot/releases/tag/20.0.0>`_ for .NET 7.

See more about new feature being added into ASP.NET Core 7.0 release:

* `RateLimiter Class <https://learn.microsoft.com/en-us/dotnet/api/system.threading.ratelimiting.ratelimiter>`_, since ASP.NET Core	**7.0**
* `System.Threading.RateLimiting <https://www.nuget.org/packages/System.Threading.RateLimiting>`_ NuGet package
* `Rate limiting middleware in ASP.NET Core <https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit>`_ article by Arvin Kahbazi, Maarten Balliauw, and Rick Anderson

However, it makes sense to keep the old implementation as a Ocelot built-in native feature, but we are going to migrate to the new Rate Limiter from ``Microsoft.AspNetCore.RateLimiting`` namespace.

.. |octocat| image:: https://github.githubassets.com/images/icons/emoji/octocat.png
  :alt: octocat
  :width: 23

Please, share your opinion to us in the `Discussions <https://github.com/ThreeMammals/Ocelot/discussions>`_ space of the repository. |octocat|
