.. _ocelot.json: https://github.com/ThreeMammals/Ocelot/blob/main/samples/Basic/ocelot.json

Request ID
==========

  | Feature of: :doc:`../features/logging`
  | Also known as "Correlation ID" or `HttpContext.TraceIdentifier <https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.httpcontext.traceidentifier>`_

Ocelot allows a client to send a *Request ID* through an HTTP header.
If provided, Ocelot uses the *Request ID* for logging as soon as it becomes available in the middleware pipeline.
Additionally, Ocelot forwards the *Request ID* via the specified header to the downstream service.

  You can still obtain the ASP.NET Core *Request ID* in the logs if you set ``IncludeScopes`` to ``true`` in your logging configuration.

Configuration
-------------

In order to use the *Request ID* feature, you have two options: specifying it globally or for the route.

In your `ocelot.json`_, set the following configuration in the ``GlobalConfiguration`` section.
This setting will apply to all requests processed by Ocelot.

.. code-block:: json

  "GlobalConfiguration": {
    "RequestIdKey": "Oc-RequestId"
  }

.. _break: http://break.do

  We recommend using the ``GlobalConfiguration`` unless it is absolutely necessary to make it route-specific.

If you want to override this for a specific route, add the following to `ocelot.json`_:

.. code-block:: json

  "RequestIdKey": "Oc-RequestId"

Once Ocelot identifies incoming requests that match a route, it will set the *Request ID* based on the route configuration.

Problem
-------

This can lead to a small issue.
If you set a ``GlobalConfiguration``, it is possible to use one *Request ID* until the route is identified and then another afterward, as the *Request ID* key can change.
This behavior is intentional and represents the best solution we have devised for now.
In this case, the ``OcelotLogger`` will display both the current *Request ID* and the previous *Request ID* in the logs.

Below is an example of the logging when the ``Debug`` level is set for a normal request:

  .. code-block:: text

      info: Microsoft.AspNetCore.Hosting.Diagnostics[1]
            Request starting HTTP/1.1 GET https://localhost:7778/ocelot2/posts/3 - - -
      dbug: Ocelot.Errors.Middleware.ExceptionHandlerMiddleware[0]
            RequestId: 0HNBA3NEIQUNJ:11111111, PreviousRequestId: -
            Ocelot pipeline started
      dbug: Ocelot.DownstreamRouteFinder.Middleware.DownstreamRouteFinderMiddleware[0]
            RequestId: 0HNBA3NEIQUNJ:11111111, PreviousRequestId: -
            Upstream URL path: /ocelot2/posts/3
      dbug: Ocelot.DownstreamRouteFinder.Middleware.DownstreamRouteFinderMiddleware[0]
            RequestId: 0HNBA3NEIQUNJ:11111111, PreviousRequestId: -
            Downstream templates: /ocelot/posts/{id}
      info: Ocelot.RateLimiting.Middleware.RateLimitingMiddleware[0]
            RequestId: 0HNBA3NEIQUNJ:11111111, PreviousRequestId: -
            EnableEndpointEndpointRateLimiting is not enabled for downstream path: /ocelot/posts/{id}
      info: Ocelot.Authentication.Middleware.AuthenticationMiddleware[0]
            RequestId: 0HNBA3NEIQUNJ:11111111, PreviousRequestId: -
            No authentication needed for path: /ocelot2/posts/3
      info: Ocelot.Authorization.Middleware.AuthorizationMiddleware[0]
            RequestId: 0HNBA3NEIQUNJ:11111111, PreviousRequestId: -
            No authorization needed for upstream path: /ocelot2/posts/{id}
      dbug: Ocelot.DownstreamUrlCreator.Middleware.DownstreamUrlCreatorMiddleware[0]
            RequestId: 0HNBA3NEIQUNJ:11111111, PreviousRequestId: -
            Downstream URL: http://localhost:5555/ocelot/posts/3
      info: Microsoft.AspNetCore.Hosting.Diagnostics[1]
            Request starting HTTP/1.1 GET https://localhost:7778/ocelot2/posts/5 - - -
      dbug: Ocelot.Errors.Middleware.ExceptionHandlerMiddleware[0]
            RequestId: 0HNBA3NEIQUNK:AAAAAAAA, PreviousRequestId: 0HNBA3NEIQUNJ:11111111
            Ocelot pipeline started
      dbug: Ocelot.DownstreamRouteFinder.Middleware.DownstreamRouteFinderMiddleware[0]
            RequestId: 0HNBA3NEIQUNK:AAAAAAAA, PreviousRequestId: 0HNBA3NEIQUNJ:11111111
            Upstream URL path: /ocelot2/posts/5
      dbug: Ocelot.DownstreamRouteFinder.Middleware.DownstreamRouteFinderMiddleware[0]
            RequestId: 0HNBA3NEIQUNK:AAAAAAAA, PreviousRequestId: 0HNBA3NEIQUNJ:11111111
            Downstream templates: /ocelot/posts/{id}
      info: Ocelot.RateLimiting.Middleware.RateLimitingMiddleware[0]
            RequestId: 0HNBA3NEIQUNK:AAAAAAAA, PreviousRequestId: 0HNBA3NEIQUNJ:11111111
            EnableEndpointEndpointRateLimiting is not enabled for downstream path: /ocelot/posts/{id}
      info: Ocelot.Authentication.Middleware.AuthenticationMiddleware[0]
            RequestId: 0HNBA3NEIQUNK:AAAAAAAA, PreviousRequestId: 0HNBA3NEIQUNJ:11111111
            No authentication needed for path: /ocelot2/posts/5
      info: Ocelot.Authorization.Middleware.AuthorizationMiddleware[0]
            RequestId: 0HNBA3NEIQUNK:AAAAAAAA, PreviousRequestId: 0HNBA3NEIQUNJ:11111111
            No authorization needed for upstream path: /ocelot2/posts/{id}
      dbug: Ocelot.DownstreamUrlCreator.Middleware.DownstreamUrlCreatorMiddleware[0]
            RequestId: 0HNBA3NEIQUNK:AAAAAAAA, PreviousRequestId: 0HNBA3NEIQUNJ:11111111
            Downstream URL: http://localhost:5555/ocelot/posts/5
      info: Ocelot.Requester.Middleware.HttpRequesterMiddleware[0]
            RequestId: 0HNBA3NEIQUNJ:11111111, PreviousRequestId: -
            200 OK status code of request URI: http://localhost:5555/ocelot/posts/3
      dbug: Ocelot.Requester.Middleware.HttpRequesterMiddleware[0]
            RequestId: 0HNBA3NEIQUNJ:11111111, PreviousRequestId: -
            Setting HTTP response message...
      dbug: Ocelot.Responder.Middleware.ResponderMiddleware[0]
            RequestId: 0HNBA3NEIQUNJ:11111111, PreviousRequestId: -
            No pipeline errors: setting and returning completed response...
      dbug: Ocelot.Errors.Middleware.ExceptionHandlerMiddleware[0]
            RequestId: 0HNBA3NEIQUNJ:11111111, PreviousRequestId: -
            Ocelot pipeline finished
      info: Microsoft.AspNetCore.Hosting.Diagnostics[2]
            Request finished HTTP/1.1 GET https://localhost:7778/ocelot2/posts/3 - 200 84 application/json;+charset=utf-8 404.7256ms
      info: Microsoft.AspNetCore.Hosting.Diagnostics[16]
            Request reached the end of the middleware pipeline without being handled by application code. Request path: GET https://localhost:7778/ocelot2/posts/3, Response status code: 200
      info: Ocelot.Requester.Middleware.HttpRequesterMiddleware[0]
            RequestId: 0HNBA3NEIQUNK:AAAAAAAA, PreviousRequestId: 0HNBA3NEIQUNJ:11111111
            200 OK status code of request URI: http://localhost:5555/ocelot/posts/5
      dbug: Ocelot.Requester.Middleware.HttpRequesterMiddleware[0]
            RequestId: 0HNBA3NEIQUNK:AAAAAAAA, PreviousRequestId: 0HNBA3NEIQUNJ:11111111
            Setting HTTP response message...
      dbug: Ocelot.Responder.Middleware.ResponderMiddleware[0]
            RequestId: 0HNBA3NEIQUNK:AAAAAAAA, PreviousRequestId: 0HNBA3NEIQUNJ:11111111
            No pipeline errors: setting and returning completed response...
      dbug: Ocelot.Errors.Middleware.ExceptionHandlerMiddleware[0]
            RequestId: 0HNBA3NEIQUNK:AAAAAAAA, PreviousRequestId: 0HNBA3NEIQUNJ:11111111
            Ocelot pipeline finished
      info: Microsoft.AspNetCore.Hosting.Diagnostics[2]
            Request finished HTTP/1.1 GET https://localhost:7778/ocelot2/posts/5 - 200 128 application/json;+charset=utf-8 347.2607ms
      info: Microsoft.AspNetCore.Hosting.Diagnostics[16]
            Request reached the end of the middleware pipeline without being handled by application code. Request path: GET https://localhost:7778/ocelot2/posts/5, Response status code: 200

.. Note by Maintainer:
..   The PreviousRequestId feature requires review and possible redesign, as it may not be implemented or could be broken.
..   Typically, PreviousRequestId is '-' for all requests.

Technical Facts
---------------

* *Request ID* is a part of big :doc:`../features/logging` feature.
* Every log record has these 2 properties:

  * ``RequestId`` represents ID of the current request as plain string, for example ``0HNBA3NEIQUNJ:00000001``.
  * ``PreviousRequestId`` represents ID of the previous request.
* As an ``IOcelotLogger`` interface object is injected into the constructors of service classes, the current default Ocelot logger (the ``OcelotLogger`` class) retrieves these two properties from the ``IRequestScopedDataRepository`` service.
