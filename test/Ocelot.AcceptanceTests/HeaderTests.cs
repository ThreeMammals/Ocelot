using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;

namespace Ocelot.AcceptanceTests;

public sealed class HeaderTests : Steps
{
    private int _count;
    private readonly ServiceHandler _serviceHandler;

    public HeaderTests()
    {
        _serviceHandler = new ServiceHandler();
    }

    [Fact]
    public void Should_transform_upstream_header()
    {
        var port = PortFinder.GetRandomPort();
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
                        UpstreamHeaderTransform = new Dictionary<string,string>
                        {
                            {"Laz", "D, GP"},
                        },
                    },
                },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), "/", 200, "Laz"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader("Laz", "D"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("GP"))
            .BDDfy();
    }

    [Fact]
    public void Should_transform_downstream_header()
    {
        var port = PortFinder.GetRandomPort();
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
                        DownstreamHeaderTransform = new Dictionary<string,string>
                        {
                            {"Location", "http://www.bbc.co.uk/, http://ocelot.com/"},
                        },
                    },
                },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), "/", 200, "Location", "http://www.bbc.co.uk/"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseHeaderIs("Location", "http://ocelot.com/"))
            .BDDfy();
    }

    [Fact]
    public void Should_fix_issue_190()
    {
        var port = PortFinder.GetRandomPort();
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
                        DownstreamHeaderTransform = new Dictionary<string,string>
                        {
                            {"Location", $"http://localhost:{port}, {{BaseUrl}}"},
                        },
                        HttpHandlerOptions = new FileHttpHandlerOptions
                        {
                            AllowAutoRedirect = false,
                        },
                    },
                },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), "/", 302, "Location", $"http://localhost:{port}/pay/Receive"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Redirect))
            .And(x => ThenTheResponseHeaderIs("Location", "http://localhost:5000/pay/Receive"))
            .BDDfy();
    }

    [Fact]
    public void Should_fix_issue_205()
    {
        var port = PortFinder.GetRandomPort();
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
                        DownstreamHeaderTransform = new Dictionary<string,string>
                        {
                            {"Location", "{DownstreamBaseUrl}, {BaseUrl}"},
                        },
                        HttpHandlerOptions = new FileHttpHandlerOptions
                        {
                            AllowAutoRedirect = false,
                        },
                    },
                },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), "/", 302, "Location", $"http://localhost:{port}/pay/Receive"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Redirect))
            .And(x => ThenTheResponseHeaderIs("Location", "http://localhost:5000/pay/Receive"))
            .BDDfy();
    }

    [Fact]
    public void Should_fix_issue_417()
    {
        var port = PortFinder.GetRandomPort();
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
                        DownstreamHeaderTransform = new Dictionary<string,string>
                        {
                            {"Location", "{DownstreamBaseUrl}, {BaseUrl}"},
                        },
                        HttpHandlerOptions = new FileHttpHandlerOptions
                        {
                            AllowAutoRedirect = false,
                        },
                    },
                },
            GlobalConfiguration = new FileGlobalConfiguration
            {
                BaseUrl = "http://anotherapp.azurewebsites.net",
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), "/", 302, "Location", $"{DownstreamUrl(port)}/pay/Receive"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Redirect))
            .And(x => ThenTheResponseHeaderIs("Location", "http://anotherapp.azurewebsites.net/pay/Receive"))
            .BDDfy();
    }

    [Fact]
    public void Request_should_reuse_cookies_with_cookie_container()
    {
        var port = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/sso/{everything}",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    UpstreamPathTemplate = "/sso/{everything}",
                    UpstreamHttpMethod = new List<string> { "Get", "Post", "Options" },
                    HttpHandlerOptions = new FileHttpHandlerOptions
                    {
                        UseCookieContainer = true,
                    },
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), "/sso/test", 200))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => WhenIGetUrlOnTheApiGateway("/sso/test"))
            .And(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseHeaderIs("Set-Cookie", "test=0; path=/"))
            .And(x => GivenIAddCookieToMyRequest("test=1; path=/"))
            .When(x => WhenIGetUrlOnTheApiGateway("/sso/test"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .BDDfy();
    }

    [Fact]
    public void Request_should_have_own_cookies_no_cookie_container()
    {
        var port = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/sso/{everything}",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    UpstreamPathTemplate = "/sso/{everything}",
                    UpstreamHttpMethod = new List<string> { "Get", "Post", "Options" },
                    HttpHandlerOptions = new FileHttpHandlerOptions
                    {
                        UseCookieContainer = false,
                    },
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), "/sso/test", 200))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => WhenIGetUrlOnTheApiGateway("/sso/test"))
            .And(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseHeaderIs("Set-Cookie", "test=0; path=/"))
            .And(x => GivenIAddCookieToMyRequest("test=1; path=/"))
            .When(x => WhenIGetUrlOnTheApiGateway("/sso/test"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .BDDfy();
    }

    [Fact]
    public void Issue_474_should_not_put_spaces_in_header()
    {
        var port = PortFinder.GetRandomPort();
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
                    },
                },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), "/", 200, "Accept"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader("Accept", "text/html,application/xhtml+xml,application/xml;"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("text/html,application/xhtml+xml,application/xml;"))
            .BDDfy();
    }

    [Fact]
    public void Issue_474_should_put_spaces_in_header()
    {
        var port = PortFinder.GetRandomPort();
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
                    },
                },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), "/", 200, "Accept"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader("Accept", "text/html"))
            .And(x => GivenIAddAHeader("Accept", "application/xhtml+xml"))
            .And(x => GivenIAddAHeader("Accept", "application/xml"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("text/html, application/xhtml+xml, application/xml"))
            .BDDfy();
    }

    private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, int statusCode)
    {
        _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, basePath, context =>
        {
            if (_count == 0)
            {
                context.Response.Cookies.Append("test", "0");
                _count++;
                context.Response.StatusCode = statusCode;
                return Task.CompletedTask;
            }

            if (context.Request.Cookies.TryGetValue("test", out var cookieValue))
            {
                if (cookieValue == "0")
                {
                    context.Response.StatusCode = statusCode;
                    return Task.CompletedTask;
                }
            }

            if (context.Request.Headers.TryGetValue("Set-Cookie", out var headerValue))
            {
                if (headerValue == "test=1; path=/")
                {
                    context.Response.StatusCode = statusCode;
                    return Task.CompletedTask;
                }
            }

            context.Response.StatusCode = 500;
            return Task.CompletedTask;
        });
    }

    private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, int statusCode, string headerKey)
    {
        _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, basePath, async context =>
        {
            if (context.Request.Headers.TryGetValue(headerKey, out var values))
            {
                var result = values.First();
                context.Response.StatusCode = statusCode;
                await context.Response.WriteAsync(result);
            }
        });
    }

    private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, int statusCode, string headerKey, string headerValue)
    {
        _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, basePath, context =>
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.Append(headerKey, headerValue);
                context.Response.StatusCode = statusCode;
                return Task.CompletedTask;
            });

            return Task.CompletedTask;
        });
    }

    public override void Dispose()
    {
        _serviceHandler.Dispose();
        base.Dispose();
    }
}
