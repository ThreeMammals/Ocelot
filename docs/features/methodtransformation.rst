Method Transformation
=====================

Ocelot allows the user to change the HTTP request method that will be used when making a request to a downstream service.

This achieved by setting the following Route configuration:

.. code-block:: json

  {
    "UpstreamPathTemplate": "/{url}",
    "DownstreamPathTemplate": "/{url}",
    "DownstreamScheme": "http",
    "DownstreamHostAndPorts": [
      { "Host": "localhost", "Port": 54321 }
    ],
    "UpstreamHttpMethod": [ "Get" ],
    "DownstreamHttpMethod": "POST" // !
  }

The key property here is **DownstreamHttpMethod** which is set as ``POST`` and the Route will only match on ``GET`` as set by **UpstreamHttpMethod**.

This feature can be useful when interacting with downstream APIs that only support POST and you want to present some kind of RESTful interface.
