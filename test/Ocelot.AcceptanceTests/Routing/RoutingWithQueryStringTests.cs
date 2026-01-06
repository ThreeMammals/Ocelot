using Microsoft.AspNetCore.Http;

namespace Ocelot.AcceptanceTests.Routing;

public sealed class RoutingWithQueryStringTests : Steps
{
    public RoutingWithQueryStringTests()
    {
    }

    [Fact]
    public void Should_return_response_200_with_query_string_template()
    {
        var subscriptionId = Guid.NewGuid().ToString();
        var unitId = Guid.NewGuid().ToString();
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port,
            "/api/units/{subscriptionId}/{unitId}/updates",
            "/api/subscriptions/{subscriptionId}/updates?unitId={unitId}");
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, $"/api/subscriptions/{subscriptionId}/updates", $"?unitId={unitId}", "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway($"/api/units/{subscriptionId}/{unitId}/updates"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Theory]
    [Trait("Bug", "952")]
    [InlineData("")]
    [InlineData("&x=xxx")]
    public void Should_return_200_with_query_string_template_different_keys(string additionalParams)
    {
        var subscriptionId = Guid.NewGuid().ToString();
        var unitId = Guid.NewGuid().ToString();
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port,
            "/api/units/{subscriptionId}/updates?unit={unit}",
            "/api/subscriptions/{subscriptionId}/updates?unitId={unit}");
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, $"/api/subscriptions/{subscriptionId}/updates", $"?unitId={unitId}{additionalParams}", "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway($"/api/units/{subscriptionId}/updates?unit={unitId}{additionalParams}"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact]
    [Trait("Bug", "952")]
    public void Should_map_query_parameters_with_different_names()
    {
        const string userId = "webley";
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port,
            "/users?userId={userId}",
            "/persons?personId={userId}");
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/persons", $"?personId={userId}", "Hello from @webley"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway($"/users?userId={userId}"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from @webley"))
            .BDDfy();
    }

    [Fact]
    [Trait("Bug", "952")]
    public void Should_map_query_parameters_with_different_names_and_save_old_param_if_placeholder_and_param_names_differ()
    {
        const string uid = "webley";
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port,
            "/users?userId={uid}",
            "/persons?personId={uid}");
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/persons", $"?personId={uid}&userId={uid}", "Hello from @webley"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway($"/users?userId={uid}"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from @webley"))
            .BDDfy();
    }

    [Fact]
    [Trait("Bug", "952")]
    public void Should_map_query_parameters_with_different_names_and_save_old_param_if_placeholder_and_param_names_differ_case_sensitive()
    {
        const string userid = "webley";
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port,
            "/users?userId={userid}",
            "/persons?personId={userid}");
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/persons", $"?personId={userid}&userId={userid}", "Hello from @webley"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway($"/users?userId={userid}"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from @webley"))
            .BDDfy();
    }

    [Theory]
    [Trait("Bug", "1174")]
    [InlineData("projectNumber=45&startDate=2019-12-12&endDate=2019-12-12", "projectNumber=45&startDate=2019-12-12&endDate=2019-12-12")]
    [InlineData("$filter=ProjectNumber eq 45 and DateOfSale ge 2020-03-01T00:00:00z and DateOfSale le 2020-03-15T00:00:00z", "$filter=ProjectNumber%20eq%2045%20and%20DateOfSale%20ge%202020-03-01T00:00:00z%20and%20DateOfSale%20le%202020-03-15T00:00:00z")]
    public void Should_return_200_and_forward_query_parameters_without_duplicates(string everythingelse, string expected)
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port,
            "/contracts?{everythingelse}",
            "/api/contracts?{everythingelse}");
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, $"/api/contracts", $"?{expected}", "Hello from @sunilk3"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway($"/contracts?{everythingelse}"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from @sunilk3"))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_200_with_odata_query_string()
    {
        var subscriptionId = Guid.NewGuid().ToString();
        var unitId = Guid.NewGuid().ToString();
        var port = PortFinder.GetRandomPort();
        var route = GivenCatchAllRoute(port);
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/odata/customers", "?$filter=Name%20eq%20'Sam'", "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/odata/customers?$filter=Name eq 'Sam' "))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact]
    [Trait("Feat", "467")]
    public void Should_return_response_200_with_query_string_upstream_template()
    {
        var subscriptionId = Guid.NewGuid().ToString();
        var unitId = Guid.NewGuid().ToString();
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port,
            "/api/subscriptions/{subscriptionId}/updates?unitId={unitId}",
            "/api/units/{subscriptionId}/{unitId}/updates");
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, $"/api/units/{subscriptionId}/{unitId}/updates", string.Empty, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway($"/api/subscriptions/{subscriptionId}/updates?unitId={unitId}"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact]
    [Trait("Feat", "467")]
    public void Should_return_response_404_with_query_string_upstream_template_no_query_string()
    {
        var subscriptionId = Guid.NewGuid().ToString();
        var unitId = Guid.NewGuid().ToString();
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port,
            "/api/subscriptions/{subscriptionId}/updates?unitId={unitId}",
            "/api/units/{subscriptionId}/{unitId}/updates");
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, $"/api/units/{subscriptionId}/{unitId}/updates", string.Empty, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway($"/api/subscriptions/{subscriptionId}/updates"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
            .BDDfy();
    }

    [Fact]
    [Trait("Feat", "467")]
    public void Should_return_response_404_with_query_string_upstream_template_different_query_string()
    {
        var subscriptionId = Guid.NewGuid().ToString();
        var unitId = Guid.NewGuid().ToString();
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port,
            "/api/subscriptions/{subscriptionId}/updates?unitId={unitId}",
            "/api/units/{subscriptionId}/{unitId}/updates");
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, $"/api/units/{subscriptionId}/{unitId}/updates", string.Empty, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway($"/api/subscriptions/{subscriptionId}/updates?test=1"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
            .BDDfy();
    }

    [Fact]
    [Trait("Feat", "467")]
    public void Should_return_response_200_with_query_string_upstream_template_multiple_params()
    {
        var subscriptionId = Guid.NewGuid().ToString();
        var unitId = Guid.NewGuid().ToString();
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port,
            "/api/subscriptions/{subscriptionId}/updates?unitId={unitId}",
            "/api/units/{subscriptionId}/{unitId}/updates");
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, $"/api/units/{subscriptionId}/{unitId}/updates", "?productId=1", "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway($"/api/subscriptions/{subscriptionId}/updates?unitId={unitId}&productId=1"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact]
    [Trait("Bug", "2002")]
    public void Should_map_when_query_parameters_has_same_names_with_placeholder()
    {
        const string username = "bbenameur";
        const string groupName = "Paris";
        const string roleid = "123456";
        const string everything = "something=9874565";
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port,
            "/WeatherForecast/{roleid}/groups?username={username}&groupName={groupName}&{everything}",
            "/account/{username}/groups/{groupName}/roles?roleId={roleid}&{everything}");
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port,
                $"/account/{username}/groups/{groupName}/roles",
                $"?roleId={roleid}&{everything}",
                "Hello from Béchir"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway($"/WeatherForecast/{roleid}/groups?username={username}&groupName={groupName}&{everything}"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Béchir"))
            .BDDfy();
    }

    /// <summary>
    /// To reproduce 1288: query string should contain the placeholder name and value.
    /// </summary>
    [Fact]
    [Trait("Bug", "1288")]
    public void Should_copy_query_string_to_downstream_path()
    {
        var idName = "id";
        var idValue = "3";
        var queryName = idName + "1";
        var queryValue = "2" + idValue + "12";
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port,
            $"/safe/{{{idName}}}",
            $"/cpx/t1/{{{idName}}}");
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, $"/cpx/t1/{idValue}", $"?{queryName}={queryValue}", "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway($"/safe/{idValue}?{queryName}={queryValue}"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact]
    [Trait("Bug", "2346")]
    public void Should_not_corrupt_query_parameter_names_containing_id_when_route_has_id_placeholder_acceptance()
    {
        // This test ensures that a placeholder like {id} does not remove or corrupt parameters like customer_id
        var id = "123";
        var customerId = "12345";
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port,
            "/v1/payment-methods",
            "/v1/payment-methods");
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/v1/payment-methods", $"?customer_id={customerId}", "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway($"/v1/payment-methods?customer_id={customerId}"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact]
    [Trait("Bug", "2346")]
    public void Should_not_remove_query_parameter_when_placeholder_is_substring_of_param_name_acceptance()
    {
        // Placeholder: {id}, Query: orderid, customer_id
        var port = PortFinder.GetRandomPort();
        var customerId = "12345";
        var orderId = "999";
        var route = GivenRoute(port,
            "/v1/orders",
            "/v1/orders");
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/v1/orders", $"?orderid={orderId}&customer_id={customerId}", "Order OK"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway($"/v1/orders?orderid={orderId}&customer_id={customerId}"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Order OK"))
            .BDDfy();
    }

    [Fact]
    [Trait("Bug", "2346")]
    public void Should_not_remove_query_parameter_when_placeholder_is_superstring_of_param_name_acceptance()
    {
        // Placeholder: {customer_id}, Query: id
        var port = PortFinder.GetRandomPort();
        var id = "999";
        var route = GivenRoute(port,
            "/v1/users",
            "/v1/users");
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/v1/users", $"?id={id}", "User OK"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway($"/v1/users?id={id}"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("User OK"))
            .BDDfy();
    }

    [Fact]
    [Trait("Bug", "2346")]
    public void Should_not_remove_query_parameter_when_placeholder_and_param_differ_by_case_acceptance()
    {
        // Placeholder: {Id}, Query: id, Id
        var port = PortFinder.GetRandomPort();
        var id = "999";
        var Id = "123";
        var route = GivenRoute(port,
            "/v1/items",
            "/v1/items");
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/v1/items", $"?id={id}&Id={Id}", "Item OK"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway($"/v1/items?id={id}&Id={Id}"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Item OK"))
            .BDDfy();
    }

    [Fact]
    [Trait("Bug", "2346")]
    public void Should_not_remove_query_parameter_when_placeholder_is_surrounded_by_other_characters_acceptance()
    {
        // Placeholder: {id}, Query: xid, idx, id
        var port = PortFinder.GetRandomPort();
        var xid = "1";
        var idx = "2";
        var id = "3";
        var route = GivenRoute(port,
            "/v1/data",
            "/v1/data");
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/v1/data", $"?xid={xid}&idx={idx}&id={id}", "Data OK"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway($"/v1/data?xid={xid}&idx={idx}&id={id}"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Data OK"))
            .BDDfy();
    }

    [Fact]
    [Trait("Bug", "2346")]
    public void Should_not_remove_query_parameter_when_placeholder_is_numeric_acceptance()
    {
        // Placeholder: {id1}, Query: id1, id10
        var port = PortFinder.GetRandomPort();
        var id1 = "123";
        var id10 = "456";
        var route = GivenRoute(port,
            "/v1/records",
            "/v1/records");
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/v1/records", $"?id1={id1}&id10={id10}", "Records OK"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway($"/v1/records?id1={id1}&id10={id10}"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Records OK"))
            .BDDfy();
    }

    [Fact]
    [Trait("Bug", "2346")]
    public void Should_not_remove_query_parameter_when_placeholder_is_prefix_or_suffix_acceptance()
    {
        // Placeholder: {id}, Query: id_, _id, id
        var port = PortFinder.GetRandomPort();
        var id_ = "1";
        var _id = "2";
        var id = "3";
        var route = GivenRoute(port,
            "/v1/alpha",
            "/v1/alpha");
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/v1/alpha", $"?id_={id_}&_id={_id}&id={id}", "Alpha OK"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway($"/v1/alpha?id_={id_}&_id={_id}&id={id}"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Alpha OK"))
            .BDDfy();
    }

    [Fact]
    [Trait("Bug", "2346")]
    public void Should_preserve_query_parameters_with_id_suffix_when_multiple_query_parameters_exist()
    {
        // Arrange
        const string customerId = "456789";
        const string status = "active";
        var port = PortFinder.GetRandomPort();

        var route = GivenRoute(
            port,
            "/v2/orders/{id}",
            "/v2/orders/{id}"
        );

        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(
                port,
                "/v2/orders/42",
                $"?status={status}&customer_id={customerId}",
                "orders-response"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())

            // Act
            .When(x => WhenIGetUrlOnTheApiGateway(
                $"/v2/orders/42?status={status}&customer_id={customerId}"))

            // Assert
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("orders-response"))
            .BDDfy();
    }


    //private static FileRoute GivenRouteWithKey(int port, string downstream, string upstream) => new()
    //{
    //    DownstreamPathTemplate = downstream,
    //    DownstreamScheme = Uri.UriSchemeHttp,
    //    DownstreamHostAndPorts = new()
    //    {
    //        new("localhost", port),
    //    },
    //    UpstreamPathTemplate = upstream,
    //    UpstreamHttpMethod = new() { HttpMethods.Get },
    //};
    private void GivenThereIsAServiceRunningOn(int port, string basePath, string queryString, string responseBody)
    {
        handler.GivenThereIsAServiceRunningOn(port, basePath, context =>
        {
            bool failed = context.Request.PathBase.Value != basePath || context.Request.QueryString.Value != queryString;
            context.Response.StatusCode = failed ? StatusCodes.Status500InternalServerError : StatusCodes.Status200OK;
            return context.Response.WriteAsync(failed ? "downstream path didnt match base path" : responseBody);
        });
    }
}
