using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Acceptance.Authentication;
using Ocelot.Authorization;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Multiplexer;
using Ocelot.Testing.Authentication;
using System.Net.Mime;
using System.Text;

namespace Ocelot.Acceptance.Aggregation;

public sealed class AggregateTests : AuthSteps
{
    [Fact]
    [Trait("Bug", "597")] // https://github.com/ThreeMammals/Ocelot/issues/597
    [Trait("PR", "600")] // https://github.com/ThreeMammals/Ocelot/pull/600
    [Trait("Release", "11.0.3")] // https://github.com/ThreeMammals/Ocelot/releases/tag/11.0.3
    public async Task Should_fix_issue_597()
    {
        var port = GivenPorts(1)[0];
        var route1 = GivenAggRoute(port, "key1", "/key1data/{userid}", "/api/values?MailId={userid}");
        var route2 = GivenAggRoute(port, "key2", "/key2data/{userid}", "/api/values?MailId={userid}");
        var route3 = GivenAggRoute(port, "key3", "/key3data/{userid}", "/api/values?MailId={userid}");
        var route4 = GivenAggRoute(port, "key4", "/key4data/{userid}", "/api/values?MailId={userid}");
        var configuration = GivenConfiguration(route1, route2, route3, route4);
        configuration.Aggregates[0].UpstreamPathTemplate = "/EmpDetail/IN/{userid}";
        configuration.Aggregates[0].UpstreamHost = null;
        configuration.Aggregates.Add(new()
        {
            RouteKeys = ["key1", "key2"],
            UpstreamPathTemplate = "/EmpDetail/US/{userid}",
        });
        configuration.GlobalConfiguration.RequestIdKey = "CorrelationID";
        var expected = "{\"key1\":some_data,\"key2\":some_data}";

        GivenThereIsAServiceRunningOn(port, "some_data"); // uses MapStatus, and status is OK
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/EmpDetail/US/1");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(expected);
    }

    [Fact]
    [Trait("Feat", "661")] // https://github.com/ThreeMammals/Ocelot/issues/661
    [Trait("PR", "704")] // https://github.com/ThreeMammals/Ocelot/pull/704
    [Trait("Release", "13.2.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/13.2.0
    public async Task Should_return_response_200_with_advanced_aggregate_configs()
    {
        var ports = GivenPorts(3);
        var route1 = GivenAggRoute(ports[0], "Comments", "/Comments", "/");
        var route2 = GivenAggRoute(ports[1], "UserDetails", "/UserDetails/{userId}", "/users/{userId}");
        var route3 = GivenAggRoute(ports[2], "PostDetails", "/PostDetails/{postId}", "/posts/{postId}");
        var configuration = GivenConfiguration(route1, route2, route3);
        configuration.Aggregates[0].RouteKeysConfig =
        [
            new("UserDetails", "$[*].writerId", "userId"),
            new("PostDetails", "$[*].postId", "postId"),
        ];
        var userDetailsResponseContent = @"{""id"":1,""firstName"":""abolfazl"",""lastName"":""rajabpour""}";
        var postDetailsResponseContent = @"{""id"":1,""title"":""post1""}";
        var commentsResponseContent = @"[{""id"":1,""writerId"":1,""postId"":2,""text"":""text1""},{""id"":2,""writerId"":1,""postId"":2,""text"":""text2""}]";

        var expected = $"{{\"Comments\":{commentsResponseContent},\"UserDetails\":{userDetailsResponseContent},\"PostDetails\":{postDetailsResponseContent}}}";

        GivenThereIsAServiceRunningOnPath(ports[0], "/", commentsResponseContent);
        GivenThereIsAServiceRunningOnPath(ports[1], "/users", userDetailsResponseContent);
        GivenThereIsAServiceRunningOnPath(ports[2], "/posts", postDetailsResponseContent);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(expected);
    }

    [Fact]
    [Trait("Feat", "298")] // https://github.com/ThreeMammals/Ocelot/issues/298
    [Trait("PR", "310")] // https://github.com/ThreeMammals/Ocelot/pull/310
    [Trait("Release", "5.5.2")] // https://github.com/ThreeMammals/Ocelot/releases/tag/5.5.2
    public async Task Should_return_response_200_with_simple_url_user_defined_aggregate()
    {
        var ports = GivenPorts(2);
        var route1 = GivenAggRoute(ports[0], "Laura", "/laura");
        var route2 = GivenAggRoute(ports[1], "Tom", "/tom");
        var configuration = GivenConfiguration(route1, route2);
        configuration.Aggregates[0].Aggregator = nameof(FakeDefinedAggregator);

        GivenThereIsAServiceRunningOn(ports[0], "{Hello from Laura}");
        GivenThereIsAServiceRunningOn(ports[1], "{Hello from Tom}");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunningWithSpecificAggregatorsRegisteredInDi<FakeDefinedAggregator, FakeDep>();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync("Bye from Laura, Bye from Tom");
        ThenTheDownstreamUrlPathShouldBe("/", "/");
    }

    [Fact]
    [Trait("Feat", "79")] // https://github.com/ThreeMammals/Ocelot/issues/79
    [Trait("PR", "248")] // https://github.com/ThreeMammals/Ocelot/pull/248
    [Trait("Release", "3.1.7")] // https://github.com/ThreeMammals/Ocelot/releases/tag/3.1.7
    public async Task Should_return_response_200_with_simple_url()
    {
        var ports = GivenPorts(2);
        var route1 = GivenAggRoute(ports[0], "Laura", "/laura");
        var route2 = GivenAggRoute(ports[1], "Tom", "/tom");
        var configuration = GivenConfiguration(route1, route2);

        GivenThereIsAServiceRunningOn(ports[0], "{Hello from Laura}");
        GivenThereIsAServiceRunningOn(ports[1], "{Hello from Tom}");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync("{\"Laura\":{Hello from Laura},\"Tom\":{Hello from Tom}}");
        ThenTheDownstreamUrlPathShouldBe("/", "/");
    }

    [Fact]
    [Trait("Feat", "79")] // https://github.com/ThreeMammals/Ocelot/issues/79
    [Trait("PR", "248")] // https://github.com/ThreeMammals/Ocelot/pull/248
    [Trait("Release", "3.1.7")] // https://github.com/ThreeMammals/Ocelot/releases/tag/3.1.7
    public async Task Should_return_response_200_with_simple_url_one_service_404()
    {
        var ports = GivenPorts(2);
        var route1 = GivenAggRoute(ports[0], "Laura", "/laura");
        var route2 = GivenAggRoute(ports[1], "Tom", "/tom");
        var configuration = GivenConfiguration(route1, route2);

        GivenThereIsAServiceRunningOn(ports[0], HttpStatusCode.NotFound, "{}"); // TODO valid empty JSON, but anyway it was added to resulting data ignoring the status.
        GivenThereIsAServiceRunningOn(ports[1], "{Hello from Tom}");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync("{\"Laura\":{},\"Tom\":{Hello from Tom}}");
        ThenTheDownstreamUrlPathShouldBe("/", "/");
    }

    [Fact]
    [Trait("Feat", "79")] // https://github.com/ThreeMammals/Ocelot/issues/79
    [Trait("PR", "248")] // https://github.com/ThreeMammals/Ocelot/pull/248
    [Trait("Release", "3.1.7")] // https://github.com/ThreeMammals/Ocelot/releases/tag/3.1.7
    public async Task Should_return_response_200_with_simple_url_both_service_404()
    {
        var ports = GivenPorts(2);
        var route1 = GivenAggRoute(ports[0], "Laura", "/laura");
        var route2 = GivenAggRoute(ports[1], "Tom", "/tom");
        var configuration = GivenConfiguration(route1, route2);

        GivenThereIsAServiceRunningOn(ports[0], HttpStatusCode.NotFound, ""); // TODO Returning invalid JSON which comes to resultin JSON to be invalid.
        GivenThereIsAServiceRunningOn(ports[1], HttpStatusCode.NotFound, "");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync("{\"Laura\":,\"Tom\":}");
        ThenTheDownstreamUrlPathShouldBe("/", "/");
    }

    [Fact]
    [Trait("Feat", "79")] // https://github.com/ThreeMammals/Ocelot/issues/79
    [Trait("PR", "248")] // https://github.com/ThreeMammals/Ocelot/pull/248
    [Trait("Release", "3.1.7")] // https://github.com/ThreeMammals/Ocelot/releases/tag/3.1.7
    public async Task Should_be_thread_safe()
    {
        var ports = GivenPorts(2);
        var route1 = GivenAggRoute(ports[0], "Laura", "/laura");
        var route2 = GivenAggRoute(ports[1], "Tom", "/tom");
        var configuration = GivenConfiguration(route1, route2);

        GivenThereIsAServiceRunningOn(ports[0], "{Hello from Laura}");
        GivenThereIsAServiceRunningOn(ports[1], "{Hello from Tom}");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIMakeLotsOfDifferentRequestsToTheApiGateway();
        ThenTheDownstreamUrlPathShouldBe("/", "/");
    }

    private async Task WhenIMakeLotsOfDifferentRequestsToTheApiGateway()
    {
        var numberOfRequests = 100;
        var aggregateUrl = "/";
        var aggregateExpected = "{\"Laura\":{Hello from Laura},\"Tom\":{Hello from Tom}}";
        var tomUrl = "/tom";
        var tomExpected = "{Hello from Tom}";
        var lauraUrl = "/laura";
        var lauraExpected = "{Hello from Laura}";

        var aggregateTasks = new Task[numberOfRequests];
        for (var i = 0; i < numberOfRequests; i++)
        {
            aggregateTasks[i] = Fire(aggregateUrl, aggregateExpected, random);
        }

        var tomTasks = new Task[numberOfRequests];
        for (var i = 0; i < numberOfRequests; i++)
        {
            tomTasks[i] = Fire(tomUrl, tomExpected, random);
        }

        var lauraTasks = new Task[numberOfRequests];
        for (var i = 0; i < numberOfRequests; i++)
        {
            lauraTasks[i] = Fire(lauraUrl, lauraExpected, random);
        }

        Task.WaitAll(lauraTasks);
        Task.WaitAll(tomTasks);
        Task.WaitAll(aggregateTasks);
    }

    private async Task Fire(string url, string expectedBody, Random random)
    {
        var request = new HttpRequestMessage(new HttpMethod("GET"), url);
        await Task.Delay(random.Next(0, 2));
        var response = await ocelotClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldBe(expectedBody);
    }

    [Fact]
    [Trait("Bug", "1396")] // https://github.com/ThreeMammals/Ocelot/issues/1396
    [Trait("PR", "1462")] // https://github.com/ThreeMammals/Ocelot/pull/1462
    [Trait("Release", "23.1.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/23.1.0
    public async Task Should_return_response_200_with_user_forwarding()
    {
        var ports = GivenPorts(3);
        var route1 = GivenAggRoute(ports[0], "Laura", "/laura");
        var route2 = GivenAggRoute(ports[1], "Tom", "/tom");
        var configuration = GivenConfiguration(route1, route2);
        configuration.GlobalConfiguration.AuthenticationOptions = new(JwtBearerDefaults.AuthenticationScheme);

        void WithJwtBearerAndAuthorizationPolicy(IServiceCollection services)
        {
            WithJwtBearerAuthentication(services); // with AddOcelot()
            services.AddMvcCore(mvc =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .RequireClaim(ScopesAuthorizer.Scope, OcelotScopes.Api)
                    .Build();
                mvc.Filters.Add(new AuthorizeFilter(policy));
            });
        }
        var count = 0;
        var actualContexts = new HttpContext[2];
        var pipeline = new OcelotPipelineConfiguration
        {
            PreErrorResponderMiddleware = async (context, next) =>
            {
                var auth = await context.AuthenticateAsync();
                context.User = auth.Succeeded && auth.Principal?.Identity.IsAuthenticated == true
                    ? auth.Principal : null;
                await next.Invoke();
            },
            AuthorizationMiddleware = (context, next) =>
            {
                actualContexts[count++] = context;
                return next.Invoke();
            },
        };
        GivenThereIsAServiceRunningOn(ports[0], "{Hello from Laura}");
        GivenThereIsAServiceRunningOn(ports[1], "{Hello from Tom}");
        GivenThereIsAConfiguration(configuration);
        int ocelot = await GivenOcelotIsRunningAsync(WithBasicConfiguration, WithJwtBearerAndAuthorizationPolicy,
                    configureApp: async a => await a.UseOcelot(pipeline));
        var identityServerUrl = await GivenThereIsExternalJwtSigningService(NoScopes, CancelMe); // 'api' scope is already inside
        var token = await GivenIHaveAToken();
        GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBeOk();
        await ThenTheResponseBodyShouldBeAsync("{\"Laura\":{Hello from Laura},\"Tom\":{Hello from Tom}}");
        ThenTheDownstreamUrlPathShouldBe("/", "/");
        for (var i = 0; i < actualContexts.Length; i++)
        {
            var ctx = actualContexts[i].ShouldNotBeNull();
            ctx.Items.DownstreamRoute().Key.ShouldBe(configuration.Routes[i].Key);
            var user = ctx.User.ShouldNotBeNull();
            user.Identity.IsAuthenticated.ShouldBeTrue();
            var claims = user.Claims.ToList();
            claims.Count.ShouldBeGreaterThan(1);
            claims.FirstOrDefault(c => c is { Type: ScopesAuthorizer.Scope, Value: OcelotScopes.Api }).ShouldNotBeNull();
        }
    }

    #region PR 2050 // https://github.com/ThreeMammals/Ocelot/pull/2050
    [Fact]
    [Trait("Bug", "2039")] // https://github.com/ThreeMammals/Ocelot/issues/2039
    [Trait("PR", "2050")] // https://github.com/ThreeMammals/Ocelot/pull/2050
    [Trait("Release", "23.3.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/23.3.0
    public async Task Should_return_response_200_with_copied_body_sent_on_multiple_services()
    {
        var ports = GivenPorts(2);
        var route1 = GivenAggRoute(ports[0], "Service1", "/Service1", "/Sub1");
        var route2 = GivenAggRoute(ports[1], "Service2", "/Service2", "/Sub2");
        var configuration = GivenConfiguration(route1, route2);
        var requestBody = @"{""id"":1,""response"":""fromBody-#REPLACESTRING#""}";
        var sub1ResponseContent = @"{""id"":1,""response"":""fromBody-s1""}";
        var sub2ResponseContent = @"{""id"":1,""response"":""fromBody-s2""}";
        var expected = $"{{\"Service1\":{sub1ResponseContent},\"Service2\":{sub2ResponseContent}}}";

        GivenThereIsAServiceRunningOn(ports[0], "/Sub1", ctx => MapFromBody(ctx, body => body.Replace("#REPLACESTRING#", "s1")));
        GivenThereIsAServiceRunningOn(ports[1], "/Sub2", ctx => MapFromBody(ctx, body => body.Replace("#REPLACESTRING#", "s2")));
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/", requestBody);
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(expected);
    }

    [Fact]
    [Trait("Bug", "2039")] // https://github.com/ThreeMammals/Ocelot/issues/2039
    [Trait("PR", "2050")] // https://github.com/ThreeMammals/Ocelot/pull/2050
    [Trait("Release", "23.3.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/23.3.0
    public async Task Should_return_response_200_with_copied_form_sent_on_multiple_services()
    {
        var ports = GivenPorts(2);
        var route1 = GivenAggRoute(ports[0], "Service1", "/Service1", "/Sub1");
        var route2 = GivenAggRoute(ports[1], "Service2", "/Service2", "/Sub2");
        var configuration = GivenConfiguration(route1, route2);
        var formValues = new[]
        {
            new KeyValuePair<string, string>("param1", "value1"),
            new KeyValuePair<string, string>("param2", "from-form-REPLACESTRING"),
        };
        var sub1ResponseContent = "\"[key:param1=value1&param2=from-form-s1]\"";
        var sub2ResponseContent = "\"[key:param1=value1&param2=from-form-s2]\"";
        var expected = $"{{\"Service1\":{sub1ResponseContent},\"Service2\":{sub2ResponseContent}}}";

        GivenThereIsAServiceRunningOn(ports[0], "/Sub1", ctx => MapFromForm(ctx, body => body.Replace("REPLACESTRING", "s1")));
        GivenThereIsAServiceRunningOn(ports[1], "/Sub2", ctx => MapFromForm(ctx, body => body.Replace("REPLACESTRING", "s2")));
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGatewayWithForm("/", "key", formValues);
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(expected);
    }

    private static string FormatFormCollection(IFormCollection reqForm)
        => new StringBuilder()
            .Append('"')
            .AppendJoin(string.Empty, reqForm.Select(x => $"[{x.Key}:{x.Value}]"))
            .Append('"')
            .ToString();
    #endregion PR 2050

    [Fact]
    [Trait("Bug", "2248")] // // https://github.com/ThreeMammals/Ocelot/issues/2248
    [Trait("PR", "2328")] // https://github.com/ThreeMammals/Ocelot/pull/2328
    [Trait("Release", "25.0.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0
    public async Task Should_expand_jsonpath_array_into_multiple_parameterized_calls_for_complex_aggregation()
    {
        const string id = "id"; // placeholder
        var ports = GivenPorts(2);
        var commentsPort = ports[0];
        var usersPort = ports[1];
        var comments = GivenAggRoute(commentsPort, "comments", "/comments", "/comments");
        var user = GivenAggRoute(usersPort, "user", $"/users/{{{id}}}", $"/users/{{{id}}}");
        var configuration = GivenConfiguration(comments, user);
        var aggregate = configuration.Aggregates[0];
        aggregate.UpstreamPathTemplate = "/aggregatecommentuser";
        aggregate.RouteKeysConfig = [
            new(user.Key, "$[*].userId", id)
        ];

        const string CommentsResponseContent = "[{\"id\":1,\"userId\":1},{\"id\":2,\"userId\":2}]";
        static Task MapJson(HttpContext ctx)
        {
            var parts = ctx.Request.Path.Value?.Trim('/').Split('/');
            var ok = parts?.Length == 2 && parts[0] == "users" && int.TryParse(parts[1], out var id);
            ctx.Response.StatusCode = ok ? (int)HttpStatusCode.OK : (int)HttpStatusCode.BadRequest;
            ctx.Response.ContentType = MediaTypeNames.Application.Json;
            return ctx.Response.WriteAsync(ok
                ? $"{{\"id\":{parts![1]},\"name\":\"User-{parts[1]}\"}}"
                : "{\"error\":\"bad id\"}");
        }
        var expected =
            "{\"comments\":[{\"id\":1,\"userId\":1},{\"id\":2,\"userId\":2}],\"user\":[{\"id\":1,\"name\":\"User-1\"},{\"id\":2,\"name\":\"User-2\"}]}";

        GivenThereIsAServiceRunningOn(commentsPort, CommentsResponseContent); // uses MapStatus, and status is OK
        GivenThereIsAServiceRunningOn(usersPort, "/", MapJson);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/aggregatecommentuser");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(expected);
    }

    [Theory]
    [InlineData(nameof(HttpMethods.Delete))]
    [InlineData(nameof(HttpMethods.Head))] // no aggregation -> empty body
    [InlineData(nameof(HttpMethods.Options))]
    [InlineData(nameof(HttpMethods.Patch))]
    [InlineData(nameof(HttpMethods.Post))]
    [InlineData(nameof(HttpMethods.Put))]
    [InlineData(nameof(HttpMethods.Trace))]
    [Trait("Feat", "1741")] // https://github.com/ThreeMammals/Ocelot/discussions/1741
    [Trait("PR", "1389")] // https://github.com/ThreeMammals/Ocelot/pull/1389
    [Trait("Release", "25.1.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.1.0
    public async Task Should_return_response_200_with_any_verb_body_sent_not_following_the_REST(string httpVerb)
    {
        var ports = GivenPorts(2);
        var route1 = GivenAggRoute(ports[0], "Service1", "/Service1", "/Sub1");
        var route2 = GivenAggRoute(ports[1], "Service2", "/Service2", "/Sub2");
        var configuration = GivenConfiguration(route1, route2);
        configuration.Aggregates[0].UpstreamHttpMethod = [httpVerb]; // !!! Yahooo!
        var requestBody = "{\"id\":1,\"response\":\"fromBody-REPLACE_ME\"}";
        var downstreamBody1 = "{\"id\":1,\"response\":\"fromBody-s1\"}";
        var downstreamBody2 = "{\"id\":1,\"response\":\"fromBody-s2\"}";
        var expected = $"{{\"Service1\":{downstreamBody1},\"Service2\":{downstreamBody2}}}";

        GivenThereIsAServiceRunningOn(ports[0], "/Sub1", ctx => MapFromBody(ctx, body => body.Replace("REPLACE_ME", "s1")));
        GivenThereIsAServiceRunningOn(ports[1], "/Sub2", ctx => MapFromBody(ctx, body => body.Replace("REPLACE_ME", "s2")));
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        HttpContent content = new StringContent(requestBody, Encoding.UTF8, MediaTypeNames.Application.Json);

        // Let's ignore REST guidelines :LoL
        await WhenIWannaIgnoreAnyRestGuidelinesForCrazyAggregation("/", httpVerb, content);

        ThenTheStatusCodeShouldBeOk();
        if (httpVerb.Equals(HttpMethods.Head, StringComparison.OrdinalIgnoreCase))
            await ThenTheResponseBodyShouldBeEmpty();
        else
            await ThenTheResponseBodyShouldBeAsync(expected);
    }

    [Theory]
    [InlineData(new string[] { }, new string[] { },
        nameof(HttpMethods.Get), HttpStatusCode.OK)] // default GET
    [InlineData(new string[] { }, new string[] { },
        nameof(HttpMethods.Post), HttpStatusCode.NotFound)] // POST not allowed by default
    [InlineData(new string[] { }, new[] { nameof(HttpMethods.Post) },
        nameof(HttpMethods.Post), HttpStatusCode.OK)] // global option only
    [InlineData(new string[] { }, new[] { nameof(HttpMethods.Post) },
        nameof(HttpMethods.Get), HttpStatusCode.NotFound)] // global replaces default GET
    [InlineData(new[] { nameof(HttpMethods.Put) }, new[] { nameof(HttpMethods.Post) },
        nameof(HttpMethods.Put), HttpStatusCode.OK)] // merged: route option
    [InlineData(new[] { nameof(HttpMethods.Put) }, new[] { nameof(HttpMethods.Post) },
        nameof(HttpMethods.Post), HttpStatusCode.OK)] // merged: global option
    [Trait("Feat", "1741")] // https://github.com/ThreeMammals/Ocelot/discussions/1741
    [Trait("PR", "1389")] // https://github.com/ThreeMammals/Ocelot/pull/1389
    [Trait("Release", "25.1.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.1.0
    public async Task Should_return_expected_status_for_aggregate_upstream_http_methods(string[] routeMethods, string[] globalMethods, string requestMethod, HttpStatusCode expectedStatus)
    {
        var ports = GivenPorts(2);
        var route1 = GivenAggRoute(ports[0], "Service1", "/Service1", "/Sub1");
        var route2 = GivenAggRoute(ports[1], "Service2", "/Service2", "/Sub2");
        var configuration = GivenConfiguration(route1, route2);
        configuration.Aggregates[0].UpstreamHttpMethod = [.. routeMethods];
        configuration.GlobalConfiguration.UpstreamHttpMethod = [.. globalMethods];

        GivenThereIsAServiceRunningOn(ports[0], "/Sub1", ctx => MapFromBody(ctx, _ => "{\"id\":1}"));
        GivenThereIsAServiceRunningOn(ports[1], "/Sub2", ctx => MapFromBody(ctx, _ => "{\"id\":2}"));
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();

        // Act
        await WhenIWannaIgnoreAnyRestGuidelinesForCrazyAggregation("/", requestMethod, null);

        // Assert
        ThenTheStatusCodeShouldBe(expectedStatus);
    }

    [Fact]
    [Trait("Feat", "1741")] // https://github.com/ThreeMammals/Ocelot/discussions/1741
    [Trait("PR", "1389")] // https://github.com/ThreeMammals/Ocelot/pull/1389
    [Trait("Release", "25.1.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.1.0
    public async Task Should_use_default_http_method_overridden_in_custom_aggregates_creator()
    {
        var ports = GivenPorts(2);
        var route1 = GivenAggRoute(ports[0], "Service1", "/Service1", "/Sub1");
        var route2 = GivenAggRoute(ports[1], "Service2", "/Service2", "/Sub2");
        var configuration = GivenConfiguration(route1, route2);

        GivenThereIsAServiceRunningOn(ports[0], "/Sub1", ctx => MapFromBody(ctx, _ => "{\"id\":1}"));
        GivenThereIsAServiceRunningOn(ports[1], "/Sub2", ctx => MapFromBody(ctx, _ => "{\"id\":2}"));
        GivenThereIsAConfiguration(configuration);
        static void WithCustomAggregatesCreator(IServiceCollection services) => services
            //.AddOcelot()
            //.Services.AddOcelotAggregation<MyAggregatesCreator>();
            .AddOcelotAggregation<MyAggregatesCreator>()
            .AddOcelot();
        GivenOcelotIsRunning(WithCustomAggregatesCreator);

        // Act & Assert
        await WhenIWannaIgnoreAnyRestGuidelinesForCrazyAggregation("/", nameof(HttpMethods.Post), null);
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK); // overridden default POST is accepted

        await WhenIWannaIgnoreAnyRestGuidelinesForCrazyAggregation("/", nameof(HttpMethods.Get), null);
        ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound); // standard default GET is replaced
    }

    private async Task WhenIWannaIgnoreAnyRestGuidelinesForCrazyAggregation(string url, string httpMethod, HttpContent content)
    {
        HttpMethod method = new(httpMethod);
        var request = new HttpRequestMessage(method, url) { Content = content };
        response = await ocelotClient.SendAsync(request, CancelMe);
    }

    private static async Task MapFromBody(HttpContext context, Func<string, string> responseFromBody)
    {
        using var reader = new StreamReader(context.Request.Body);
        var requestBody = await reader.ReadToEndAsync(context.RequestAborted);
        var body = responseFromBody.Invoke(requestBody);
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = MediaTypeNames.Application.Json;
        await context.Response.WriteAsync(body, context.RequestAborted);
    }

    private static Task MapFromForm(HttpContext context, Func<string, string> responseFromForm)
    {
        var requestBody = FormatFormCollection(context.Request.Form);
        var body = responseFromForm.Invoke(requestBody);
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = MediaTypeNames.Application.Json;
        return context.Response.WriteAsync(body, context.RequestAborted);
    }

    private int[] _ports = [];
    private int[] GivenPorts(int count)
    {
        _downstreamPaths = new string[count];
        return _ports = PortFinder.GetPorts(count);
    }
    protected override Task MapStatus(HttpContext context)
    {
        int port = context.Request.Host.Port.Value;
        int index = Array.IndexOf(_ports, port);
        _downstreamPaths[index] = !string.IsNullOrEmpty(context.Request.PathBase.Value)
            ? context.Request.PathBase.Value
            : context.Request.Path.Value;
        context.Response.ContentType = MediaTypeNames.Application.Json;
        return base.MapStatus(context);
    }

    private void GivenOcelotIsRunningWithSpecificAggregatorsRegisteredInDi<TAggregator, TDependency>()
        where TAggregator : class, IDefinedAggregator
        where TDependency : class
    {
        static void WithSpecificAggregators(IServiceCollection services) => services
            .AddSingleton<TDependency>()
            .AddOcelot()
            .AddSingletonDefinedAggregator<TAggregator>();
        GivenOcelotIsRunning(WithSpecificAggregators);
    }

    private string[] _downstreamPaths = [];
    private void ThenTheDownstreamUrlPathShouldBe(string expectedDownstreamPathOne, string expectedDownstreamPath)
    {
        _downstreamPaths[0].ShouldBe(expectedDownstreamPathOne);
        _downstreamPaths[1].ShouldBe(expectedDownstreamPath);
    }

    private FileRoute GivenAggRoute(int port, string key, string upstream = null, string downstream = null)
    {
        var r = GivenRoute(port, upstream, downstream);
        r.Key = key;
        return r;
    }

    public override FileConfiguration GivenConfiguration(params FileRoute[] routes)
    {
        var conf = base.GivenConfiguration(routes);
        conf.Aggregates.Add(new()
            {
                UpstreamPathTemplate = "/",
                UpstreamHost = "localhost",
                RouteKeys = [.. routes.Select(r => r.Key)], // [ "Laura", "Tom" ],
            });
        return conf;
    }
}

public class FakeDep { }

public class MyAggregatesCreator : AggregatesCreator
{
    public MyAggregatesCreator(IUpstreamTemplatePatternCreator creator, IUpstreamHeaderTemplatePatternCreator headerCreator)
        : base(creator, headerCreator)
    {
        DefaultHttpMethod = HttpMethod.Post;
    }

    /*protected override HttpMethod DefaultHttpMethod
    {
        get => HttpMethod.Post; // read from anywhere
    }*/

    public void ResetDefaultHttpMethod() => DefaultHttpMethod = HttpMethod.Get;
}

public class FakeDefinedAggregator(FakeDep dep) : IDefinedAggregator
{
    private readonly FakeDep _dep = dep;

    public async Task<DownstreamResponse> Aggregate(List<HttpContext> responses)
    {
        var a = _dep;
        var one = await responses[0].Items.DownstreamResponse().Content.ReadAsStringAsync();
        var two = await responses[1].Items.DownstreamResponse().Content.ReadAsStringAsync();

        var merge = $"{one}, {two}";
        merge = merge.Replace("Hello", "Bye").Replace("{", "").Replace("}", "");
        var headers = responses.SelectMany(x => x.Items.DownstreamResponse().Headers).ToList();
        return new DownstreamResponse(new StringContent(merge), HttpStatusCode.OK, headers, "some reason");
    }
}
