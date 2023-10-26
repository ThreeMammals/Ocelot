Request ID
==========

     aka **Correlation ID**

Ocelot supports a client sending a *request ID* in the form of a header.
If set, Ocelot will use the **requestId** for logging as soon as it becomes available in the middleware pipeline. 
Ocelot will also forward the *request ID* with the specified header to the downstream service.

You can still get the ASP.NET Core *request ID* in the logs if you set **IncludeScopes** ``true`` in your logging config.

In order to use the *Request ID* feature you have two options.

Global
------

In your **ocelot.json** set the following in the **GlobalConfiguration** section. This will be used for all requests into Ocelot.

.. code-block:: json

  "GlobalConfiguration": {
    "RequestIdKey": "OcRequestId"
  }

We recommend using the **GlobalConfiguration** unless you really need it to be Route specific.

Route
-----

If you want to override this for a specific Route, add the following to **ocelot.json** for the specific Route:

.. code-block:: json

  "RequestIdKey": "OcRequestId"

Once Ocelot has identified the incoming requests matching Route object it will set the *request ID* based on the Route configuration.

Gotcha
------

This can lead to a small gotcha.
If you set a **GlobalConfiguration**, it is possible to get one *request ID* until the Route is identified and then another after that because the *request ID* key can change.
This is by design and is the best solution we can think of at the moment.
In this case the ``OcelotLogger`` will show the *request ID* and previous *request ID* in the logs.

Below is an example of the logging when set at ``Debug`` level for a normal request:

.. code-block:: text

    dbug: Ocelot.Errors.Middleware.ExceptionHandlerMiddleware[0]
          requestId: asdf, previousRequestId: no previous request id, message: ocelot pipeline started,
    dbug: Ocelot.DownstreamRouteFinder.Middleware.DownstreamRouteFinderMiddleware[0]
          requestId: asdf, previousRequestId: no previous request id, message: upstream url path is {upstreamUrlPath},
    dbug: Ocelot.DownstreamRouteFinder.Middleware.DownstreamRouteFinderMiddleware[0]
          requestId: asdf, previousRequestId: no previous request id, message: downstream template is {downstreamRoute.Data.Route.DownstreamPath},
    dbug: Ocelot.RateLimit.Middleware.ClientRateLimitMiddleware[0]
          requestId: asdf, previousRequestId: no previous request id, message: EndpointRateLimiting is not enabled for Ocelot.Values.PathTemplate,
    dbug: Ocelot.Authorization.Middleware.AuthorizationMiddleware[0]
          requestId: 1234, previousRequestId: asdf, message: /posts/{postId} route does not require user to be authorized,
    dbug: Ocelot.DownstreamUrlCreator.Middleware.DownstreamUrlCreatorMiddleware[0]
          requestId: 1234, previousRequestId: asdf, message: downstream url is {downstreamUrl.Data.Value},
    dbug: Ocelot.Request.Middleware.HttpRequestBuilderMiddleware[0]
          requestId: 1234, previousRequestId: asdf, message: setting upstream request,
    dbug: Ocelot.Requester.Middleware.HttpRequesterMiddleware[0]
          requestId: 1234, previousRequestId: asdf, message: setting http response message,
    dbug: Ocelot.Responder.Middleware.ResponderMiddleware[0]
          requestId: 1234, previousRequestId: asdf, message: no pipeline errors, setting and returning completed response,
    dbug: Ocelot.Errors.Middleware.ExceptionHandlerMiddleware[0]
          requestId: 1234, previousRequestId: asdf, message: ocelot pipeline finished,
