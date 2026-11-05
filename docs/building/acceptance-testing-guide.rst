Acceptance Testing Guide
========================
.. contents:: Table of Contents
   :depth: 2
   :local:

This guide provides best practices and examples for writing acceptance tests in Ocelot, with a focus on testing real-world scenarios involving routing, aggregation, and request handling.

Overview
--------

Ocelot's acceptance test suite verifies the gateway's behavior end-to-end by:

1. Starting one or more downstream test services
2. Configuring Ocelot routes pointing to these services
3. Sending HTTP requests through the gateway
4. Validating that the gateway correctly forwards requests and responses

This approach tests the complete request/response pipeline, including middleware, routing logic, and header transformations.

Test Structure
--------------

Acceptance tests typically follow this pattern:

.. code-block:: csharp

  [Fact]
  public async Task Should_route_request_to_downstream_service()
  {
    // Arrange: Set up test services and routes
    var downstreamPort = 5001;
    GivenThereIsAServiceRunningOn(downstreamPort, "/api/endpoint", async context =>
    {
      await context.Response.WriteAsJsonAsync(new { message = "success" });
    });

    var routeConfig = new Route
    {
      UpstreamHttpMethod = new[] { "Get" },
      UpstreamPathTemplate = "/gateway/endpoint",
      DownstreamPathTemplate = "/api/endpoint",
      DownstreamScheme = "http",
      DownstreamHostAndPorts = new[] { new DownstreamHostAndPort { Host = "localhost", Port = downstreamPort } }
    };

    GivenThereIsAConfiguration(routeConfig);
    GivenOcelotIsRunning();

    // Act: Send request through the gateway
    var response = await MakeARequest("/gateway/endpoint");

    // Assert: Verify the response
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var content = await response.Content.ReadAsAsync<dynamic>();
    content.message.Should().Be("success");
  }

**Key Points:**

- **GivenThereIsAServiceRunningOn**: Starts a test service that responds to requests
- **GivenThereIsAConfiguration**: Registers Ocelot configuration (routes, aggregates, etc.)
- **GivenOcelotIsRunning**: Starts the Ocelot gateway
- **MakeARequest**: Sends an HTTP request through the gateway
- **Assertions**: Verify response status, headers, body content

Testing Multipart Form Data
----------------------------

When testing multipart requests, you need to construct the request body with proper boundary markers.

**Basic Multipart Routing Test**

.. code-block:: csharp

  [Fact]
  public async Task Should_route_multipart_form_data_with_file()
  {
    var downstreamPort = 5002;
    var capturedRequest = new MultipartFormDataContent();

    GivenThereIsAServiceRunningOn(downstreamPort, "/api/upload", async context =>
    {
      // Capture the multipart request for verification
      var form = await context.Request.ReadFormAsync();
      capturedRequest = form;

      context.Response.StatusCode = StatusCodes.Status200OK;
      await context.Response.WriteAsJsonAsync(new { filesReceived = form.Files.Count });
    });

    var routeConfig = new Route
    {
      UpstreamHttpMethod = new[] { "Post" },
      UpstreamPathTemplate = "/upload",
      DownstreamPathTemplate = "/api/upload",
      DownstreamScheme = "http",
      DownstreamHostAndPorts = new[] { new DownstreamHostAndPort { Host = "localhost", Port = downstreamPort } }
    };

    GivenThereIsAConfiguration(routeConfig);
    GivenOcelotIsRunning();

    // Create multipart request
    var content = new MultipartFormDataContent();
    content.Add(new StringContent("test description"), "description");
    content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("file content")), "file", "test.txt");

    // Act
    var response = await MakeARequest("/upload", HttpMethod.Post, content);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    capturedRequest.Files.Count.Should().Be(1);
    capturedRequest["description"].Should().Be("test description");
  }

**Multiplexing Test**

When testing multiplexing (forwarding a single request to multiple downstream services), use helper methods to capture requests at all destinations:

.. code-block:: csharp

  [Fact]
  public async Task Should_multiplex_multipart_form_data_to_multiple_services()
  {
    var service1Port = 5003;
    var service2Port = 5004;
    var service1Requests = new List<(string field, string fileName, string content)>();
    var service2Requests = new List<(string field, string fileName, string content)>();

    // Service 1 captures requests
    GivenThereIsAServiceRunningOn(service1Port, "/api/files", async context =>
    {
      var form = await context.Request.ReadFormAsync();
      var field = form["description"].ToString();
      var file = form.Files["file"];
      var fileContent = await new StreamReader(file.OpenReadStream()).ReadToEndAsync();

      service1Requests.Add((field, file.FileName, fileContent));
      context.Response.StatusCode = StatusCodes.Status200OK;
      await context.Response.WriteAsJsonAsync(new { received = true });
    });

    // Service 2 captures requests
    GivenThereIsAServiceRunningOn(service2Port, "/api/files", async context =>
    {
      var form = await context.Request.ReadFormAsync();
      var field = form["description"].ToString();
      var file = form.Files["file"];
      var fileContent = await new StreamReader(file.OpenReadStream()).ReadToEndAsync();

      service2Requests.Add((field, file.FileName, fileContent));
      context.Response.StatusCode = StatusCodes.Status200OK;
      await context.Response.WriteAsJsonAsync(new { received = true });
    });

    // Configure route with multiple downstream hosts
    var routeConfig = new Route
    {
      UpstreamHttpMethod = new[] { "Post" },
      UpstreamPathTemplate = "/upload",
      DownstreamPathTemplate = "/api/files",
      DownstreamScheme = "http",
      DownstreamHostAndPorts = new[]
      {
        new DownstreamHostAndPort { Host = "localhost", Port = service1Port },
        new DownstreamHostAndPort { Host = "localhost", Port = service2Port }
      }
    };

    GivenThereIsAConfiguration(routeConfig);
    GivenOcelotIsRunning();

    // Create and send multipart request
    var content = new MultipartFormDataContent();
    content.Add(new StringContent("test description"), "description");
    content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("file content")), "file", "test.txt");

    // Act
    var response = await MakeARequest("/upload", HttpMethod.Post, content);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    service1Requests.Should().HaveCount(1);
    service2Requests.Should().HaveCount(1);
    
    // Both services should receive identical data
    service1Requests[0].field.Should().Be("test description");
    service2Requests[0].field.Should().Be("test description");
    service1Requests[0].content.Should().Be(service2Requests[0].content);
  }

Testing Headers and Metadata
-----------------------------

Capture and verify headers passed through the gateway:

.. code-block:: csharp

  [Fact]
  public async Task Should_preserve_custom_headers_in_multipart_request()
  {
    var downstreamPort = 5005;
    var capturedHeaders = new Dictionary<string, string>();

    GivenThereIsAServiceRunningOn(downstreamPort, "/api/upload", async context =>
    {
      // Capture all request headers
      foreach (var header in context.Request.Headers)
      {
        capturedHeaders[header.Key] = header.Value.ToString();
      }

      context.Response.StatusCode = StatusCodes.Status200OK;
      await context.Response.WriteAsync("OK");
    });

    var routeConfig = new Route
    {
      UpstreamHttpMethod = new[] { "Post" ],
      UpstreamPathTemplate = "/upload",
      DownstreamPathTemplate = "/api/upload",
      DownstreamScheme = "http",
      DownstreamHostAndPorts = new[] { new DownstreamHostAndPort { Host = "localhost", Port = downstreamPort } }
    };

    GivenThereIsAConfiguration(routeConfig);
    GivenOcelotIsRunning();

    var content = new MultipartFormDataContent();
    content.Add(new StringContent("data"), "field1");

    var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:8000/upload");
    request.Content = content;
    request.Headers.Add("X-Custom-Header", "custom-value");

    // Act
    var response = await _httpClient.SendAsync(request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    capturedHeaders.Should().ContainKey("X-Custom-Header");
    capturedHeaders["X-Custom-Header"].Should().Be("custom-value");
  }

Best Practices
--------------

**1. Use Descriptive Test Names**

Test names should clearly describe what scenario is being tested:

.. code-block:: csharp

  // Good
  Should_route_multipart_form_data_with_file_to_single_downstream_service

  // Avoid
  Test_Multipart_Routing

**2. Clean Up Resources**

Always dispose of services and clients to avoid port conflicts:

.. code-block:: csharp

  public void Dispose()
  {
    _ocelotServer?.Dispose();
    _httpClient?.Dispose();
    // Clean up test services
  }

**3. Use Assertions with Fluent API**

Use FluentAssertions for readable and maintainable assertions:

.. code-block:: csharp

  response.StatusCode.Should().Be(HttpStatusCode.OK);
  response.Content.Headers.ContentType.MediaType.Should().Be("application/json");
  capturedRequest.Files.Should().HaveCount(1);

**4. Capture Request/Response Data**

Store request and response data for detailed assertions:

.. code-block:: csharp

  var capturedRequests = new List<HttpContext>();
  GivenThereIsAServiceRunningOn(port, "/endpoint", async context =>
  {
    capturedRequests.Add(context);
    // Handle request
  });

**5. Test Edge Cases**

Include tests for boundary conditions and error scenarios:

.. code-block:: csharp

  // Test with large file
  // Test with special characters in field names
  // Test with missing required fields
  // Test with invalid content type
  // Test with timeout scenarios

Debugging Failed Tests
----------------------

If a test fails, capture detailed diagnostic information:

.. code-block:: csharp

  // Log request details
  Console.WriteLine($"Status: {response.StatusCode}");
  Console.WriteLine($"Headers: {string.Join(", ", response.Headers)}");
  var responseBody = await response.Content.ReadAsStringAsync();
  Console.WriteLine($"Body: {responseBody}");

  // Verify configuration
  Console.WriteLine($"Routes: {config.Routes.Count}");
  foreach (var route in config.Routes)
  {
    Console.WriteLine($"  {route.UpstreamPathTemplate} -> {route.DownstreamPathTemplate}");
  }

References
-----------

- `xUnit Documentation <https://xunit.net/docs/getting-started/netcore>`_
- `FluentAssertions Documentation <https://fluentassertions.com/>`_
- :doc:`../features/multipart-formdata`
- :doc:`../features/routing`
- :doc:`../features/aggregation`
