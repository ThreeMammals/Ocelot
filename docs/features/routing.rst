Routing
=======

Ocelot's primary functionality is to take incomeing http requests and forward them on
to a downstream service. At the moment in the form of another http request (in the future
this could be any transport mechanism.). 

Ocelot always adds a trailing slash to an UpstreamPathTemplate.

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
        "DownstreamPort": 80,
        "DownstreamHost" "localhost",
        "UpstreamPathTemplate": "/posts/{postId}",
        "UpstreamHttpMethod": [ "Put", "Delete" ]
    }

The DownstreamPathTemplate, Scheme, Port and Host make the URL that this request will be forwarded to.
The UpstreamPathTemplate is the URL that Ocelot will use to identity which 
DownstreamPathTemplate to use for a given request. Finally the UpstreamHttpMethod is used so
Ocelot can distinguish between requests to the same URL and is obviously needed to work :)
You can set a specific list of HTTP Methods or set an empty list to allow any of them. In Ocelot you can add placeholders for variables to your Templates in the form of {something}.
The placeholder needs to be in both the DownstreamPathTemplate and UpstreamPathTemplate. If it is
Ocelot will attempt to replace the placeholder with the correct variable value from the 
Upstream URL when the request comes in.

You can also do a catch all type of ReRoute e.g. 

.. code-block:: json

    {
        "DownstreamPathTemplate": "/api/{everything}",
        "DownstreamScheme": "https",
        "DownstreamPort": 80,
        "DownstreamHost" "localhost",
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