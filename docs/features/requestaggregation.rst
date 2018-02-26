Request Aggregation
===================

Ocelot allow's you to specify Aggregate ReRoutes that compose multiple normal ReRoutes and map their responses into one object. This is usual where you have 
a client that is making multiple requests to a server where it could just be one. This feature allows you to start implementing back end for a front end type 
architecture with Ocelot.

This feature was requested as part of `Issue 79 <https://github.com/TomPallister/Ocelot/pull/79>`_ .

In order to set this up you must do something like the following in your configuration.json. Here we have specified two normal ReRoutes and each one has a Key property. 
We then specify an Aggregate that composes the two ReRoutes using their keys in the ReRouteKeys list and says then we have the UpstreamPathTemplate which works like a normal ReRoute.
Obviously you cannot have duplicate UpstreamPathTemplates between ReRoutes and Aggregates. You can use all of Ocelot's normal ReRoute options apart from RequestIdKey (explained in gotchas below).

.. code-block:: json

    {
        "ReRoutes": [
            {
                "DownstreamPathTemplate": "/",
                "UpstreamPathTemplate": "/laura",
                "UpstreamHttpMethod": [
                    "Get"
                ],
                "DownstreamScheme": "http",
                "DownstreamHostAndPorts": [
                    {
                        "Host": "localhost",
                        "Port": 51881
                    }
                ],
                "Key": "Laura"
            },
            {
                "DownstreamPathTemplate": "/",
                "UpstreamPathTemplate": "/tom",
                "UpstreamHttpMethod": [
                    "Get"
                ],
                "DownstreamScheme": "http",
                "DownstreamHostAndPorts": [
                    {
                        "Host": "localhost",
                        "Port": 51882
                    }
                ],
                "Key": "Tom"
            }
        ],
        "Aggregates": [
            {
                "ReRouteKeys": [
                    "Tom",
                    "Laura"
                ],
                "UpstreamPathTemplate": "/"
            }
        ]
    }

You can also set UpstreamHost and ReRouteIsCaseSensitive in the Aggregate configuration. These behave the same as any other ReRoutes.

If the ReRoute /tom returned a body of {"Age": 19} and /laura returned {"Age": 25} the the response after aggregation would be as follows.

.. code-block:: json

    {"Tom":{"Age": 19},"Laura":{"Age": 25}}

Gotcha's / Further info
^^^^^^^^^^^^^^^^^^^^^^^

At the moment the aggregation is very simple. Ocelot just gets the response from your downstream service and sticks it into a json dictionary 
as above. With the ReRoute key being the key of the dictionary and the value the response body from your downstream service. You can see that the object is just
JSON without any pretty spaces etc.

All headers will be lost from the downstream services response.

Ocelot will always return content type application/json with an aggregate request.

You cannot use ReRoutes with specific RequestIdKeys as this would be crazy complicated to track.

Aggregation only supports the GET HTTP Verb.

If you downstream services return a 404 the aggregate will just return nothing for that downstream service. 
It will not change the aggregate response into a 404 even if all the downstreams return a 404.

Future
^^^^^^

There are loads of cool ways to enchance this such as..

What happens when downstream goes slow..should we timeout?
Can we do something like GraphQL where the user chooses what fields are returned?
Can we handle 404 better etc?
Can we make this not just support a JSON dictionary response?

