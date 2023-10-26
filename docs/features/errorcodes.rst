Error Status Codes
==================

Ocelot will return HTTP status error codes based on internal logic in certain situations:

Client error responses
----------------------

- **401** - if the authentication middleware runs and the user is not authenticated.
- **403** - if the authorization middleware runs and the user is unauthenticated, claim value not authorized, scope not authorized, user doesn't have required claim, or cannot find claim.
- **404** - if unable to find a downstream route, or Ocelot is unable to map an internal error code to a HTTP status code.
- **499** - if the request is cancelled by the client.

Server error responses
----------------------

- **500** - if unable to complete the HTTP request to downstream service, and the exception is not ``OperationCanceledException`` or ``HttpRequestException``.
- **502** - if unable to connect to downstream service.
- **503** - if the downstream request times out.

Design
------

Historically Ocelot errors are implemented by the `HttpExceptionToErrorMapper <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20HttpExceptionToErrorMapper&type=code>`_ class.
The ``Map`` method converts a ``System.Exception`` object to native ``Ocelot.Errors.Error`` object.

We do HTTP status code overriding because of Exception-to-Error mapping.
This can be confusing for the developer since the actual status code of the downstream service may be different and get lost.
Please, research and review all response headers of upstream service.
If you did not find statuses and (or) required headers then :doc:`../features/headerstransformation` feature should help.

.. |octocat| image:: https://github.githubassets.com/images/icons/emoji/octocat.png
  :alt: octocat
  :width: 23

We expect you to share your user case with us in the `Discussions <https://github.com/ThreeMammals/Ocelot/discussions>`_ space of the repository. |octocat|
