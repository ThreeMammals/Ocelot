Error Status Codes
==================

Ocelot will return HTTP status error codes based on internal logic in certain situations:

Client Error Responses
----------------------

- **401**: If the authentication middleware runs and the user is not authenticated.
- **403**: If the authorization middleware runs and the user is unauthenticated, claim value not authorized, scope not authorized, user has not the required claim, or cannot find the claim.
- **404**: If unable to find a downstream route, or Ocelot is unable to map an internal error code to an HTTP status code.
- **499**: If the request is canceled by the client.

Server Error Responses
----------------------

- **500**: If unable to complete the HTTP request to the downstream service, and the exception is not ``OperationCanceledException`` or ``HttpRequestException``.
- **502**: If unable to connect to the downstream service.
- **503**: If the downstream request times out.

Design
------

Historically, Ocelot errors are implemented by the `HttpExceptionToErrorMapper <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20HttpExceptionToErrorMapper&type=code>`_ class.
The ``Map`` method converts a ``Exception`` object to a native ``Ocelot.Errors.Error`` object.

We override HTTP status codes because of exception-to-error mapping.
This can be confusing for the developer since the actual status code of the downstream service may be different and get lost.
Please research and review all response headers of the upstream service.
If you do not find statuses and/or required headers, then the :doc:`../features/headerstransformation` feature should help.

We expect you to share your use case with us in the `Discussions <https://github.com/ThreeMammals/Ocelot/discussions>`_ space of the repository. |octocat|

.. |octocat| image:: https://github.githubassets.com/images/icons/emoji/octocat.png
  :alt: octocat
  :width: 23
