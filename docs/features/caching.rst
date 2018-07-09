Caching
=======

Ocelot supports some very rudimentary caching at the moment provider by 
the `CacheManager <https://github.com/MichaCo/CacheManager>`_ project. This is an amazing project
that is solving a lot of caching problems. I would reccomend using this package to 
cache with Ocelot. 

The following example shows how to add CacheManager to Ocelot so that you can do output caching. The first thing you need to do is add the following to your ConfigureServices..

.. code-block:: csharp

    s.AddOcelot()
        .AddCacheManager(x =>
        {
            x.WithDictionaryHandle();
        })

In order to use caching on a route in your ReRoute configuration add this setting.

.. code-block:: json

    "FileCacheOptions": { "TtlSeconds": 15, "Region": "somename" }

In this example ttl seconds is set to 15 which means the cache will expire after 15 seconds.

If you look at the example `here <https://github.com/TomPallister/Ocelot/blob/develop/test/Ocelot.ManualTest/Program.cs>`_ you can see how the cache manager is setup and then passed into the Ocelot 
AddOcelotOutputCaching configuration method. You can use any settings supported by 
the CacheManager package and just pass them in.

Anyway Ocelot currently supports caching on the URL of the downstream service 
and setting a TTL in seconds to expire the cache. You can also clear the cache for a region
by calling Ocelot's administration API.

