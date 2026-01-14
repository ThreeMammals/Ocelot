Method Transformation [#f1]_
============================

Ocelot allows users to modify the HTTP request method used when making requests to a downstream service.
This is achieved by setting the following route configuration:

.. code-block:: json

  {
    "UpstreamPathTemplate": "/{everything}",
    "DownstreamPathTemplate": "/{everything}",
    // other props and opts...
    "UpstreamHttpMethod": [ "Get" ], // we transform HTTP verb...
    "DownstreamHttpMethod": "Post" // ...from GET to POST
  }

The key property here is ``DownstreamHttpMethod``, which is set to ``POST``, and the route will only match ``GET``, as specified by ``UpstreamHttpMethod``.

This feature is useful when interacting with downstream APIs that only support ``POST`` while presenting a RESTful interface.

""""

.. [#f1] The *"Method Transformation"* feature was released in version `14.0.8`_.
.. _14.0.8: https://github.com/ThreeMammals/Ocelot/releases/tag/14.0.8
