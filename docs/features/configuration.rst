Configuration
============

An example configuration can be found `here <https://github.com/TomPallister/Ocelot/blob/develop/test/Ocelot.ManualTest/configuration.json>`_.
There are two sections to the configuration. An array of ReRoutes and a GlobalConfiguration. 
The ReRoutes are the objects that tell Ocelot how to treat an upstream request. The Global 
configuration is a bit hacky and allows overrides of ReRoute specific settings. It's useful
if you don't want to manage lots of ReRoute specific settings.

.. code-block:: json

    {
        "ReRoutes": [],
        "GlobalConfiguration": {}
    }

Here is an example ReRoute configuration, You don't need to set all of these things but this is everything that is available at the moment:

.. code-block:: json

  {
            "DownstreamPathTemplate": "/",
            "UpstreamPathTemplate": "/",
            "UpstreamHttpMethod": [
                "Get"
            ],
            "AddHeadersToRequest": {},
            "AddClaimsToRequest": {},
            "RouteClaimsRequirement": {},
            "AddQueriesToRequest": {},
            "RequestIdKey": "",
            "FileCacheOptions": {
                "TtlSeconds": 0,
                "Region": ""
            },
            "ReRouteIsCaseSensitive": false,
            "ServiceName": "",
            "DownstreamScheme": "http",
            "DownstreamHostAndPorts": [
                {
                    "Host": "localhost",
                    "Port": 51876,
                }
            ],
            "QoSOptions": {
                "ExceptionsAllowedBeforeBreaking": 0,
                "DurationOfBreak": 0,
                "TimeoutValue": 0
            },
            "LoadBalancer": "",
            "RateLimitOptions": {
                "ClientWhitelist": [],
                "EnableRateLimiting": false,
                "Period": "",
                "PeriodTimespan": 0,
                "Limit": 0
            },
            "AuthenticationOptions": {
                "AuthenticationProviderKey": "",
                "AllowedScopes": []
            },
            "HttpHandlerOptions": {
                "AllowAutoRedirect": true,
                "UseCookieContainer": true,
                "UseTracing": true
            },
            "UseServiceDiscovery": false
        }

More information on how to use these options is below..

Follow Redirects / Use CookieContainer 
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Use HttpHandlerOptions in ReRoute configuration to set up HttpHandler behavior:

1. AllowAutoRedirect is a value that indicates whether the request should follow redirection responses. Set it true if the request should automatically 
follow redirection responses from the Downstream resource; otherwise false. The default value is false.
2. UseCookieContainer is a value that indicates whether the handler uses the CookieContainer 
property to store server cookies and uses these cookies when sending requests. The default value is false. Please note
that if you are using the CookieContainer Ocelot caches the HttpClient for each downstream service. This means that all requests
to that DownstreamService will share the same cookies. `Issue 274 <https://github.com/ThreeMammals/Ocelot/issues/274>`_ was created because a user
noticed that the cookies were being shared. I tried to think of a nice way to handle this but I think it is impossible. If you don't cache the clients
that means each request gets a new client and therefore a new cookie container. If you clear the cookies from the cached client container you get race conditions due to inflight
requests. This would also mean that subsequent requests dont use the cookies from the previous response! All in all not a great situation. I would avoid setting 
UseCookieContainer to true unless you have a really really good reason. Just look at your response headers and forward the cookies back with your next request! 

Multiple environments
^^^^^^^^^^^^^^^^^^^^^

Like any other asp.net core project Ocelot supports configuration file names such as configuration.dev.json, configuration.test.json etc. In order to implement this add the following 
to you 

.. code-block:: csharp

        .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config
                        .SetBasePath(hostingContext.HostingEnvironment.ContentRootPath)
                        .AddJsonFile("appsettings.json", true, true)
                        .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", true, true)
                        .AddJsonFile("configuration.json")
                        .AddJsonFile($"configuration.{hostingContext.HostingEnvironment.EnvironmentName}.json")
                        .AddEnvironmentVariables();
                })

Ocelot should now use the environment specific configuration and fall back to configuration.json if there isnt one.

You also need to set the corresponding environment variable which is ASPNETCORE_ENVIRONMENT. More info on this can be found in the `asp.net core docs <https://docs.microsoft.com/en-us/aspnet/core/fundamentals/environments>`_.

Store configuration in consul
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

If you add the following when you register your services Ocelot will attempt to store and retrieve its configuration in consul KV store.

.. code-block:: csharp

 services
    .AddOcelot()
    .AddStoreOcelotConfigurationInConsul();

You also need to add the following to your configuration.json. This is how Ocelot
finds your Consul agent and interacts to load and store the configuration from Consul.

.. code-block:: json

    "GlobalConfiguration": {
        "ServiceDiscoveryProvider": {
            "Host": "localhost",
            "Port": 9500
        }
    }

I decided to create this feature after working on the raft consensus algorithm and finding out its super hard. Why not take advantage of the fact Consul already gives you this! 
I guess it means if you want to use Ocelot to its fullest you take on Consul as a dependency for now.

This feature has a 3 second ttl cache before making a new request to your local consul agent.
