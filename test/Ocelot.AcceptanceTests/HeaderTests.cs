using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;

namespace Ocelot.AcceptanceTests;

public sealed class HeaderTests : Steps
{
    private int _count;

    public HeaderTests()
    {
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

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/", HttpStatusCode.OK, "Laz"))
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

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/", HttpStatusCode.OK, "Location", "http://www.bbc.co.uk/"))
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

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/", HttpStatusCode.Found, "Location", $"{DownstreamUrl(port)}/pay/Receive"))
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

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/", HttpStatusCode.Found, "Location", $"{DownstreamUrl(port)}/pay/Receive"))
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

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/", HttpStatusCode.Found, "Location", $"{DownstreamUrl(port)}/pay/Receive"))
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

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/sso/test", HttpStatusCode.OK))
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

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/sso/test", HttpStatusCode.OK))
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

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/", HttpStatusCode.OK, "Accept"))
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

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/", HttpStatusCode.OK, "Accept"))
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

    private void GivenThereIsAServiceRunningOn(int port, string basePath, HttpStatusCode statusCode)
    {
        handler.GivenThereIsAServiceRunningOn(port, basePath, context =>
        {
            if (_count == 0)
            {
                context.Response.Cookies.Append("test", "0");
                _count++;
                context.Response.StatusCode = (int)statusCode;
                return Task.CompletedTask;
            }

            if (context.Request.Cookies.TryGetValue("test", out var cookieValue))
            {
                if (cookieValue == "0")
                {
                    context.Response.StatusCode = (int)statusCode;
                    return Task.CompletedTask;
                }
            }

            if (context.Request.Headers.TryGetValue("Set-Cookie", out var headerValue))
            {
                if (headerValue == "test=1; path=/")
                {
                    context.Response.StatusCode = (int)statusCode;
                    return Task.CompletedTask;
                }
            }

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            return Task.CompletedTask;
        });
    }

    private void GivenThereIsAServiceRunningOn(int port, string basePath, HttpStatusCode statusCode, string headerKey)
    {
        handler.GivenThereIsAServiceRunningOn(port, basePath, async context =>
        {
            if (context.Request.Headers.TryGetValue(headerKey, out var values))
            {
                var result = values.First();
                context.Response.StatusCode = (int)statusCode;
                await context.Response.WriteAsync(result);
            }
        });
    }

    private void GivenThereIsAServiceRunningOn(int port, string basePath, HttpStatusCode statusCode, string headerKey, string headerValue)
    {
        handler.GivenThereIsAServiceRunningOn(port, basePath, context =>
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.Append(headerKey, headerValue);
                context.Response.StatusCode = (int)statusCode;
                return Task.CompletedTask;
            });

            return Task.CompletedTask;
        });
    }
}
