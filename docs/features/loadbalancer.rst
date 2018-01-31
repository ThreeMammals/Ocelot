Load Balancer
=============

Ocelot can load balance across available downstream services for each ReRoute. This means you can scale your downstream services and Ocelot can use them effectively.

The type of load balancer available are:
    
    LeastConnection - tracks which services are dealing with requests and sends new requests to service with least existing requests. The algorythm state is not distributed across a cluster of Ocelot's.

    RoundRobin - loops through available services and sends requests. The algorythm state is not distributed across a cluster of Ocelot's.
    
    NoLoadBalancer - takes the first available service from config or service discovery.

You must choose in your configuration which load balancer to use.

Configuration
^^^^^^^^^^^^^

The following shows how to set up multiple downstream services for a ReRoute using configuration.json and then select the LeadConnection load balancer. This is the simplest way to get load balancing set up.

.. code-block:: json

    {
        "DownstreamPathTemplate": "/api/posts/{postId}",
        "DownstreamScheme": "https",
        "DownstreamHostAndPorts": [
                {
                    "Host": "10.0.1.10",
                    "Port": 5000,
                },
                {
                    "Host": "10.0.1.11",
                    "Port": 5000,
                }
            ],
        "UpstreamPathTemplate": "/posts/{postId}",
        "LoadBalancer": "LeastConnection",
        "UpstreamHttpMethod": [ "Put", "Delete" ]
    }


Service Discovery
^^^^^^^^^^^^^^^^^

The following shows how to set up a ReRoute using service discovery then select the LeadConnection load balancer.

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

When this is set up Ocelot will lookup the downstream host and port from the service discover provider and load balance requests across any available services. If you add and remove services from the 
service discovery provider (consul) then Ocelot should respect this and stop calling services that have been removed and start calling services that have been added.