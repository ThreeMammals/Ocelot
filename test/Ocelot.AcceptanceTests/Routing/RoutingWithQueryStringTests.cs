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
