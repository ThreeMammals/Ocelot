Request Proxying
================
.. contents:: Table of Contents
   :depth: 2
   :local:

Ocelot's core responsibility is proxying HTTP requests from upstream clients to downstream services.
This section covers key request proxying capabilities and special handling for different request types.

Basic Routing
-------------

  Feature: :doc:`../features/routing`

Ocelot routes HTTP requests from upstream paths to downstream services based on route configuration:

.. code-block:: json

  {
    "Routes": [
      {
        "UpstreamHttpMethod": [ "Get" ],
        "UpstreamPathTemplate": "/api/users/{id}",
        "DownstreamPathTemplate": "/users/{id}",
        "DownstreamScheme": "http",
        "DownstreamHostAndPorts": [
          { "Host": "localhost", "Port": 5000 }
        ]
      }
    ]
  }

When a client requests ``GET /api/users/123``, Ocelot forwards the request to the downstream service at ``http://localhost:5000/users/123``.

Headers and Metadata
--------------------

  Features: :doc:`../features/metadata`, :doc:`../features/claimstransformation`, :doc:`../features/headerstransformation`, :doc:`../features/methodtransformation`

Ocelot preserves HTTP headers during request forwarding, ensuring that important metadata (authentication, content type, custom headers) is maintained across the gateway.
You can configure :doc:`../features/headerstransformation` rules to add, remove, or modify headers as needed.

.. _rproxy-multipart-form-data:

Multipart Form Data
-------------------
.. _Content-Type: https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Content-Type
.. _RFC 7578: https://www.rfc-editor.org/info/rfc7578/

  Features: :doc:`../features/routing`, :doc:`../features/aggregation`

Ocelot supports routing and multiplexing of HTTP requests with ``multipart/form-data`` `Content-Type`_ header,
which is commonly used for BFF file uploads and form submissions with binary data.

When clients upload files or submit forms with mixed text and binary data, they use the ``multipart/form-data`` `Content-Type`_ (as defined in `RFC 7578`_).
Ocelot routes these requests to downstream services while preserving the multipart structure and file boundaries.

.. admonition:: Key Considerations

  **Request Body Buffering**.
  When routing multipart requests, especially in scenarios involving :doc:`../features/aggregation`, the request body must be buffered in memory.
  This allows Ocelot to read the request body once and forward it to multiple destinations without losing data.

  **Preservation of Boundaries**.
  Multipart requests contain boundary markers that separate form fields and file data.
  Ocelot preserves these boundaries and the original `Content-Type`_ header (including the boundary parameter) to ensure downstream services can correctly parse the request.

  **Content-Type Headers**.
  The `Content-Type`_ header is automatically set by HTTP clients and includes a boundary identifier.
  Ocelot does not modify this header during routing, ensuring compatibility with downstream services.

Basic Multipart Routing
^^^^^^^^^^^^^^^^^^^^^^^

  Feature: :doc:`../features/routing`

Multipart requests can be routed like any other HTTP POST request:

.. code-block:: json

  {
    "Routes": [
      {
        "UpstreamHttpMethod": [ "Post" ],
        "UpstreamPathTemplate": "/upload",
        "DownstreamPathTemplate": "/api/files/upload",
        "DownstreamScheme": "http",
        "DownstreamHostAndPorts": [
          { "Host": "localhost", "Port": 5000 }
        ]
      }
    ]
  }

When a client uploads a file to ``/upload``, Ocelot forwards the request with its multipart structure intact to ``/api/files/upload`` on the downstream service.

Aggregation with Multipart Data
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

  Feature: :doc:`../features/aggregation`

Ocelot also supports aggregating responses from multipart requests.
This is useful when you need to combine responses from multiple services that each process the same file upload.

.. code-block:: json
  :emphasize-lines: 11, 21, 28

  {
    "Routes": [
      {
        "UpstreamHttpMethod": [ "Post" ],
        "UpstreamPathTemplate": "/process/service1",
        "DownstreamPathTemplate": "/api/process",
        "DownstreamScheme": "http",
        "DownstreamHostAndPorts": [
          { "Host": "service1.example.com", "Port": 5000 }
        ],
        "Key": "Service1"
      },
      {
        "UpstreamHttpMethod": [ "Post" ],
        "UpstreamPathTemplate": "/process/service2",
        "DownstreamPathTemplate": "/api/process",
        "DownstreamScheme": "http",
        "DownstreamHostAndPorts": [
          { "Host": "service2.example.com", "Port": 5000 }
        ],
        "Key": "Service2"
      }
    ],
    "Aggregates": [
      {
        "UpstreamPathTemplate": "/process/aggregated",
        "UpstreamHttpMethod": [ "Post" ],
        "RouteKeys": [ "Service1", "Service2" ]
      }
    ]
  }

When a client sends a multipart request to ``/process/aggregated``, Ocelot:

1. Buffers the request body
2. Forwards the multipart request to both services: forms, files as part of the body
3. Collects the responses from both services
4. Aggregates them into a single JSON response

.. note::
  Non-GET HTTP methods for :doc:`../features/aggregation`, such as POST and PUT, can be specified starting with version `25.1`_.
  Refer to the ":ref:`Upstream HTTP Methods <agg-upstream-http-methods>`" feature documentation.

.. _25.1: https://github.com/ThreeMammals/Ocelot/releases/tag/25.1.0

Sample Application
^^^^^^^^^^^^^^^^^^
.. _MultipartFormData: https://github.com/ThreeMammals/Ocelot/tree/develop/samples/MultipartFormData

Ocelot includes a `MultipartFormData`_ sample application in the ``samples/`` directory that demonstrates file upload handling with a polished user interface.
The sample showcases:

- A file upload form with progress indicators
- Routing of multipart requests to a downstream service
- Real-time logging of request and response data
- File content inspection interface for large uploads

To run the sample:

.. code-block:: bash

  cd samples/MultipartFormData
  dotnet run

Then navigate to ``http://localhost:5555`` and use the upload interface to test multipart routing.

Gotchas
-------

**Large File Uploads**

When uploading very large files, ensure that your gateway and downstream services have sufficient memory to buffer the request for :doc:`../features/aggregation`.
Consider increasing the ``MaxRequestBodySize`` in your Ocelot configuration if needed.

**Binary Content Preservation**

The :ref:`multipart <rproxy-multipart-form-data>` format requires exact preservation of boundary markers and binary content.
Ocelot does not perform any transformation on the request body, ensuring that binary files (images, PDFs, archives) are forwarded unchanged.

**Content Encoding**

If your client applies content encoding (e.g., gzip) to the :ref:`multipart <rproxy-multipart-form-data>` request, Ocelot will preserve the encoding header.
Ensure downstream services can handle the specified encoding.

**Timeout Considerations**

Large file uploads may take longer to transmit.
Configure appropriate :ref:`config-timeout` values in your route configuration and downstream service settings to avoid premature request termination.

References
-----------

- `RFC 7578: Returning Values from Forms in HTML/XML <https://tools.ietf.org/html/rfc7578>`_
- :doc:`../features/aggregation`
- :doc:`../features/routing`
