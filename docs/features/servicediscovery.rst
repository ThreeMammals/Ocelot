.. service-discovery:

Service Discovery
=================

Ocelot allows you to specify a *service discovery* provider and will use this to find the host and port for the downstream service to which Ocelot forwards the request.
At the moment this is only supported in the **GlobalConfiguration** section, which means the same *service discovery* provider will be used for all Routes for which you specify a ``ServiceName`` at Route level.

Consul
------

    | **Namespace**: `Ocelot.Provider.Consul <https://github.com/ThreeMammals/Ocelot/tree/develop/src/Ocelot.Provider.Consul>`_

The first thing you need to do is install `the NuGet package <https://www.nuget.org/packages/Ocelot.Provider.Consul>`_ that provides `Consul <https://www.consul.io/>`_ support in Ocelot.

.. code-block:: powershell

    Install-Package Ocelot.Provider.Consul

Then add the following to your ``ConfigureServices`` method:

.. code-block:: csharp

    ConfigureServices(services =>
    {
        services.AddOcelot()
            .AddConsul();
    });

Currently there are 2 types of Consul *service discovery* providers: ``Consul`` and ``PollConsul``.
The default provider is ``Consul``, which means that if ``ConsulProviderFactory`` cannot read, understand, or parse the **Type** property of the ``ServiceProviderConfiguration`` object, then a ``Consul`` provider instance is created by the factory.

Explore these types of providers and understand the differences in the subsections below.

Consul Provider Type
^^^^^^^^^^^^^^^^^^^^

    | **Class**: `Ocelot.Provider.Consul.Consul <https://github.com/search?q=repo%3AThreeMammals%2FOcelot+Consul&type=code>`_

The following is required in the `GlobalConfiguration <https://github.com/search?q=repo%3AThreeMammals%2FOcelot+%22FileGlobalConfiguration+GlobalConfiguration%22&type=code>`_.
The **ServiceDiscoveryProvider** property is required, and if you do not specify a host and port, the Consul default ones will be used.

Please note the `Scheme <https://github.com/search?q=repo%3AThreeMammals%2FOcelot+%22public+string+Scheme+%7B+get%3B+%7D%22+path%3A%2F%5Esrc%5C%2FOcelot%5C%2FConfiguration%5C%2F%2F&type=code>`_ option defaults to HTTP.
It was added in `PR 1154 <https://github.com/ThreeMammals/Ocelot/pull/1154>`_. It defaults to HTTP to not introduce a breaking change.

.. code-block:: json

    "ServiceDiscoveryProvider": {
        "Scheme": "https",
        "Host": "localhost",
        "Port": 8500,
        "Type": "Consul"
    }

In the future we can add a feature that allows Route specfic configuration. 

In order to tell Ocelot a Route is to use the *service discovery* provider for its host and port you must add the ServiceName and load balancer you wish to use when making requests downstream.
At the moment Ocelot has a `RoundRobin <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20RoundRobin&type=code>`_ 
and `LeastConnection <https://github.com/search?q=repo%3AThreeMammals%2FOcelot+LeastConnection&type=code>`_ algorithm you can use.
If no load balancer is specified Ocelot will not load balance requests.

.. code-block:: json

    {
        "DownstreamPathTemplate": "/api/posts/{postId}",
        "DownstreamScheme": "https",
        "UpstreamPathTemplate": "/posts/{postId}",
        "UpstreamHttpMethod": [ "Put" ],
        "ServiceName": "product",
        "LoadBalancerOptions": {
            "Type": "LeastConnection"
        },
    }

When this is set up Ocelot will lookup the downstream host and port from the *service discovery* provider and load balance requests across any available services.

PollConsul Provider Type
^^^^^^^^^^^^^^^^^^^^^^^^

    | **Class**: `Ocelot.Provider.Consul.PollConsul <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20PollConsul&type=code>`_

A lot of people have asked me to implement a feature where Ocelot *polls Consul* for latest service information rather than per request.
If you want to *poll Consul* for the latest services rather than per request (default behaviour) then you need to set the following configuration:

.. code-block:: json

    "ServiceDiscoveryProvider": {
        "Host": "localhost",
        "Port": 8500,
        "Type": "PollConsul",
        "PollingInterval": 100
    }

The polling interval is in milliseconds and tells Ocelot how often to call Consul for changes in service configuration.

Please note there are tradeoffs here. If you *poll Consul* it is possible Ocelot will not know if a service is down depending on your polling interval and you might get more errors than if you get the latest services per request. This really depends on how volatile your services are. I doubt it will matter for most people and polling may give a tiny performance improvement over calling Consul per request (as sidecar agent). If you are calling a remote Consul agent then polling will be a good performance improvement.

Service Definition
^^^^^^^^^^^^^^^^^^

Your services need to be added to Consul something like below (C# style but hopefully this make sense)...
The only important thing to note is not to add ``http`` or ``https`` to the Address field.
I have been contacted before about not accepting scheme in Address and accepting scheme in address.
After reading `this <https://developer.hashicorp.com/consul/docs/agent/config>`_ I don't think the scheme should be in there.

In C#

.. code-block:: csharp

    new AgentService()
    {
        Service = "some-service-name",
        Address = "localhost",
        Port = 8080,
        ID = "some-id",
    }

Or, in JSON

.. code-block:: json

    "Service": {
        "ID": "some-id",
        "Service": "some-service-name",
        "Address": "localhost",
        "Port": 8080
    }

ACL Token
^^^^^^^^^

If you are using `ACL <https://developer.hashicorp.com/consul/commands/acl/token>`_ with Consul, Ocelot supports adding the "X-Consul-Token" header.
In order so this to work you must add the additional property below:

.. code-block:: json

    "ServiceDiscoveryProvider": {
        "Host": "localhost",
        "Port": 8500,
        "Token": "footoken",
        "Type": "Consul"
    }

Ocelot will add this token to the Consul client that it uses to make requests and that is then used for every request.

Eureka
------

This feature was requested as part of `Issue 262 <https://github.com/ThreeMammals/Ocelot/issues/262>`_ . to add support for Netflix's Eureka service discovery provider. The main reason for this is it is a key part of  `Steeltoe <https://steeltoe.io/>`_ which is something to do with `Pivotal <https://pivotal.io/platform>`_! Anyway enough of the background.

The first thing you need to do is install the NuGet package that provides Eureka support in Ocelot.

.. code-block:: powershell

    Install-Package Ocelot.Provider.Eureka

Then add the following to your ConfigureServices method.

.. code-block:: csharp

    s.AddOcelot()
        .AddEureka();

Then in order to get this working add the following to ocelot.json..

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

Ocelot will now register all the necessary services when it starts up and if you have the json above will register itself with Eureka. One of the services polls Eureka every 30 seconds (default) and gets the latest service state and persists this in memory. When Ocelot asks for a given service it is retrieved from memory so performance is not a big problem. Please note that this code is provided by the Pivotal.Discovery.Client NuGet package so big thanks to them for all the hard work.

Ocelot will use the scheme (http/https) set in Eureka if these values are not provided in ocelot.json

Dynamic Routing
---------------

This feature was requested in `issue 340 <https://github.com/ThreeMammals/Ocelot/issues/340>`_. The idea is to enable dynamic routing when using a service discovery provider (see that section of the docs for more info). In this mode Ocelot will use the first segment of the upstream path to lookup the downstream service with the service discovery provider. 

An example of this would be calling Ocelot with a url like https://api.mywebsite.com/product/products. Ocelot will take the first segment of 
the path which is product and use it as a key to look up the service in Consul. If Consul returns a service Ocelot will request it on whatever host and port comes back from Consul plus the remaining path segments in this case products thus making the downstream call http://hostfromconsul:portfromconsul/products. Ocelot will apprend any query string to the downstream url as normal.

In order to enable dynamic routing you need to have 0 Routes in your config. At the moment you cannot mix dynamic and configuration Routes. In addition to this you need to specify the Service Discovery provider details as outlined above and the downstream http/https scheme as DownstreamScheme.

In addition to that you can set RateLimitOptions, QoSOptions, LoadBalancerOptions and HttpHandlerOptions, DownstreamScheme (You might want to call Ocelot on https but talk to private services over http) that will be applied to all of the dynamic Routes.

The config might look something like 

.. code-block:: json

    {
        "Routes": [],
        "Aggregates": [],
        "GlobalConfiguration": {
            "RequestIdKey": null,
            "ServiceDiscoveryProvider": {
                "Host": "localhost",
                "Port": 8500,
                "Type": "Consul",
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

Ocelot also allows you to set DynamicRoutes which lets you set rate limiting rules per downstream service. This is useful if you have for example a product and search service and you want to rate limit one more than the other. An example of this would be as follows.

.. code-block:: json

    {
        "DynamicRoutes": [
            {
            "ServiceName": "product",
            "RateLimitRule": {
                    "ClientWhitelist": [],
                    "EnableRateLimiting": true,
                    "Period": "1s",
                    "PeriodTimespan": 1000.0,
                    "Limit": 3
                }
            }
        ],
        "GlobalConfiguration": {
            "RequestIdKey": null,
            "ServiceDiscoveryProvider": {
                "Host": "localhost",
                "Port": 8523,
                "Type": "Consul"
            },
            "RateLimitOptions": {
                "ClientIdHeader": "ClientId",
                "QuotaExceededMessage": "",
                "RateLimitCounterPrefix": "",
                "DisableRateLimitHeaders": false,
                "HttpStatusCode": 428
            }
            "DownstreamScheme": "http",
        }
    }

This configuration means that if you have a request come into Ocelot on /product/* then dynamic routing will kick in and ocelot will use the rate limiting set against the product service in the DynamicRoutes section.

Please take a look through all of the docs to understand these options.

Custom Providers
----------------

Ocelot also allows you to create your own ServiceDiscovery implementation.
This is done by implementing the ``IServiceDiscoveryProvider`` interface, as shown in the following example:

.. code-block:: csharp

    public class MyServiceDiscoveryProvider : IServiceDiscoveryProvider
    {
        private readonly DownstreamRoute _downstreamRoute;
        
        public MyServiceDiscoveryProvider(DownstreamRoute downstreamRoute)
        {
            _downstreamRoute = downstreamRoute;
        }
       
        public async Task<List<Service>> Get()
        {
            var services = new List<Service>();
            //...
            //Add service(s) to the list matching the _downstreamRoute
            return services;
        }
    }

And set its class name as the provider type in **ocelot.json**:

.. code-block:: json

  "GlobalConfiguration": {
    "ServiceDiscoveryProvider": {
      "Type": "MyServiceDiscoveryProvider"
    }
  }
  
Finally, in the application's **ConfigureServices** method, register a ``ServiceDiscoveryFinderDelegate`` to initialize and return the provider:

.. code-block:: csharp

    ServiceDiscoveryFinderDelegate serviceDiscoveryFinder = (provider, config, route) =>
    {
        return new MyServiceDiscoveryProvider(route);
    };
    services.AddSingleton(serviceDiscoveryFinder);
    services.AddOcelot();

Custom Provider Sample
^^^^^^^^^^^^^^^^^^^^^^

In order to introduce a basic template for a custom Service Discovery provider, we've prepared a good sample:

    | **Link**: `samples <../../samples>`_ / `OcelotServiceDiscovery <../../samples/OcelotServiceDiscovery>`_
    | **Solution**: `Ocelot.Samples.ServiceDiscovery.sln <../../samples/OcelotServiceDiscovery/Ocelot.Samples.ServiceDiscovery.sln>`_

This solution contains the following projects:

- `ApiGateway <#apigateway>`_
- `DownstreamService <#downstreamservice>`_

This solution is ready for any deployment. All services are bound, meaning all ports and hosts are prepared for immediate use (running in Visual Studio).

All instructions for running this solution are in `README.md <../../samples/OcelotServiceDiscovery/README.md>`_.

DownstreamService
"""""""""""""""""

This project provides a single downstream service that can be reused across `ApiGateway <#apigateway>`_ routes.
It has multiple **launchSettings.json** profiles for your favorite launch and hosting scenarios: Visual Studio running sessions, Kestrel console hosting, and Docker deployments.

ApiGateway
""""""""""

This project includes a custom Service Discovery provider and it only has route(s) to `DownstreamService <#downstreamservice>`_ services in the **ocelot.json** file.
You can add more routes!

The main source code for the custom provider is in the `ServiceDiscovery <../../samples/OcelotServiceDiscovery/ApiGateway/ServiceDiscovery>`_ folder:
the ``MyServiceDiscoveryProvider`` and ``MyServiceDiscoveryProviderFactory`` classes. You are welcome to design and develop them!

Additionally, the cornerstone of this custom provider is the ``ConfigureServices`` method, where you can choose design and implementation options: simple or more complex:

.. code-block:: csharp

            builder.ConfigureServices(s =>
            {
                // Perform initialization from application configuration or hardcode/choose the best option.
                bool easyWay = true;

                if (easyWay)
                {
                    // Design #1. Define a custom finder delegate to instantiate a custom provider under the default factory, which is ServiceDiscoveryProviderFactory
                    s.AddSingleton<ServiceDiscoveryFinderDelegate>((serviceProvider, config, downstreamRoute)
                        => new MyServiceDiscoveryProvider(serviceProvider, config, downstreamRoute));
                }
                else
                {
                    // Design #2. Abstract from the default factory (ServiceDiscoveryProviderFactory) and from FinderDelegate,
                    // and create your own factory by implementing the IServiceDiscoveryProviderFactory interface.
                    s.RemoveAll<IServiceDiscoveryProviderFactory>();
                    s.AddSingleton<IServiceDiscoveryProviderFactory, MyServiceDiscoveryProviderFactory>();

                    // It will not be called, but it is necessary for internal validators, it is also a lifehack
                    s.AddSingleton<ServiceDiscoveryFinderDelegate>((serviceProvider, config, downstreamRoute) => null);
                }

                s.AddOcelot();
            });

The easy way, lite design means that you only design the provider class, and specify ``ServiceDiscoveryFinderDelegate`` object for default ``ServiceDiscoveryProviderFactory`` in Ocelot core.

A more complex design means that you design both provider and provider factory classes.
After this, you need to add the ``IServiceDiscoveryProviderFactory`` interface to the DI-container, removing the default registered ``ServiceDiscoveryProviderFactory`` class.
Note that in this case the Ocelot core will not use ``ServiceDiscoveryProviderFactory`` by default.
Additionally, you do not need to specify ``"Type": "MyServiceDiscoveryProvider"`` in the **ServiceDiscoveryProvider** properties of the **GlobalConfiguration** settings.
But you can leave this ``Type`` option for compatibility between both designs.
