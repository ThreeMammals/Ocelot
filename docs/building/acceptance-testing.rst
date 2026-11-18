Acceptance Testing
==================
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

Acceptance tests typically follow this pattern when the test class inherits from :class:`Steps`:

.. code-block:: csharp

  [Fact]
  public async Task Should_route_request_to_downstream_service()
  {
      // Arrange: Set up test services and routes
      var downstreamPort = PortFinder.GetRandomPort();
      GivenThereIsAServiceRunningOn(downstreamPort, "/api/endpoint",
          async context =>
          {
              await context.Response.WriteAsJsonAsync(new { message = "success" }, context.RequestAborted);
          });
      var route = GivenRoute(downstreamPort, "/gateway/endpoint", "/api/endpoint");
      var configuration = GivenConfiguration(route);
      GivenThereIsAConfiguration(configuration);
      GivenOcelotIsRunning();

      // Act: Send request through the gateway
      await WhenIGetUrlOnTheApiGateway("/gateway/endpoint");

      // Assert: Verify the response
      ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
      await ThenTheResponseBodyShouldBeAsync("{\"message\":\"success\"}");

      var content = await response.Content.ReadFromJsonAsync<JsonElement>(CancelMe);
      content.ValueKind.ShouldBe(JsonValueKind.Object); // sanity check
      content.GetProperty("message").GetString().ShouldBe("success");
  }

Where

- ``GivenThereIsAServiceRunningOn``: Starts a test service that responds to requests.
- ``GivenThereIsAConfiguration``: Registers Ocelot configuration (routes, aggregates, etc.).
- ``GivenOcelotIsRunning``: Starts the Ocelot gateway.
- ``WhenIGetUrlOnTheApiGateway``: Sends an HTTP GET request through the gateway, tracking the response internally.
- ``Then``-assertions: Verify response status, headers, body content.

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

Always dispose ``IDisposable`` services when the test class inherits from :class:`Steps`:

.. code-block:: csharp

  public override void Dispose()
  {
      _myService.Dispose();
      base.Dispose();
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
    var responseBody = await response.Content.ReadAsStringAsync(CancelMe);
    Console.WriteLine($"Body: {responseBody}");

    // Verify configuration
    Console.WriteLine($"Routes: {configuration.Routes.Count}");
    foreach (var rt in configuration.Routes)
    {
        Console.WriteLine($"  {rt.UpstreamPathTemplate} -> {rt.DownstreamPathTemplate}");
    }

References
-----------

- `xUnit Documentation <https://xunit.net/docs/getting-started/netcore>`_
- `FluentAssertions Documentation <https://fluentassertions.com/>`_
