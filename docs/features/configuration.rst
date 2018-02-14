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
- _AllowAutoRedirect_ is a value that indicates whether the request should follow redirection responses.
Set it true if the request should automatically follow redirection responses from the Downstream resource; otherwise false. The default value is true.
- _UseCookieContainer_ is a value that indicates whether the handler uses the CookieContainer property to store server cookies and uses these cookies when sending requests.
The default value is true.

Store configuration in consul
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

If you add the following when you register your services Ocelot will attempt to store and retrieve its configuration in consul KV store.

.. code-block:: csharp

 services
    .AddOcelot(Configuration)
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