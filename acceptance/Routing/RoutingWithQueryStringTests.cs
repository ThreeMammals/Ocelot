using Microsoft.AspNetCore.Http;

namespace Ocelot.Acceptance.Routing;

public sealed class RoutingWithQueryStringTests : Steps
{
    [Fact]
    public async Task Should_return_response_200_with_query_string_template()
    {
        var subscriptionId = Guid.NewGuid().ToString();
        var unitId = Guid.NewGuid().ToString();
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port,
            "/api/units/{subscriptionId}/{unitId}/updates",
            "/api/subscriptions/{subscriptionId}/updates?unitId={unitId}");
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOn(port, $"/api/subscriptions/{subscriptionId}/updates", "Hello from Laura");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway($"/api/units/{subscriptionId}/{unitId}/updates");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenTheResponseBodyShouldBe("Hello from Laura");
        ThenTheDownstreamUrlPathShouldBe($"/api/subscriptions/{subscriptionId}/updates");
        ThenTheDownstreamUrlQueryStringShouldBe($"?unitId={unitId}");
    }

    [Theory]
    [Trait("Bug", "952")] // https://github.com/ThreeMammals/Ocelot/issues/952
    [InlineData("")]
    [InlineData("&x=xxx")]
    public async Task Should_return_200_with_query_string_template_different_keys(string additionalParams)
    {
        var subscriptionId = Guid.NewGuid().ToString();
        var unitId = Guid.NewGuid().ToString();
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port,
            "/api/units/{subscriptionId}/updates?unit={unit}",
            "/api/subscriptions/{subscriptionId}/updates?unitId={unit}");
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOn(port, $"/api/subscriptions/{subscriptionId}/updates", "Hello from Laura");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway($"/api/units/{subscriptionId}/updates?unit={unitId}{additionalParams}");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenTheResponseBodyShouldBe("Hello from Laura");
        ThenTheDownstreamUrlPathShouldBe($"/api/subscriptions/{subscriptionId}/updates");
        ThenTheDownstreamUrlQueryStringShouldBe($"?unitId={unitId}{additionalParams}");
    }

    [Fact]
    [Trait("Bug", "952")] // https://github.com/ThreeMammals/Ocelot/issues/952
    public async Task Should_map_query_parameters_with_different_names()
    {
        const string userId = "webley";
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port,
            "/users?userId={userId}",
            "/persons?personId={userId}");
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOn(port, "/persons", "Hello from @webley");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway($"/users?userId={userId}");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenTheResponseBodyShouldBe("Hello from @webley");
        ThenTheDownstreamUrlPathShouldBe("/persons");
        ThenTheDownstreamUrlQueryStringShouldBe($"?personId={userId}");
    }

    [Fact]
    [Trait("Bug", "952")] // https://github.com/ThreeMammals/Ocelot/issues/952
    public async Task Should_map_query_parameters_with_different_names_and_save_old_param_if_placeholder_and_param_names_differ()
    {
        const string uid = "webley";
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port,
            "/users?userId={uid}",
            "/persons?personId={uid}");
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOn(port, "/persons", "Hello from @webley");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway($"/users?userId={uid}");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenTheResponseBodyShouldBe("Hello from @webley");
        ThenTheDownstreamUrlPathShouldBe("/persons");
        ThenTheDownstreamUrlQueryStringShouldBe($"?personId={uid}&userId={uid}");
    }

    [Fact]
    [Trait("Bug", "952")] // https://github.com/ThreeMammals/Ocelot/issues/952
    public async Task Should_map_query_parameters_with_different_names_and_save_old_param_if_placeholder_and_param_names_differ_case_sensitive()
    {
        const string userid = "webley";
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port,
            "/users?userId={userid}",
            "/persons?personId={userid}");
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOn(port, "/persons", "Hello from @webley");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway($"/users?userId={userid}");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenTheResponseBodyShouldBe("Hello from @webley");
        ThenTheDownstreamUrlPathShouldBe("/persons");
        ThenTheDownstreamUrlQueryStringShouldBe($"?personId={userid}&userId={userid}");
    }

    [Theory]
    [Trait("Bug", "1174")] // https://github.com/ThreeMammals/Ocelot/issues/1174
    [InlineData("projectNumber=45&startDate=2019-12-12&endDate=2019-12-12", "?projectNumber=45&startDate=2019-12-12&endDate=2019-12-12")]
    [InlineData("$filter=ProjectNumber eq 45 and DateOfSale ge 2020-03-01T00:00:00z and DateOfSale le 2020-03-15T00:00:00z", "?$filter=ProjectNumber%20eq%2045%20and%20DateOfSale%20ge%202020-03-01T00%3A00%3A00z%20and%20DateOfSale%20le%202020-03-15T00%3A00%3A00z")]
    public async Task Should_return_200_and_forward_query_parameters_without_duplicates(string everythingelse, string expected)
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port,
            "/contracts?{everythingelse}",
            "/api/contracts?{everythingelse}");
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOn(port, $"/api/contracts", "Hello from @sunilk3");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway($"/contracts?{everythingelse}");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenTheResponseBodyShouldBe("Hello from @sunilk3");
        ThenTheDownstreamUrlPathShouldBe("/api/contracts");
        ThenTheDownstreamUrlQueryStringShouldBe(expected);
    }

    [Fact]
    [Trait("Bug", "548")] // https://github.com/ThreeMammals/Ocelot/issues/548
    [Trait("Commit", "00a6000")] // https://github.com/ThreeMammals/Ocelot/commit/00a600064deea0877058d04e6189d7e0278c99a5
    [Trait("Release", "10.0.4")]
    public async Task Should_return_response_200_with_odata_query_string()
    {
        var subscriptionId = Guid.NewGuid().ToString();
        var unitId = Guid.NewGuid().ToString();
        var port = PortFinder.GetRandomPort();
        var route = GivenCatchAllRoute(port);
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOn(port, "/odata/customers", "Hello from Laura");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/odata/customers?$filter=Name eq 'Sam' ");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenTheResponseBodyShouldBe("Hello from Laura");
        ThenTheDownstreamUrlPathShouldBe("/odata/customers");
        ThenTheDownstreamUrlQueryStringShouldBe("?$filter=Name%20eq%20%27Sam%27");
    }

    [Fact]
    [Trait("Feat", "467")] // https://github.com/ThreeMammals/Ocelot/pull/467
    public async Task Should_return_response_200_with_query_string_upstream_template()
    {
        var subscriptionId = Guid.NewGuid().ToString();
        var unitId = Guid.NewGuid().ToString();
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port,
            "/api/subscriptions/{subscriptionId}/updates?unitId={unitId}",
            "/api/units/{subscriptionId}/{unitId}/updates");
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOn(port, $"/api/units/{subscriptionId}/{unitId}/updates", "Hello from Laura");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway($"/api/subscriptions/{subscriptionId}/updates?unitId={unitId}");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenTheResponseBodyShouldBe("Hello from Laura");
        ThenTheDownstreamUrlPathShouldBe($"/api/units/{subscriptionId}/{unitId}/updates");
        ThenTheDownstreamUrlQueryStringShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Feat", "467")] // https://github.com/ThreeMammals/Ocelot/pull/467
    public async Task Should_return_response_404_with_query_string_upstream_template_no_query_string()
    {
        var subscriptionId = Guid.NewGuid().ToString();
        var unitId = Guid.NewGuid().ToString();
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port,
            "/api/subscriptions/{subscriptionId}/updates?unitId={unitId}",
            "/api/units/{subscriptionId}/{unitId}/updates");
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOn(port, $"/api/units/{subscriptionId}/{unitId}/updates", "Hello from Laura");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway($"/api/subscriptions/{subscriptionId}/updates");
        ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("Feat", "467")] // https://github.com/ThreeMammals/Ocelot/pull/467
    public async Task Should_return_response_404_with_query_string_upstream_template_different_query_string()
    {
        var subscriptionId = Guid.NewGuid().ToString();
        var unitId = Guid.NewGuid().ToString();
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port,
            "/api/subscriptions/{subscriptionId}/updates?unitId={unitId}",
            "/api/units/{subscriptionId}/{unitId}/updates");
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOn(port, $"/api/units/{subscriptionId}/{unitId}/updates", "Hello from Laura");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway($"/api/subscriptions/{subscriptionId}/updates?test=1");
        ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("Feat", "467")] // https://github.com/ThreeMammals/Ocelot/pull/467
    public async Task Should_return_response_200_with_query_string_upstream_template_multiple_params()
    {
        var subscriptionId = Guid.NewGuid().ToString();
        var unitId = Guid.NewGuid().ToString();
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port,
            "/api/subscriptions/{subscriptionId}/updates?unitId={unitId}",
            "/api/units/{subscriptionId}/{unitId}/updates");
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOn(port, $"/api/units/{subscriptionId}/{unitId}/updates", "Hello from Laura");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway($"/api/subscriptions/{subscriptionId}/updates?unitId={unitId}&productId=1");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenTheResponseBodyShouldBe("Hello from Laura");
        ThenTheDownstreamUrlPathShouldBe($"/api/units/{subscriptionId}/{unitId}/updates");
        ThenTheDownstreamUrlQueryStringShouldBe("?productId=1");
    }

    [Fact]
    [Trait("Bug", "2002")] // https://github.com/ThreeMammals/Ocelot/issues/2002
    [Trait("Commit", "a034e8c")] // https://github.com/ThreeMammals/Ocelot/commit/a034e8c1e3fc23a086ad10000c85615b9696a43e
    [Trait("Release", "23.3.0")]
    public async Task Should_map_when_query_parameters_has_same_names_with_placeholder()
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
        GivenThereIsAServiceRunningOn(port, $"/account/{username}/groups/{groupName}/roles", "Hello from Béchir");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway($"/WeatherForecast/{roleid}/groups?username={username}&groupName={groupName}&{everything}");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenTheResponseBodyShouldBe("Hello from Béchir");
        ThenTheDownstreamUrlPathShouldBe($"/account/{username}/groups/{groupName}/roles");
        ThenTheDownstreamUrlQueryStringShouldBe($"?roleId={roleid}&{everything}");
    }

    /// <summary>
    /// To reproduce 1288: query string should contain the placeholder name and value.
    /// </summary>
    [Fact]
    [Trait("Bug", "1288")] // https://github.com/ThreeMammals/Ocelot/issues/1288
    public async Task Should_copy_query_string_to_downstream_path()
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
        GivenThereIsAServiceRunningOn(port, $"/cpx/t1/{idValue}", "Hello from Laura");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway($"/safe/{idValue}?{queryName}={queryValue}");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenTheResponseBodyShouldBe("Hello from Laura");
        ThenTheDownstreamUrlPathShouldBe($"/cpx/t1/{idValue}");
        ThenTheDownstreamUrlQueryStringShouldBe($"?{queryName}={queryValue}");
    }

    #region PR 2351
    [Fact]
    [Trait("PR", "2351")] // https://github.com/ThreeMammals/Ocelot/pull/2351
    [Trait("Bug", "2346")] // https://github.com/ThreeMammals/Ocelot/issues/2346
    public async Task Should_not_corrupt_query_parameter_names_containing_id_when_route_has_id_placeholder_as_a_Catch_All_Query_String()
    {
        const string customer_id = "12345";
        var port = PortFinder.GetRandomPort();
        var route1 = GivenRoute(port,
            upstream: "/finance/v1/payment-methods?{id}",
            downstream: "/v1/payment-methods?{id}");
        var route2 = GivenRoute(port,
            upstream: "/finance/v1/payment-methods",
            downstream: "/v1/payment-methods");
        var configuration = GivenConfiguration(route1, route2);
        var query = $"?{nameof(customer_id)}={customer_id}";
        GivenThereIsAServiceRunningOn(port, "/v1/payment-methods", "Hello from Bhargav");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway(route1.UpstreamPathTemplate.Replace("?{id}", query));
        ThenTheStatusCodeShouldBeOk();
        ThenTheResponseBodyShouldBe("Hello from Bhargav");
        ThenTheDownstreamUrlPathShouldBe("/v1/payment-methods");
        ThenTheDownstreamUrlQueryStringShouldBe(query);
        await WhenIGetUrlOnTheApiGateway(route2.UpstreamPathTemplate);
        ThenTheStatusCodeShouldBeOk();
        ThenTheDownstreamUrlPathShouldBe("/v1/payment-methods");
        ThenTheDownstreamUrlQueryStringShouldBe(string.Empty);
    }

    [Theory]
    [Trait("PR", "2351")] // https://github.com/ThreeMammals/Ocelot/pull/2351
    [Trait("Bug", "2346")] // https://github.com/ThreeMammals/Ocelot/issues/2346
    [InlineData("/finance/v1/payment-methods/{id}", "/v1/payment-methods/{id}",
        "/finance/v1/payment-methods/?customer_id=123",
        "/v1/payment-methods/", "?customer_id=123")]
    [InlineData("/finance/v1/payment-methods/{id}/orders/{oid}", "/v1/orders/{id}/{oid}",
        "/finance/v1/payment-methods/1/orders/2?id=1&oid=2&orderid=3&customer_id=4",
        "/v1/orders/1/2", "?orderid=3&customer_id=4")]
    [InlineData("/finance/v1/users/{customer_id}", "/v1/users/{customer_id}",
        "/finance/v1/users/123?id=999&customer_id=x",
        "/v1/users/123", "?id=999")]
    [InlineData("/finance/v1/items/{id}/{Id}", "/v1/items/{Id}/{id}",
        "/finance/v1/items/1/2?id=3&Id=4&ID=5&extra=x",
        "/v1/items/2/1", "?extra=x")]
    [InlineData("/finance/v1/data/{id}", "/v1/data/{id}",
        "/finance/v1/data/123/?xid=1&idx=2&id=3&extra=x",
        "/v1/data/123/", "?xid=1&idx=2&extra=x")]
    [InlineData("/finance/v1/records/{id1}", "/v1/records/{id1}",
        "/finance/v1/records/123/?id1=1&id10=10&extra=x",
        "/v1/records/123/", "?id10=10&extra=x")]
    [InlineData("/finance/v1/alpha/{id}", "/v1/alpha/{id}",
        "/finance/v1/alpha/123/?id_=1&_id=2&id=3&extra=x",
        "/v1/alpha/123/", "?id_=1&_id=2&extra=x")]
    [InlineData("/finance/v2/orders/{id}", "/v2/orders/{id}",
        "/finance/v2/orders/42?status=active&customer_id=123&extra=x",
        "/v2/orders/42", "?status=active&customer_id=123&extra=x")]
    public async Task Should_not_corrupt_query_parameter_names_containing_id_when_route_has_id_placeholder(
        string upstream, string downstream, string url, string path, string query)
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, upstream, downstream);
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOn(port, path, "Hello from Bhargav");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway(url);
        ThenTheStatusCodeShouldBeOk();
        ThenTheResponseBodyShouldBe("Hello from Bhargav");
        ThenTheDownstreamUrlPathShouldBe(path);
        ThenTheDownstreamUrlQueryStringShouldBe(query);
    }
    #endregion of PR 2351

    private void GivenThereIsAServiceRunningOn(int port, string basePath, string responseBody)
    {
        handler.GivenThereIsAServiceRunningOn(port, basePath, MapStatusCode);

        Task MapStatusCode(HttpContext context)
        {
            _downstreamPath = !string.IsNullOrEmpty(context.Request.PathBase.Value)
                ? context.Request.PathBase.Value + context.Request.Path.Value
                : context.Request.Path.Value;
            _downstreamQuery = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
            bool oK = _downstreamPath == basePath; // || _downstreamQuery != queryString; // TODO this is strict assertion, it can be without query string
            context.Response.StatusCode = oK ? StatusCodes.Status200OK : StatusCodes.Status404NotFound;
            return context.Response.WriteAsync(oK ? responseBody : "Downstream path didn't match base path");
        }
    }

    private string _downstreamPath;
    private string _downstreamQuery;
    private void ThenTheDownstreamUrlPathShouldBe(string expected) => _downstreamPath.ShouldBe(expected);
    private void ThenTheDownstreamUrlQueryStringShouldBe(string expected) => _downstreamQuery.ShouldBe(expected);
}
