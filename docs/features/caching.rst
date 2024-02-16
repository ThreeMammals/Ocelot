Caching
=======

Ocelot supports some very rudimentary caching at the moment provider by the `CacheManager <https://github.com/MichaCo/CacheManager>`_ project.
This is an amazing project that is solving a lot of caching problems. We would recommend using this package to cache with Ocelot. 

The following example shows how to add **CacheManager** to Ocelot so that you can do output caching. 

Install
-------

First of all, add the following `NuGet package <https://www.nuget.org/packages/Ocelot.Cache.CacheManager>`_:

.. code-block:: powershell

    Install-Package Ocelot.Cache.CacheManager

This will give you access to the Ocelot cache manager extension methods.

The second thing you need to do something like the following to your ``ConfigureServices`` method:

.. code-block:: csharp

    using Ocelot.Cache.CacheManager;

    ConfigureServices(services =>
    {
        services.AddOcelot()
            .AddCacheManager(x => x.WithDictionaryHandle());
    });

Configuration
-------------

Finally, in order to use caching on a route in your Route configuration add this setting:

.. code-block:: json

    "FileCacheOptions": {
      "TtlSeconds": 15,
      "Region": "europe-central",
      "Header": "Authorization"
    }

In this example **TtlSeconds** is set to 15 which means the cache will expire after 15 seconds.
The **Region** represents a region of caching. 

Additionally, if a header name is defined in the **Header** property, that header value is looked up by the key (header name) in the ``HttpRequest`` headers,
and if the header is found, its value will be included in caching key. This causes the cache to become invalid due to the header value changing.

If you look at the example `here <https://github.com/ThreeMammals/Ocelot/blob/main/test/Ocelot.ManualTest/Program.cs>`_ you can see how the cache manager is setup and then passed into the Ocelot ``AddCacheManager`` configuration method.
You can use any settings supported by the **CacheManager** package and just pass them in.

Anyway, Ocelot currently supports caching on the URL of the downstream service and setting a TTL in seconds to expire the cache. You can also clear the cache for a region by calling Ocelot's administration API.

Your Own Caching
----------------

If you want to add your own caching method, implement the following interfaces and register them in DI e.g.

.. code-block:: csharp

    services.AddSingleton<IOcelotCache<CachedResponse>, MyCache>();

* ``IOcelotCache<CachedResponse>`` this is for output caching.
* ``IOcelotCache<FileConfiguration>`` this is for caching the file configuration if you are calling something remote to get your config such as Consul.

Please dig into the Ocelot source code to find more.
We would really appreciate it if anyone wants to implement `Redis <https://redis.io/>`_, `Memcached <http://www.memcached.org/>`_ etc.
Please, open a new `Show and tell <https://github.com/ThreeMammals/Ocelot/discussions/categories/show-and-tell>`_ thread in `Discussions <https://github.com/ThreeMammals/Ocelot/discussions>`_ space of the repository.
