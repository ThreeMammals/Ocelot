Request Aggregation
===================

Ocelot allows you to specify Aggregate Routes that compose multiple normal Routes and map their responses into one object.
This is usually where you have a client that is making multiple requests to a server where it could just be one.
This feature allows you to start implementing back-end for a front-end (BFF) type architecture with Ocelot.

This feature was requested as part of `issue 79 <https://github.com/ThreeMammals/Ocelot/issues/79>`_ and further improvements were made as part of `issue 298 <https://github.com/ThreeMammals/Ocelot/issues/298>`_.

In order to set this up you must do something like the following in your **ocelot.json**.
Here we have specified two normal Routes and each one has a **Key** property. 
We then specify an Aggregate that composes the two Routes using their keys in the **RouteKeys** list and says then we have the **UpstreamPathTemplate** which works like a normal Route.
Obviously you cannot have duplicate **UpstreamPathTemplates** between **Routes** and **Aggregates**.
You can use all of Ocelot's normal Route options apart from **RequestIdKey** (explained in `gotchas <#gotchas>`_ below).

Advanced Register Your Own Aggregators
--------------------------------------

Ocelot started with just the basic request aggregation and since then we have added a more advanced method that let's the user take in the responses from the 
downstream services and then aggregate them into a response object.

The **ocelot.json** setup is pretty much the same as the basic aggregation approach apart from you need to add an **Aggregator** property like below:

.. code-block:: json

  {
    "Routes": [
      {
        "UpstreamHttpMethod": [ "Get" ],
        "UpstreamPathTemplate": "/laura",
        "DownstreamPathTemplate": "/",
        "DownstreamScheme": "http",
        "DownstreamHostAndPorts": [
          { "Host": "localhost", "Port": 51881 }
        ],
        "Key": "Laura" // <--
      },
      {
        "UpstreamHttpMethod": [ "Get" ],
        "UpstreamPathTemplate": "/tom",
        "DownstreamPathTemplate": "/",
        "DownstreamScheme": "http",
        "DownstreamHostAndPorts": [
          { "Host": "localhost", "Port": 51882 }
        ],
        "Key": "Tom" // <--
      }
    ],
    "Aggregates": [ 
      {
        "UpstreamPathTemplate": "/",
        "RouteKeys": [
          "Tom",
          "Laura"
        ],
        "Aggregator": "FakeDefinedAggregator"
      }
    ]
  }

Here we have added an aggregator called ``FakeDefinedAggregator``. Ocelot is going to look for this aggregator when it tries to aggregate this Route.

In order to make the aggregator available we must add the ``FakeDefinedAggregator`` to the ``OcelotBuilder`` being returned by ``AddOcelot()`` [#f1]_ like below:

.. code-block:: csharp

    services
        .AddOcelot()
        .AddSingletonDefinedAggregator<FakeDefinedAggregator>();

Now when Ocelot tries to aggregate the Route above it will find the ``FakeDefinedAggregator`` in the container and use it to aggregate the Route. 
Because the ``FakeDefinedAggregator`` is registered in the container you can add any dependencies it needs into the container like below:
    
.. code-block:: csharp

    services.AddSingleton<FooDependency>();
    // ...
    services.AddOcelot()
        .AddSingletonDefinedAggregator<FooAggregator>();

In this example ``FooAggregator`` takes a dependency on ``FooDependency`` and it will be resolved by the container.

In addition to this Ocelot lets you add transient aggregators like below:

.. code-block:: csharp

    services
        .AddOcelot()
        .AddTransientDefinedAggregator<FakeDefinedAggregator>();

In order to make an Aggregator you must implement this interface:

.. code-block:: csharp

    public interface IDefinedAggregator
    {
        Task<DownstreamResponse> Aggregate(List<HttpContext> responses);
    }

With this feature you can pretty much do whatever you want because the ``HttpContext`` objects contain the results of all the aggregate requests.
Please note, if the ``HttpClient`` throws an exception when making a request to a Route in the aggregate then you will not get a ``HttpContext`` for it, but you would for any that succeed.
If it does throw an exception, this will be logged.

Basic Expecting JSON from Downstream Services
---------------------------------------------

.. code-block:: json

  {
    "Routes": [
      {
        "UpstreamHttpMethod": [ "Get" ],
        "UpstreamPathTemplate": "/laura",
        "DownstreamPathTemplate": "/",
        "DownstreamScheme": "http",
        "DownstreamHostAndPorts": [
          { "Host": "localhost", "Port": 51881 }
        ],
        "Key": "Laura"
      },
      {
        "UpstreamHttpMethod": [ "Get" ],
        "UpstreamPathTemplate": "/tom",
        "DownstreamPathTemplate": "/",
        "DownstreamScheme": "http",
        "DownstreamHostAndPorts": [
          { "Host": "localhost", "Port": 51882 }
        ],
        "Key": "Tom"
      }
    ],
    "Aggregates": [
      {
        "UpstreamPathTemplate": "/",
        "RouteKeys": [
          "Tom",
          "Laura"
        ]
      }
    ]
  }

You can also set **UpstreamHost** and **RouteIsCaseSensitive** in the Aggregate configuration. These behave the same as any other Routes.

If the Route ``/tom`` returned a body of ``{"Age": 19}`` and ``/laura`` returned ``{"Age": 25}``, the the response after aggregation would be as follows:

.. code-block:: json

    {"Tom":{"Age": 19},"Laura":{"Age": 25}}

At the moment the aggregation is very simple. Ocelot just gets the response from your downstream service and sticks it into a JSON dictionary as above.
With the Route key being the key of the dictionary and the value the response body from your downstream service.
You can see that the object is just JSON without any pretty spaces etc.

Note, all headers will be lost from the downstream services response.

Ocelot will always return content type ``application/json`` with an aggregate request.

If you downstream services return a `404 Not Found <https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/404>`_, the aggregate will just return nothing for that downstream service. 
It will not change the aggregate response into a ``404`` even if all the downstreams return a ``404``.

Gotchas
-------

You cannot use Routes with specific **RequestIdKeys** as this would be crazy complicated to track.

Aggregation only supports the ``GET`` HTTP verb.

""""

.. [#f1] The ``AddOcelot`` method adds default ASP.NET services to DI-container. You could call another more extended ``AddOcelotUsingBuilder`` method while configuring services to build and use custom builder via an ``IMvcCoreBuilder`` interface object. See more instructions in :doc:`../features/dependencyinjection`, "**The AddOcelotUsingBuilder method**" section.
