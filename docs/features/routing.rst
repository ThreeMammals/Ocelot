Routing
=======

Ocelot's primary functionality is to take incomeing http requests and forward them on
to a downstream service. At the moment in the form of another http request (in the future
this could be any transport mechanism). 

Ocelot's describes the routing of one request to another as a ReRoute. In order to get 
anything working in Ocelot you need to set up a ReRoute in the configuration.

.. code-block:: json

    {
        "ReRoutes": [
        ]
    }

In order to set up a ReRoute you need to add one to the json array called ReRoutes like
the following.

.. code-block:: json

    {
        "DownstreamPathTemplate": "/api/posts/{postId}",
        "DownstreamScheme": "https",
        "DownstreamHostAndPorts": [
                {
                    "Host": "localhost",
                    "Port": 80,
                }
            ],
        "UpstreamPathTemplate": "/posts/{postId}",
        "UpstreamHttpMethod": [ "Put", "Delete" ]
    }

The DownstreamPathTemplate, Scheme and DownstreamHostAndPorts make the URL that this request will be forwarded to. 

DownstreamHostAndPorts is an array that contains the host and port of any downstream services that you wish to forward requests to. Usually this will just contain one entry but sometimes you might want to load balance
requests to your downstream services and Ocelot let's you add more than one entry and then select a load balancer.

The UpstreamPathTemplate is the URL that Ocelot will use to identity which DownstreamPathTemplate to use for a given request. Finally the UpstreamHttpMethod is used so
Ocelot can distinguish between requests to the same URL and is obviously needed to work :)

You can set a specific list of HTTP Methods or set an empty list to allow any of them. In Ocelot you can add placeholders for variables to your Templates in the form of {something}.
The placeholder needs to be in both the DownstreamPathTemplate and UpstreamPathTemplate. If it is Ocelot will attempt to replace the placeholder with the correct variable value from the Upstream URL when the request comes in.

You can also do a catch all type of ReRoute e.g. 

.. code-block:: json

    {
        "DownstreamPathTemplate": "/api/{everything}",
        "DownstreamScheme": "https",
        "DownstreamHostAndPorts": [
                {
                    "Host": "localhost",
                    "Port": 80,
                }
            ],
        "UpstreamPathTemplate": "/{everything}",
        "UpstreamHttpMethod": [ "Get", "Post" ]
    }

This will forward any path + query string combinations to the downstream service after the path /api.

At the moment without any configuration Ocelot will default to all ReRoutes being case insensitive.
In order to change this you can specify on a per ReRoute basis the following setting.

.. code-block:: json

    "ReRouteIsCaseSensitive": true

This means that when Ocelot tries to match the incoming upstream url with an upstream template the
evaluation will be case sensitive. This setting defaults to false so only set it if you want 
the ReRoute to be case sensitive is my advice!

Catch All
^^^^^^^^^

Ocelot's routing also supports a catch all style routing where the user can specify that they want to match all traffic if you set up your config like below the request will be proxied straight through (it doesnt have to be url any placeholder name will work). 

.. code-block:: json

    {
        "DownstreamPathTemplate": "/{url}",
        "DownstreamScheme": "https",
        "DownstreamHostAndPorts": [
                {
                    "Host": "localhost",
                    "Port": 80,
                }
            ],
        "UpstreamPathTemplate": "/{url}",
        "UpstreamHttpMethod": [ "Get" ]
    }

The catch all has a lower priority than any other ReRoute. If you also have the ReRoute below in your config then Ocelot would match it before the catch all. 

.. code-block:: json

    {
        "DownstreamPathTemplate": "/",
        "DownstreamScheme": "https",
        "DownstreamHostAndPorts": [
                {
                    "Host": "10.0.10.1",
                    "Port": 80,
                }
            ],
        "UpstreamPathTemplate": "/",
        "UpstreamHttpMethod": [ "Get" ]
    }

Upstream Host 
^^^^^^^^^^^^^

This feature allows you to have ReRoutes based on the upstream host. This works by looking at the host header the client has used and then using this as part of the information we use to identify a ReRoute.

In order to use this feature please add the following to your config.

.. code-block:: json

    {
        "DownstreamPathTemplate": "/",
        "DownstreamScheme": "https",
        "DownstreamHostAndPorts": [
                {
                    "Host": "10.0.10.1",
                    "Port": 80,
                }
            ],
        "UpstreamPathTemplate": "/",
        "UpstreamHttpMethod": [ "Get" ],
        "UpstreamHost": "somedomain.com"
    }

The ReRoute above will only be matched when the host header value is somedomain.com.

If you do not set UpstreamHost on a ReRoue then any host header can match it. This is basically a catch all and 
preservers existing functionality at the time of building the feature. This means that if you have two ReRoutes that are the same apart from the UpstreamHost where one is null and the other set. Ocelot will favour the one that has been set. 

This feature was requested as part of `Issue 216 <https://github.com/TomPallister/Ocelot/pull/216>`_ .

Priority
^^^^^^^^

In `Issue 270 <https://github.com/TomPallister/Ocelot/pull/270>`_ I finally decided to expose the ReRoute priority in 
ocelot.json. This means you can decide in what order you want your ReRoutes to match the Upstream HttpRequest.

In order to get this working add the following to a ReRoute in ocelot.json, 0 is just an example value here but will explain below.

.. code-block:: json

    {
        "Priority": 0
    }

0 is the lowest priority, Ocelot will always use 0 for /{catchAll} ReRoutes and this is still hardcoded. After that you are free
to set any priority you wish.

e.g. you could have

.. code-block:: json

    {
        "UpstreamPathTemplate": "/goods/{catchAll}"
        "Priority": 0
    }

and 

.. code-block:: json

    {
        "UpstreamPathTemplate": "/goods/delete"
        "Priority": 1
    }

In the example above if you make a request into Ocelot on /goods/delete Ocelot will match /goods/delete ReRoute. Previously it would have
matched /goods/{catchAll} (because this is the first ReRoute in the list!).

Dynamic Routing
^^^^^^^^^^^^^^^

This feature was requested in `issue 340 <https://github.com/TomPallister/Ocelot/issue/340>`_. The idea is to enable dynamic routing 
when using a service discovery provider so you don't have to provide the ReRoute config. See the docs :ref:`service-discovery` if 
this sounds interesting to you.
