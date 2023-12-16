Request ID
==========

     aka **Correlation ID** or `HttpContext.TraceIdentifier <https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.httpcontext.traceidentifier>`_

Ocelot supports a client sending a *request ID* in the form of a header.
If set, Ocelot will use the **RequestId** for logging as soon as it becomes available in the middleware pipeline. 
Ocelot will also forward the *RequestId* with the specified header to the downstream service.

You can still get the ASP.NET Core *Request ID* in the logs if you set **IncludeScopes** ``true`` in your logging config.

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

And more practical example from secret production environment in Switzerland:

.. code-block:: text

    warn: Ocelot.DownstreamRouteFinder.Middleware.DownstreamRouteFinderMiddleware[0]
          requestId: 0HMVD33IIJRFR:00000001, previousRequestId: no previous request id, message: DownstreamRouteFinderMiddleware setting pipeline errors. IDownstreamRouteFinder returned Error Code: UnableToFindDownstreamRouteError Message: Failed to match Route configuration for upstream path: /, verb: GET.
    warn: Ocelot.Responder.Middleware.ResponderMiddleware[0]
          requestId: 0HMVD33IIJRFR:00000001, previousRequestId: no previous request id, message: Error Code: UnableToFindDownstreamRouteError Message: Failed to match Route configuration for upstream path: /, verb: GET. errors found in ResponderMiddleware. Setting error response for request path:/, request method: GET

Curious?
--------

*Request ID* is a part of big :doc:`../features/logging` feature.

Every log record has these 2 properties:

* **RequestId** represents ID of the current request as plain string, for example ``0HMVD33IIJRFR:00000001``
* **PreviousRequestId** represents ID of the previous request

As an ``IOcelotLogger`` interface object being injected to constructors of service classes, current default Ocelot logger (the ``OcelotLogger`` class) reads these 2 properties from the ``IRequestScopedDataRepository`` interface object.
