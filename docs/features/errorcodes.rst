HTTP Error Status Codes
=======================

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
