.. service-discovery:

Service Discovery
=================

Ocelot allows you to specify a service discovery provider and will use this to find the host and port 
for the downstream service Ocelot is forwarding a request to. At the moment this is only supported in the
GlobalConfiguration section which means the same service discovery provider will be used for all ReRoutes
you specify a ServiceName for at ReRoute level. 

Consul
^^^^^^

The following is required in the GlobalConfiguration. The Provider is required and if you do not specify a host and port the Consul default
will be used.

.. code-block:: json

    "ServiceDiscoveryProvider": {
        "Host": "localhost",
        "Port": 9500
    }

In the future we can add a feature that allows ReRoute specfic configuration. 

In order to tell Ocelot a ReRoute is to use the service discovery provider for its host and port you must add the 
ServiceName, UseServiceDiscovery and load balancer you wish to use when making requests downstream. At the moment Ocelot has a RoundRobin
and LeastConnection algorithm you can use. If no load balancer is specified Ocelot will not load balance requests.

.. code-block:: json

    {
        "DownstreamPathTemplate": "/api/posts/{postId}",
        "DownstreamScheme": "https",
        "UpstreamPathTemplate": "/posts/{postId}",
        "UpstreamHttpMethod": [ "Put" ],
        "ServiceName": "product",
        "LoadBalancer": "LeastConnection",
        "UseServiceDiscovery": true
    }

When this is set up Ocelot will lookup the downstream host and port from the service discover provider and load balance requests across any available services.

ACL Token
---------

If you are using ACL with Consul Ocelot supports adding the X-Consul-Token header. In order so this to work you must add the additional property below.

.. code-block:: json

    "ServiceDiscoveryProvider": {
        "Host": "localhost",
        "Port": 9500,
        "Token": "footoken"
    }

Ocelot will add this token to the consul client that it uses to make requests and that is then used for every request.

Eureka
^^^^^^

This feature was requested as part of `Issue 262 <https://github.com/TomPallister/Ocelot/issue/262>`_ . to add support for Netflix's 
Eureka service discovery provider. The main reason for this is it is a key part of  `Steeltoe <https://steeltoe.io/>`_ which is something
to do with `Pivotal <https://pivotal.io/platform>`_! Anyway enough of the background.

In order to get this working add the following to ocelot.json..

.. code-block:: json

    "ServiceDiscoveryProvider": {
        "Type": "Eureka"
    }

And following the guide `Here <https://steeltoe.io/docs/steeltoe-discovery/>`_ you may also need to add some stuff to appsettings.json. For example the json below tells the steeltoe / pivotal services where to look for the service discovery server and if the service should register with it.

.. code-block:: json

    "eureka": {
        "client": {
        "serviceUrl": "http://localhost:8761/eureka/",
        "shouldRegisterWithEureka": false,
        "shouldFetchRegistry": true
        }
    }

I am told that if shouldRegisterWithEureka is false then shouldFetchRegistry will defaut to true so you don't need it explicitly but left it in there.

Ocelot will now register all the necessary services when it starts up and if you have the json above will register itself with 
Eureka. One of the services polls Eureka every 30 seconds (default) and gets the latest service state and persists this in memory.
When Ocelot asks for a given service it is retrieved from memory so performance is not a big problem. Please note that this code
is provided by the Pivotal.Discovery.Client NuGet package so big thanks to them for all the hard work.

Dynamic Routing
^^^^^^^^^^^^^^^

This feature was requested in `issue 340 <https://github.com/TomPallister/Ocelot/issue/340>`_. The idea is to enable dynamic routing when using 
a service discovery provider (see that section of the docs for more info). In this mode Ocelot will use the first segmentof the upstream path to lookup the
downstream service with the service discovery provider. 

An example of this would be calling ocelot with a url like https://api.mywebsite.com/product/products. Ocelot will take the first segment of 
the path which is product and use it as a key to look up the service in consul. If consul returns a service Ocelot will request it on whatever host and
port comes back from consul plus the remaining path segments in this case products thus making the downstream call http://hostfromconsul:portfromconsul/products. 
Ocelot will apprend any query string to the downstream url as normal.

In order to enable dynamic routing you need to have 0 ReRoutes in your config. At the moment you cannot mix dynamic and configuration ReRoutes. In addition to this you
need to specify the Service Discovery provider details as outlined above and the downstream http/https scheme as DownstreamScheme.

In addition to that you can set RateLimitOptions, QoSOptions, LoadBalancerOptions and HttpHandlerOptions, DownstreamScheme (You might want to call Ocelot on https but 
talk to private services over http) that will be applied to all of the dynamic ReRoutes.

The config might look something like 

.. code-block:: json

    {
        "ReRoutes": [],
        "Aggregates": [],
        "GlobalConfiguration": {
            "RequestIdKey": null,
            "ServiceDiscoveryProvider": {
                "Host": "localhost",
                "Port": 8510,
                "Type": null,
                "Token": null,
                "ConfigurationKey": null
            },
            "RateLimitOptions": {
                "ClientIdHeader": "ClientId",
                "QuotaExceededMessage": null,
                "RateLimitCounterPrefix": "ocelot",
                "DisableRateLimitHeaders": false,
                "HttpStatusCode": 429
            },
            "QoSOptions": {
                "ExceptionsAllowedBeforeBreaking": 0,
                "DurationOfBreak": 0,
                "TimeoutValue": 0
            },
            "BaseUrl": null,
                "LoadBalancerOptions": {
                "Type": "LeastConnection",
                "Key": null,
                "Expiry": 0
            },
            "DownstreamScheme": "http",
            "HttpHandlerOptions": {
                "AllowAutoRedirect": false,
                "UseCookieContainer": false,
                "UseTracing": false
            }
        }
    }

Please take a look through all of the docs to understand these options.