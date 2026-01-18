Aggregation
===========

*Aggregation*, also known as HTTP response data aggregation, is a well-known Backend for Frontend pattern of Microservices architecture.

  * `Backend for Frontend (BFF) Pattern: Microservices for UX | Teleport Academy <https://goteleport.com/learn/backend-for-frontend-bff-pattern/>`_
  * `Gateway Aggregation pattern | Azure Architecture Center | Microsoft Learn <https://learn.microsoft.com/en-us/azure/architecture/patterns/gateway-aggregation>`_
  * `Backends for Frontends pattern | Azure Architecture Center | Microsoft Learn <https://learn.microsoft.com/en-us/azure/architecture/patterns/backends-for-frontends>`_
  * `Implement API Gateways with Ocelot | .NET microservices - Architecture e-book | Microsoft Learn <https://learn.microsoft.com/en-us/dotnet/architecture/microservices/multi-container-microservice-net-applications/implement-api-gateways-with-ocelot>`_

Ocelot allows you to specify *Aggregate Routes* [#f1]_ that combine multiple normal routes and map their responses into a single object.
This is particularly useful when a client is making multiple requests to a server that could be consolidated into one.
This feature supports the implementation of a Backend for Frontend (BFF) architecture using Ocelot.

Configuration
-------------

.. _ocelot.json: https://github.com/ThreeMammals/Ocelot/blob/main/samples/Basic/ocelot.json

In order to set this up, you need to configure the `ocelot.json`_ file as follows.
In this example, two normal routes are specified, each having a ``Key`` property.
An *aggregation* is then defined, which combines the two routes using their keys listed in ``RouteKeys``, and the ``UpstreamPathTemplate`` is set up to function like a normal route.

  Note that duplicate ``UpstreamPathTemplates`` are not allowed between ``Routes`` and ``Aggregates``.
  You can use all of Ocelot's normal route options, except for ``RequestIdKey``, as explained in the :ref:`agg-gotchas` section.

.. code-block:: json
  :emphasize-lines: 11, 21, 24

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
        "RouteKeys": [ "Tom", "Laura" ]
      }
    ]
  }

You can also set ``UpstreamHost`` and ``RouteIsCaseSensitive`` in the *aggregation* configuration. These settings behave the same as in other routes.

If the route ``/tom`` returned a body of ``{"Age": 19}`` and ``/laura`` returned ``{"Age": 25}``, the response after *aggregation* would be as follows:

.. code-block:: json

    {"Tom":{"Age": 19},"Laura":{"Age": 25}}

At the moment, the *aggregation* is quite simple.
Ocelot retrieves the response from your downstream service and inserts it into a JSON dictionary, as shown above.
The route ``Key`` becomes the key of the dictionary, and the response body from your downstream service serves as the value.
The resulting object is plain JSON without any formatting or additional spaces.

  **Note 1**: All headers will be lost from the downstream service's response.

  **Note 2**: Ocelot will always return the content type ``application/json`` for an aggregate request.

  **Note 3**: If your downstream services return a ``404`` `Not Found <https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/404>`_, the aggregate will simply return nothing for that downstream service.
  It will not change the aggregate response to a ``404``, even if all the downstream services return a ``404``.

.. _agg-complex-aggregation:

Complex Aggregation [#f2]_
--------------------------

Imagine you would like to use aggregated queries but don't have all the parameters for your queries.
First, you need to call an endpoint to obtain the necessary data, such as a user's ID, and then return the user's details.

Let's say we have an endpoint that returns a series of comments referencing various users or threads.
The author of the comments is identified by their ID, but you want to return all the details about the author.

Here, you could use aggregation to: 1) retrieve all the comments, and 2) attach the author details.
In fact, two endpoints are called, but for the second, you dynamically replace the user's ID in the route to obtain the details.

In concrete terms:

1) ``/Comments`` contains the ``authorId`` property.
2) ``/users/{userId}``, with ``{userId}`` replaced by ``authorId``, is used to obtain the user's details.

To perform the mapping, you need to use the ``RouteKeysConfig`` list of configuration options for aggreagte route, typed as ``AggregateRouteConfig`` class:

.. code-block:: json

  "RouteKeysConfig": [
    {
      "RouteKey": "UserDetails",
      "JsonPath": "$[*].authorId",
      "Parameter": "userId"
    }
  ]

``RouteKey`` is used as a reference for the route, ``JsonPath`` indicates where the parameter of interest is located in the first request's response body, and ``Parameter`` specifies that the value for ``authorId`` should be used as the request parameter ``userId``.

The final configuration is as follows:

.. code-block:: json
  :emphasize-lines: 27-30

  {
    "Routes": [
      {
        "UpstreamPathTemplate": "/Comments",
        "DownstreamPathTemplate": "/",
        // ...
        "Key": "Comments"
      },
      {
        "UpstreamPathTemplate": "/UserDetails/{userId}",
        "DownstreamPathTemplate": "/users/{userId}",
        // ...
        "Key": "UserDetails"
      },
      {
        "UpstreamPathTemplate": "/PostDetails/{postId}",
        "DownstreamPathTemplate": "/posts/{postId}",
        // ...
        "Key": "PostDetails"
      }
    ],
    "Aggregates": [
      {
        "UpstreamPathTemplate": "/",
        "UpstreamHost": "localhost",
        "RouteKeys": [ "Comments", "UserDetails", "PostDetails" ],
        "RouteKeysConfig": [
          { "RouteKey": "UserDetails", "JsonPath": "$[*].writerId", "Parameter": "userId" },
          { "RouteKey": "PostDetails", "JsonPath": "$[*].postId", "Parameter": "postId" }
        ]
      }
    ]
  }

Custom Aggregators
------------------

Ocelot started with basic request *aggregation*, and since then, a more advanced method has been added.
This method allows the user to take the responses from downstream services and aggregate them into a response object.
The `ocelot.json`_ setup is almost identical to the basic *aggregation* approach, except that you need to add an ``Aggregator`` property, as shown below:

.. code-block:: json
  :emphasize-lines: 20

  {
    "Routes": [
      {
        "UpstreamPathTemplate": "/laura",
        "DownstreamPathTemplate": "/",
        // ...
        "Key": "Laura"
      },
      {
        "UpstreamPathTemplate": "/tom",
        "DownstreamPathTemplate": "/",
        // ...
        "Key": "Tom"
      }
    ],
    "Aggregates": [ 
      {
        "UpstreamPathTemplate": "/",
        "RouteKeys": [ "Tom", "Laura" ],
        "Aggregator": "MyAggregator"
      }
    ]
  }

Here, we have added an aggregator called ``MyAggregator``. Ocelot will look for this aggregator when it tries to aggregate this route.

In order to make the aggregator available in Ocelot Core, we must add the ``MyAggregator`` to the ``OcelotBuilder`` returned by ``AddOcelot()`` [#f3]_, as shown below:

.. code-block:: csharp
  :emphasize-lines: 5

  using Ocelot.Multiplexer;

  builder.Services
      .AddOcelot(builder.Configuration)
      .AddSingletonDefinedAggregator<MyAggregator>();

Now, when Ocelot tries to aggregate the route above, it will find the ``MyAggregator`` in the DI-container and use it to aggregate the route.
Since the ``MyAggregator`` is registered in the DI-container, you can add any dependencies it needs to the container, as shown below:
    
.. code-block:: csharp
  :emphasize-lines: 2, 6

  builder.Services
      .AddSingleton<MyDependency>();
  // ...
  builder.Services
      .AddOcelot(builder.Configuration)
      .AddSingletonDefinedAggregator<MyAggregator>();

In this example, ``MyAggregator`` depends on ``MyDependency``, and it will be resolved by the DI container.
In addition to this, Ocelot lets you add transient aggregators, as shown below:

.. code-block:: csharp
  :emphasize-lines: 3

  builder.Services
      .AddOcelot(builder.Configuration)
      .AddTransientDefinedAggregator<MyAggregator>();

In order to create an *aggregator*, you must implement the following interface:

.. code-block:: csharp

    public interface IDefinedAggregator
    {
        Task<DownstreamResponse> Aggregate(List<HttpContext> responses);
    }

With this feature, you can essentially do whatever you want, as the ``HttpContext`` objects contain the results of all the aggregate requests.

  Please note that if the ``HttpClient`` throws an exception when making a request to a route in the aggregate, you will not receive a ``HttpContext`` for it.
  However, you will receive one for any that succeed. If an exception is thrown, it will be logged.

Below is an example of an *aggregator* that can be implemented for your solution:

.. code-block:: csharp

  public class MyAggregator : IDefinedAggregator
  {
      public async Task<DownstreamResponse> Aggregate(List<HttpContext> responseHttpContexts)
      {
          // The aggregator gets a list of downstream responses as parameter.
          // You can now implement your own logic to aggregate the responses (including bodies and headers) from the downstream services
          var responses = responseHttpContexts.Select(x => x.Items.DownstreamResponse()).ToArray();

          // In this example we are concatenating the results,
          // but you could create a more complex construct, up to you.
          var contentList = new List<string>();
          foreach (var response in responses)
          {
              var content = await response.Content.ReadAsStringAsync();
              contentList.Add(content);
          }

          // The only constraint here: You must return a DownstreamResponse object.
          return new DownstreamResponse(
              new StringContent(JsonConvert.SerializeObject(contentList)),
              HttpStatusCode.OK,
              responses.SelectMany(x => x.Headers).ToList(),
              "reason");
      }
  }

.. _agg-gotchas:

Gotchas
-------

* You cannot use routes with specific ``RequestIdKeys``, as this would be overly complicated to track.
* *Aggregation* supports only the ``GET`` HTTP verb.
* *Aggregation* allows the forwarding of ``HttpRequest.Body`` to downstream services by duplicating the body data.
  Form data and attached files should also be forwarded.
  It is essential to specify the ``Content-Length`` header in requests to the upstream; otherwise, Ocelot will log warnings such as: *"Aggregation does not support body copy without a Content-Length header!"*

""""

.. [#f1] This feature was requested as part of issue `79`_, and further improvements were made as part of issue `298`_. A significant refactoring and revision of the `Multiplexer <https://github.com/ThreeMammals/Ocelot/tree/main/src/Ocelot/Multiplexer>`_ design was carried out on March 4, 2024, in version `23.1`_. See pull requests `1462`_ and `1826`_ for more details.
.. [#f2] The ":ref:`Complex Aggregation <agg-complex-aggregation>`" feature is still in its early stages, but it enables searching for data based on an initial request. This feature was requested as part of issue `661`_, introduced in pull request `704`_, and released in version `13.4`_.
.. [#f3] The :ref:`di-services-addocelot-method` adds default ASP.NET services to the DI container. You can call another extended :ref:`di-addocelotusingbuilder-method` while configuring services to develop your own :ref:`di-custom-builder`. See more instructions in the ":ref:`di-addocelotusingbuilder-method`" section of the :doc:`../features/dependencyinjection` feature.

.. _79: https://github.com/ThreeMammals/Ocelot/issues/79
.. _298: https://github.com/ThreeMammals/Ocelot/issues/298
.. _661: https://github.com/ThreeMammals/Ocelot/issues/661

.. _704: https://github.com/ThreeMammals/Ocelot/pull/704
.. _1462: https://github.com/ThreeMammals/Ocelot/pull/1462
.. _1826: https://github.com/ThreeMammals/Ocelot/pull/1826

.. _13.4: https://github.com/ThreeMammals/Ocelot/releases/tag/13.4.1
.. _23.1: https://github.com/ThreeMammals/Ocelot/releases/tag/23.1.0
