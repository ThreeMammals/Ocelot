Error Handling
==============
.. _Handle errors in ASP.NET Core: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling
.. _standard error handling: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling

  MS Learn: `Handle errors in ASP.NET Core`_

Ocelot has custom error handling for ``Exception`` objects.
Thus, we override the `standard error handling`_ provided by ASP.NET Core, which is based on manipulating ``Exception`` objects.

.. _eh-middleware:

Middleware
----------
.. _499 Client Closed Request: https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.statuscodes.status499clientclosedrequest
.. _500 Internal Server Error: https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Status/500

  Class: `ExceptionHandlerMiddleware <https://github.com/ThreeMammals/Ocelot/blob/main/src/Ocelot/Errors/Middleware/ExceptionHandlerMiddleware.cs>`_

The ``ExceptionHandlerMiddleware`` produces the following status codes, in fallback order, after setting the :ref:`lg-request-id`:

1. Native response status: Returned when no exception is present, or when a mapped error status is available (excluding ``499`` and ``500``).
2. `499 Client Closed Request`_: A custom Ocelot status returned when an ``OperationCanceledException`` occurs due to an aborted request.
   A warning is logged.
3. `500 Internal Server Error`_: The standard status returned when a generic ``Exception`` occurs and Ocelot does not process or map the error.
   An error record is logged.

Ocelot returns HTTP status codes based on internal logic in specific cases of :ref:`eh-client-error-responses` and :ref:`eh-server-error-responses`.

.. _eh-client-error-responses:

Client Error Responses
----------------------
.. _401 Unauthorized: https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Status/401
.. _403 Forbidden: https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Status/403
.. _404 Not Found: https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Status/404
.. _RequestCanceledError: https://github.com/search?q=repo%3AThreeMammals%2FOcelot+RequestCanceledError&type=code
.. _OcelotErrorCode.RequestCanceled: https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20OcelotErrorCode.RequestCanceled&type=code

- `401 Unauthorized`_: If the authentication middleware runs and the user is not authenticated.
- `403 Forbidden`_: If the authorization middleware runs and the user is unauthorized, if the claim value is not authorized, if the scope is not authorized, if the user does not have the required claim, or if the claim cannot be found.
- `404 Not Found`_: If a downstream route cannot be found, or if Ocelot is unable to map an internal error code to an HTTP status code.
- `499 Client Closed Request`_: If the request is canceled by the client.

    | Ocelot Error: `RequestCanceledError <https://github.com/search?q=repo%3AThreeMammals%2FOcelot+RequestCanceledError&type=code>`_
    | Ocelot Code: `OcelotErrorCode.RequestCanceled <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20OcelotErrorCode.RequestCanceled&type=code>`_

  According to Ocelot Core's design, HTTP status code ``499`` is returned in the following ``OperationCanceledException`` scenarios:

  1. By ``ExceptionHandlerMiddleware``, if an ``OperationCanceledException`` is thrown and the context's cancellation token is in the "cancellation requested" state.
     Ocelot logs a warning with the exception body. If the response has not started, the status code will be set to ``499``.
  2. By ``ResponderMiddleware``, if the default ``IErrorsToHttpStatusCodeMapper`` service maps the detected `OcelotErrorCode.RequestCanceled`_ to status ``499``.
     This error code is produced by the ``IExceptionToErrorMapper`` service when an ``OperationCanceledException`` is thrown by other middlewares.

.. _eh-server-error-responses:

Server Error Responses
----------------------
.. _502 Bad Gateway: https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Status/502
.. _503 Service Unavailable: https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Status/503

- `500 Internal Server Error`_: If unable to complete the HTTP request to the downstream service, and the exception is not ``OperationCanceledException`` or ``HttpRequestException``.
- `502 Bad Gateway`_: If unable to connect to the downstream service.
- `503 Service Unavailable`_: Returned when the downstream request times out.

    | Ocelot Error: `RequestTimedOutError <https://github.com/search?q=repo%3AThreeMammals%2FOcelot+RequestTimedOutError&type=code>`_
    | Ocelot Code: `OcelotErrorCode.RequestTimedOutError <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20OcelotErrorCode.RequestTimedOutError&type=code>`_

  According to Ocelot Core's design, status code ``503`` is produced in the following ``TimeoutException`` scenarios:

  1. By ``TimeoutDelegatingHandler`` from the ``IMessageInvokerPool`` service, when an ``OperationCanceledException`` is thrown and the context's cancellation token is not in the “cancellation requested” state.
     Ocelot does not log an error with the exception body, but the ``IExceptionToErrorMapper`` service generates the internal `OcelotErrorCode.RequestTimedOutError`_.
  2. By ``ResponderMiddleware``, if the default ``IErrorsToHttpStatusCodeMapper`` service maps the detected `OcelotErrorCode.RequestTimedOutError`_ to status ``503``.
     This error code is produced by the ``IExceptionToErrorMapper`` service when a ``TimeoutException`` is thrown by other middlewares—especially by ``TimeoutDelegatingHandler``.

.. _eh-error-mapper:

Error Mapper
------------

  Class: `HttpExceptionToErrorMapper <https://github.com/ThreeMammals/Ocelot/blob/main/src/Ocelot/Requester/HttpExceptionToErrorMapper.cs>`_

Historically, Ocelot errors are implemented by the `Exception-to-Error mapper <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20HttpExceptionToErrorMapper&type=code>`_.
The ``Map`` method converts an ``Exception`` object to a native ``Ocelot.Errors.Error`` object.

We override HTTP status codes because of ``Exception``-to-``Error`` mapping.
This can be confusing for the developer since the actual status code of the downstream service may be different and get lost.
Please research and review all response headers of the upstream service.
If you do not find status codes and/or required headers, then the :doc:`../features/headerstransformation` feature should help.

We expect you to share your use case with us in the `Discussions <https://github.com/ThreeMammals/Ocelot/discussions>`_ space of the repository. |octocat|

.. |octocat| image:: https://github.githubassets.com/images/icons/emoji/octocat.png
  :alt: octocat
  :height: 25
  :class: img-valign-middle
