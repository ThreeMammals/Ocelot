Request Proxying
================
.. contents:: Table of Contents
   :depth: 2
   :local:

Ocelot's core responsibility is proxying HTTP requests from upstream clients to downstream services.
This section covers key request proxying capabilities and special handling for different request types.

Basic Request Routing
---------------------

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

Request Headers and Metadata
-----------------------------

Ocelot preserves HTTP headers during request forwarding, ensuring that important metadata (authentication, content type, custom headers) is maintained across the gateway.
You can configure header transformation rules to add, remove, or modify headers as needed.

Multipart Form Data
-------------------

Ocelot supports routing and multiplexing of HTTP requests with ``multipart/form-data`` content type, which is commonly used for file uploads and form submissions with binary data.

Overview
^^^^^^^^

When clients upload files or submit forms with mixed text and binary data, they use the ``multipart/form-data`` content type (as defined in `RFC 7578 <https://tools.ietf.org/html/rfc7578>`_).
Ocelot routes these requests to downstream services while preserving the multipart structure and file boundaries.

Key Considerations
^^^^^^^^^^^^^^^^^^^

**Request Body Buffering**

When routing multipart requests, especially in scenarios involving :doc:`../features/aggregation` or multiplexing to multiple downstream services, the request body must be buffered in memory.
This allows Ocelot to read the request body once and forward it to multiple destinations without losing data.

**Preservation of Boundaries**

Multipart requests contain boundary markers that separate form fields and file data.
Ocelot preserves these boundaries and the original ``Content-Type`` header (including the boundary parameter) to ensure downstream services can correctly parse the request.

**Content-Type Headers**

The ``Content-Type`` header is automatically set by HTTP clients and includes a boundary identifier.
Ocelot does not modify this header during routing, ensuring compatibility with downstream services.

Basic Multipart Routing
^^^^^^^^^^^^^^^^^^^^^^^

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

Multiplexing with Multipart Data
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Ocelot can multiplex a single multipart request to multiple downstream services.
Each downstream service receives an identical copy of the original request, including all form fields and uploaded files.

**Configuration**

To multiplex a request to multiple services, define multiple ``DownstreamHostAndPorts`` entries:

.. code-block:: json

  {
    "Routes": [
      {
        "UpstreamHttpMethod": [ "Post" ],
        "UpstreamPathTemplate": "/upload",
        "DownstreamPathTemplate": "/api/files/upload",
        "DownstreamScheme": "http",
        "DownstreamHostAndPorts": [
          { "Host": "service1.example.com", "Port": 5000 },
          { "Host": "service2.example.com", "Port": 5000 }
        ]
      }
    ]
  }

In this configuration, when a client uploads a file to ``/upload``, Ocelot forwards the entire multipart request to both ``service1`` and ``service2``.
The request body is buffered and cloned for each downstream destination, ensuring each service receives the complete and unmodified multipart data.

**Load Balancing**

When multiplexing multipart requests across multiple services, you may configure a :doc:`../features/loadbalancer` to distribute requests across services.
Refer to the Load Balancer documentation for additional configuration options.

Aggregation with Multipart Data
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Ocelot also supports aggregating responses from multipart requests.
This is useful when you need to combine responses from multiple services that each process the same file upload.

.. code-block:: json

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
2. Forwards the multipart request to both services
3. Collects the responses from both services
4. Aggregates them into a single JSON response

Sample Application
^^^^^^^^^^^^^^^^^^

Ocelot includes a ``MultipartFormData`` sample application in the ``samples/`` directory that demonstrates file upload handling with a polished user interface.
The sample showcases:

- A file upload form with progress indicators
- Routing of multipart requests to a downstream service
- Real-time logging of request and response data
- File content inspection interface for large uploads

To run the sample:

.. code-block:: bash

  cd samples/MultipartFormData
  dotnet run

Then navigate to ``http://localhost:8000`` and use the upload interface to test multipart routing.

Testing Multipart Routing
^^^^^^^^^^^^^^^^^^^^^^^^^^

When writing acceptance tests for multipart form-data routing, you should:

1. Create a test form with both text fields and file data
2. Verify that the downstream service receives the complete multipart request
3. In multiplexing scenarios, ensure all downstream services receive identical copies
4. Validate that file boundaries and content are preserved during transmission

Ocelot's acceptance test suite includes comprehensive tests for multipart scenarios in ``acceptance/Request/MultipartFormDataTests.cs``.
These tests validate:

- Basic multipart request routing
- Aggregation of multipart request responses
- Multiplexing of multipart requests to multiple services
- Preservation of file content and metadata through the gateway

Multipart Gotchas
^^^^^^^^^^^^^^^^^

**Large File Uploads**

When uploading very large files, ensure that your gateway and downstream services have sufficient memory to buffer the request.
Consider increasing the ``MaxRequestBodySize`` in your Ocelot configuration if needed.

**Binary Content Preservation**

The multipart format requires exact preservation of boundary markers and binary content.
Ocelot does not perform any transformation on the request body, ensuring that binary files (images, PDFs, archives) are forwarded unchanged.

**Content Encoding**

If your client applies content encoding (e.g., gzip) to the multipart request, Ocelot will preserve the encoding header.
Ensure downstream services can handle the specified encoding.

**Timeout Considerations**

Large file uploads may take longer to transmit.
Configure appropriate timeout values in your route configuration and downstream service settings to avoid premature request termination.

References
-----------

- `RFC 7578: Returning Values from Forms in HTML/XML <https://tools.ietf.org/html/rfc7578>`_
- :doc:`../features/aggregation`
- :doc:`../features/routing`
