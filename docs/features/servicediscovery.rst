Service Discovery
=================

Ocelot allows you to specify a service discovery provider and will use this to find the host and port 
for the downstream service Ocelot is forwarding a request to. At the moment this is only supported in the
GlobalConfiguration section which means the same service discovery provider will be used for all ReRoutes
you specify a ServiceName for at ReRoute level. 

At the moment the only supported service discovery provider is Consul. The following is required in the 
GlobalConfiguration. The Provider is required and if you do not specify a host and port the Consul default
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
        "UseServiceDiscovery": false
    }

When this is set up Ocelot will lookup the downstream host and port from the service discover provider and load balance
requests across any available services.