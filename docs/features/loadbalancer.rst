Load Balancer
=============

Ocelot can load balance across available downstream services for each ReRoute. This means you can scale your downstream services and Ocelot can use them effectively.

The type of load balancer available are:
    
    LeastConnection - tracks which services are dealing with requests and sends new requests to service with least existing requests. The algorythm state is not distributed across a cluster of Ocelot's.

    RoundRobin - loops through available services and sends requests. The algorythm state is not distributed across a cluster of Ocelot's.
    
    NoLoadBalancer - takes the first available service from config or service discovery.

    CookieStickySessions - uses a cookie to stick all requests to a specific server. More info below.

You must choose in your configuration which load balancer to use.

Configuration
^^^^^^^^^^^^^

The following shows how to set up multiple downstream services for a ReRoute using ocelot.json and then select the LeastConnection load balancer. This is the simplest way to get load balancing set up.

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
        "LoadBalancerOptions": {
            "Type": "LeastConnection"
        },
        "UpstreamHttpMethod": [ "Put", "Delete" ]
    }


Service Discovery
^^^^^^^^^^^^^^^^^

The following shows how to set up a ReRoute using service discovery then select the LeastConnection load balancer.

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

When this is set up Ocelot will lookup the downstream host and port from the service discover provider and load balance requests across any available services. If you add and remove services from the 
service discovery provider (consul) then Ocelot should respect this and stop calling services that have been removed and start calling services that have been added.

CookieStickySessions
^^^^^^^^^^^^^^^^^^^^

I've implemented a really basic sticky session type of load balancer. The scenario it is meant to support is you have a bunch of downstream 
servers that don't share session state so if you get more than one request for one of these servers then it should go to the same box each 
time or the session state might be incorrect for the given user. This feature was requested in `Issue #322 <https://github.com/ThreeMammals/Ocelot/issues/322>`_
though what the user wants is more complicated than just sticky sessions :) anyway I thought this would be a nice feature to have!

In order to set up CookieStickySessions load balancer you need to do something like the following.

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
        "LoadBalancerOptions": {
            "Type": "CookieStickySessions",
            "Key": "ASP.NET_SessionId",
            "Expiry": 1800000
        },
        "UpstreamHttpMethod": [ "Put", "Delete" ]
    }

The LoadBalancerOptions are Type this needs to be CookieStickySessions, Key this is the key of the cookie you 
wish to use for the sticky sessions, Expiry this is how long in milliseconds you want to the session to be stuck for. Remember this 
refreshes on every request which is meant to mimick how sessions work usually.

If you have multiple ReRoutes with the same LoadBalancerOptions then all of those ReRoutes will use the same load balancer for there 
subsequent requests. This means the sessions will be stuck across ReRoutes.

Please note that if you give more than one DownstreamHostAndPort or you are using a Service Discovery provider such as Consul 
and this returns more than one service then CookieStickySessions uses round robin to select the next server. This is hard coded at the 
moment but could be changed.
