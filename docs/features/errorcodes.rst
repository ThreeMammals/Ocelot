.. _Handle errors in ASP.NET Core: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling

Error Handling
==============

  MS Learn: `Handle errors in ASP.NET Core`_

Ocelot has custom error handling for ``Exception`` objects.
Thus, we override the `standard error handling <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling>`_ provided by ASP.NET Core, which is based on manipulating ``Exception`` objects.

.. _eh-middleware:

Middleware
----------

  Class: `ExceptionHandlerMiddleware <https://github.com/ThreeMammals/Ocelot/blob/main/src/Ocelot/Errors/Middleware/ExceptionHandlerMiddleware.cs>`_

The ``ExceptionHandlerMiddleware`` produces the following statuses in this fallback order after setting the :ref:`lg-request-id`:

1. Native response status in case of absent exceptions and/or mapped error status (not **499**, not **500**).
2. **499**: A custom Ocelot status in case of an ``OperationCanceledException`` when the request has been aborted.
   It logs a ``LogLevel.Warning`` record.
3. **500**: The standard `Internal Server Error <https://developer.mozilla.org/ru/docs/Web/HTTP/Status/500>`_ status in case of a generic ``Exception`` when it seems Ocelot has not processed or mapped the error.
   It logs a ``LogLevel.Error`` record.

Ocelot will return HTTP status error codes based on internal logic in certain situations:

Client Error Responses
----------------------

- **401**: If the authentication middleware runs and the user is not authenticated.
- **403**: If the authorization middleware runs and the user is unauthorized, if the claim value is not authorized, if the scope is not authorized, if the user does not have the required claim, or if the claim cannot be found.
- **404**: If a downstream route cannot be found, or if Ocelot is unable to map an internal error code to an HTTP status code.
- **499**: If the request is canceled by the client.

Server Error Responses
----------------------

- **500**: If unable to complete the HTTP request to the downstream service, and the exception is not ``OperationCanceledException`` or ``HttpRequestException``.
- **502**: If unable to connect to the downstream service.
- **503**: If the downstream request times out.


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
