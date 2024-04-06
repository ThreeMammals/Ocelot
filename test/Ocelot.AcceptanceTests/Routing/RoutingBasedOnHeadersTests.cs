using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;

namespace Ocelot.AcceptanceTests.Routing;

public sealed class RoutingBasedOnHeadersTests : Steps, IDisposable
{
    private string _downstreamPath;
    private readonly ServiceHandler _serviceHandler;

    public RoutingBasedOnHeadersTests()
    {
        _serviceHandler = new();
    }

    public override void Dispose()
    {
        _serviceHandler.Dispose();
        base.Dispose();
    }

    [Fact]
    public void Should_match_one_header_value()
    {
        var port = PortFinder.GetRandomPort();
        var headerName = "country_code";
        var headerValue = "PL";

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = new List<string> { "Get" },
                    UpstreamHeaderTemplates = new Dictionary<string, string>()
                    {
                        [headerName] = headerValue,
                    },
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(port))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader(headerName, headerValue))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(Hello()))
            .BDDfy();
    }

    [Fact]
    public void Should_match_one_header_value_when_more_headers()
    {
        var port = PortFinder.GetRandomPort();
        var headerName = "country_code";
        var headerValue = "PL";

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = new List<string> { "Get" },
                    UpstreamHeaderTemplates = new Dictionary<string, string>()
                    {
                        [headerName] = headerValue,
                    },
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(port))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader("other", "otherValue"))
            .And(x => GivenIAddAHeader(headerName, headerValue))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(Hello()))
            .BDDfy();
    }

    [Fact]
    public void Should_match_two_header_values_when_more_headers()
    {
        var port = PortFinder.GetRandomPort();
        var headerName1 = "country_code";
        var headerValue1 = "PL";
        var headerName2 = "region";
        var headerValue2 = "MAZ";

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = new List<string> { "Get" },
                    UpstreamHeaderTemplates = new Dictionary<string, string>()
                    {
                        [headerName1] = headerValue1,
                        [headerName2] = headerValue2,
                    },
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(port))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader(headerName1, headerValue1))
            .And(x => GivenIAddAHeader("other", "otherValue"))
            .And(x => GivenIAddAHeader(headerName2, headerValue2))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(Hello()))
            .BDDfy();
    }

    [Fact]
    public void Should_not_match_one_header_value()
    {
        var port = PortFinder.GetRandomPort();
        var headerName = "country_code";
        var headerValue = "PL";
        var anotherHeaderValue = "UK";

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = new List<string> { "Get" },
                    UpstreamHeaderTemplates = new Dictionary<string, string>()
                    {
                        [headerName] = headerValue,
                    },
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(port))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader(headerName, anotherHeaderValue))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
            .BDDfy();
    }

    [Fact]
    public void Should_not_match_one_header_value_when_no_headers()
    {
        var port = PortFinder.GetRandomPort();
        var headerName = "country_code";
        var headerValue = "PL";

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = new List<string> { "Get" },
                    UpstreamHeaderTemplates = new Dictionary<string, string>()
                    {
                        [headerName] = headerValue,
                    },
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(port))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
            .BDDfy();
    }

    [Fact]
    public void Should_not_match_two_header_values_when_one_different()
    {
        var port = PortFinder.GetRandomPort();
        var headerName1 = "country_code";
        var headerValue1 = "PL";
        var headerName2 = "region";
        var headerValue2 = "MAZ";

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = new List<string> { "Get" },
                    UpstreamHeaderTemplates = new Dictionary<string, string>()
                    {
                        [headerName1] = headerValue1,
                        [headerName2] = headerValue2,
                    },
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(port))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader(headerName1, headerValue1))
            .And(x => GivenIAddAHeader("other", "otherValue"))
            .And(x => GivenIAddAHeader(headerName2, "anothervalue"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
            .BDDfy();
    }

    [Fact]
    public void Should_not_match_two_header_values_when_one_not_existing()
    {
        var port = PortFinder.GetRandomPort();
        var headerName1 = "country_code";
        var headerValue1 = "PL";
        var headerName2 = "region";
        var headerValue2 = "MAZ";

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = new List<string> { "Get" },
                    UpstreamHeaderTemplates = new Dictionary<string, string>()
                    {
                        [headerName1] = headerValue1,
                        [headerName2] = headerValue2,
                    },
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(port))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader(headerName1, headerValue1))
            .And(x => GivenIAddAHeader("other", "otherValue"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
            .BDDfy();
    }

    [Fact]
    public void Should_not_match_one_header_value_when_header_duplicated()
    {
        var port = PortFinder.GetRandomPort();
        var headerName = "country_code";
        var headerValue = "PL";

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = new List<string> { "Get" },
                    UpstreamHeaderTemplates = new Dictionary<string, string>()
                    {
                        [headerName] = headerValue,
                    },
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(port))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader(headerName, headerValue))
            .And(x => GivenIAddAHeader(headerName, "othervalue"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
            .BDDfy();
    }

    [Fact]
    public void Should_aggregated_route_match_header_value()
    {
        var port1 = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var headerName = "country_code";
        var headerValue = "PL";

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/a",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port1,
                        },
                    },
                    UpstreamPathTemplate = "/a",
                    UpstreamHttpMethod = new List<string> { "Get" },
                    Key = "Laura",
                },
                new()
                {
                    DownstreamPathTemplate = "/b",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port2,
                        },
                    },
                    UpstreamPathTemplate = "/b",
                    UpstreamHttpMethod = new List<string> { "Get" },
                    Key = "Tom",
                },
            },
            Aggregates = new List<FileAggregateRoute>()
            {
                new()
                {
                    UpstreamPathTemplate = "/",
                    UpstreamHost = "localhost",
                    RouteKeys = new List<string>
                    {
                        "Laura",
                        "Tom",
                    },
                    UpstreamHeaderTemplates = new Dictionary<string, string>()
                    {
                        [headerName] = headerValue,
                    },
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(port1, "/a", HttpStatusCode.OK, Hello("Laura")))
            .And(x => GivenThereIsAServiceRunningOn(port2, "/b", HttpStatusCode.OK, Hello("Tom")))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader(headerName, headerValue))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .BDDfy();
    }

    [Fact]
    public void Should_aggregated_route_not_match_header_value()
    {
        var port1 = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var headerName = "country_code";
        var headerValue = "PL";

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/a",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port1,
                        },
                    },
                    UpstreamPathTemplate = "/a",
                    UpstreamHttpMethod = new List<string> { "Get" },
                    Key = "Laura",
                },
                new()
                {
                    DownstreamPathTemplate = "/b",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port2,
                        },
                    },
                    UpstreamPathTemplate = "/b",
                    UpstreamHttpMethod = new List<string> { "Get" },
                    Key = "Tom",
                },
            },
            Aggregates = new List<FileAggregateRoute>()
            {
                new()
                {
                    UpstreamPathTemplate = "/",
                    UpstreamHost = "localhost",
                    RouteKeys = new List<string>
                    {
                        "Laura",
                        "Tom",
                    },
                    UpstreamHeaderTemplates = new Dictionary<string, string>()
                    {
                        [headerName] = headerValue,
                    },
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(port1, "/a", HttpStatusCode.OK, Hello("Laura")))
            .And(x => x.GivenThereIsAServiceRunningOn(port2, "/b", HttpStatusCode.OK, Hello("Tom")))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
            .BDDfy();
    }

    [Fact]
    public void Should_match_header_placeholder()
    {
        var port = PortFinder.GetRandomPort();
        var headerName = "Region";

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/api.internal-{code}/products",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    UpstreamPathTemplate = "/products",
                    UpstreamHttpMethod = new List<string> { "Get" },
                    UpstreamHeaderTemplates = new Dictionary<string, string>()
                    {
                        [headerName] = "{header:code}",
                    },
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/api.internal-uk/products", HttpStatusCode.OK, Hello("UK")))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader(headerName, "uk"))
            .When(x => WhenIGetUrlOnTheApiGateway("/products"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(Hello("UK")))
            .BDDfy();
    }

    [Fact]
    public void Should_match_header_placeholder_not_in_downstream_path()
    {
        var port = PortFinder.GetRandomPort();
        var headerName = "ProductName";

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/products-info",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    UpstreamPathTemplate = "/products",
                    UpstreamHttpMethod = new List<string> { "Get" },
                    UpstreamHeaderTemplates = new Dictionary<string, string>()
                    {
                        [headerName] = "product-{header:everything}",
                    },
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/products-info", HttpStatusCode.OK, Hello("products")))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader(headerName, "product-Camera"))
            .When(x => WhenIGetUrlOnTheApiGateway("/products"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(Hello("products")))
            .BDDfy();
    }

    [Fact]
    public void Should_distinguish_route_for_different_roles()
    {
        var port = PortFinder.GetRandomPort();
        var headerName = "Origin";

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/products-admin",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    UpstreamPathTemplate = "/products",
                    UpstreamHttpMethod = new List<string> { "Get" },
                    UpstreamHeaderTemplates = new Dictionary<string, string>()
                    {
                        [headerName] = "admin.xxx.com",
                    },
                },
                new()
                {
                    DownstreamPathTemplate = "/products",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    UpstreamPathTemplate = "/products",
                    UpstreamHttpMethod = new List<string> { "Get" },
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/products-admin", HttpStatusCode.OK, Hello("products admin")))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader(headerName, "admin.xxx.com"))
            .When(x => WhenIGetUrlOnTheApiGateway("/products"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(Hello("products admin")))
            .BDDfy();
    }

    [Fact]
    public void Should_match_header_and_url_placeholders()
    {
        var port = PortFinder.GetRandomPort();
        var headerName = "country_code";

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/{country_code}/{version}/{aa}",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    UpstreamPathTemplate = "/{aa}",
                    UpstreamHttpMethod = new List<string> { "Get" },
                    UpstreamHeaderTemplates = new Dictionary<string, string>()
                    {
                        [headerName] = "start_{header:country_code}_version_{header:version}_end",
                    },
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/pl/v1/bb", HttpStatusCode.OK, Hello()))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader(headerName, "start_pl_version_v1_end"))
            .When(x => WhenIGetUrlOnTheApiGateway("/bb"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(Hello()))
            .BDDfy();
    }

    [Fact]
    public void Should_match_header_with_braces()
    {
        var port = PortFinder.GetRandomPort();
        var headerName = "country_code";

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/aa",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = new List<string> { "Get" },
                    UpstreamHeaderTemplates = new Dictionary<string, string>()
                    {
                        [headerName] = "my_{header}",
                    },
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/aa", HttpStatusCode.OK, Hello()))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader(headerName, "my_{header}"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(Hello()))
            .BDDfy();
    }

    [Fact]
    public void Should_match_two_headers_with_the_same_name()
    {
        var port = PortFinder.GetRandomPort();
        var headerName = "country_code";
        var headerValue1 = "PL";
        var headerValue2 = "UK";

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = new List<string> { "Get" },
                    UpstreamHeaderTemplates = new Dictionary<string, string>()
                    {
                        [headerName] = headerValue1 + ";{header:whatever}",
                    },
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(port))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader(headerName, headerValue1))
            .And(x => GivenIAddAHeader(headerName, headerValue2))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(Hello()))
            .BDDfy();
    }

    //private static string HelloFromJolanta = "Hello from Jolanta";

    private static string Hello() => Hello("Jolanta");
    private static string Hello(string who) => $"Hello from {who}";

    private void GivenThereIsAServiceRunningOn(int port)
        => GivenThereIsAServiceRunningOn(port, "/", HttpStatusCode.OK, Hello());

    private void GivenThereIsAServiceRunningOn(int port, string basePath, HttpStatusCode statusCode, string responseBody)
    {
        basePath ??= "/";
        responseBody ??= Hello();
        var baseUrl = DownstreamUrl(port);
        _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, basePath, async context =>
        {
            _downstreamPath = !string.IsNullOrEmpty(context.Request.PathBase.Value) ? context.Request.PathBase.Value : context.Request.Path.Value;

            if (_downstreamPath != basePath)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                await context.Response.WriteAsync($"{nameof(_downstreamPath)} is not equal to {nameof(basePath)}");
            }
            else
            {
                context.Response.StatusCode = (int)statusCode;
                await context.Response.WriteAsync(responseBody);
            }
        });
    }

    private void ThenTheDownstreamUrlPathShouldBe(string expected) => _downstreamPath.ShouldBe(expected);
}
