﻿using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Ocelot.AcceptanceTests.RateLimiting;
using Ocelot.Cache;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using System.Text;

namespace Ocelot.AcceptanceTests.ServiceDiscovery;

public sealed class ConsulConfigurationInConsulTests : RateLimitingSteps, IDisposable
{
    private IWebHost _builder;
    private IWebHost _fakeConsulBuilder;
    private FileConfiguration _config;
    private readonly List<ServiceEntry> _consulServices;

    public ConsulConfigurationInConsulTests()
    {
        _consulServices = new List<ServiceEntry>();
    }

    [Fact]
    public void Should_return_response_200_with_simple_url()
    {
        var consulPort = PortFinder.GetRandomPort();
        var servicePort = PortFinder.GetRandomPort();

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
                                Port = servicePort,
                            },
                        },
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
            GlobalConfiguration = new FileGlobalConfiguration
            {
                ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                {
                    Scheme = "http",
                    Host = "localhost",
                    Port = consulPort,
                },
            },
        };

        var fakeConsulServiceDiscoveryUrl = $"http://localhost:{consulPort}";

        this.Given(x => GivenThereIsAFakeConsulServiceDiscoveryProvider(fakeConsulServiceDiscoveryUrl, string.Empty))
            .And(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{servicePort}", string.Empty, 200, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => x.GivenOcelotIsRunningUsingConsulToStoreConfig())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact]
    public void Should_load_configuration_out_of_consul()
    {
        var consulPort = PortFinder.GetRandomPort();
        var servicePort = PortFinder.GetRandomPort();

        var configuration = new FileConfiguration
        {
            GlobalConfiguration = new FileGlobalConfiguration
            {
                ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                {
                    Scheme = "http",
                    Host = "localhost",
                    Port = consulPort,
                },
            },
        };

        var fakeConsulServiceDiscoveryUrl = $"http://localhost:{consulPort}";

        var consulConfig = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/status",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = servicePort,
                        },
                    },
                    UpstreamPathTemplate = "/cs/status",
                    UpstreamHttpMethod = new List<string> {"Get"},
                },
            },
            GlobalConfiguration = new FileGlobalConfiguration
            {
                ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                {
                    Scheme = "http",
                    Host = "localhost",
                    Port = consulPort,
                },
            },
        };

        this.Given(x => GivenTheConsulConfigurationIs(consulConfig))
            .And(x => GivenThereIsAFakeConsulServiceDiscoveryProvider(fakeConsulServiceDiscoveryUrl, string.Empty))
            .And(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{servicePort}", "/status", 200, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => x.GivenOcelotIsRunningUsingConsulToStoreConfig())
            .When(x => WhenIGetUrlOnTheApiGateway("/cs/status"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact]
    public void Should_load_configuration_out_of_consul_if_it_is_changed()
    {
        var consulPort = PortFinder.GetRandomPort();
        var servicePort = PortFinder.GetRandomPort();

        var configuration = new FileConfiguration
        {
            GlobalConfiguration = new FileGlobalConfiguration
            {
                ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                {
                    Scheme = "http",
                    Host = "localhost",
                    Port = consulPort,
                },
            },
        };

        var fakeConsulServiceDiscoveryUrl = $"http://localhost:{consulPort}";

        var consulConfig = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/status",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = servicePort,
                        },
                    },
                    UpstreamPathTemplate = "/cs/status",
                    UpstreamHttpMethod = new List<string> {"Get"},
                },
            },
            GlobalConfiguration = new FileGlobalConfiguration
            {
                ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                {
                    Scheme = "http",
                    Host = "localhost",
                    Port = consulPort,
                },
            },
        };

        var secondConsulConfig = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/status",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = servicePort,
                        },
                    },
                    UpstreamPathTemplate = "/cs/status/awesome",
                    UpstreamHttpMethod = new List<string> {"Get"},
                },
            },
            GlobalConfiguration = new FileGlobalConfiguration
            {
                ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                {
                    Scheme = "http",
                    Host = "localhost",
                    Port = consulPort,
                },
            },
        };

        this.Given(x => GivenTheConsulConfigurationIs(consulConfig))
            .And(x => GivenThereIsAFakeConsulServiceDiscoveryProvider(fakeConsulServiceDiscoveryUrl, string.Empty))
            .And(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{servicePort}", "/status", 200, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => x.GivenOcelotIsRunningUsingConsulToStoreConfig())
            .And(x => WhenIGetUrlOnTheApiGateway("/cs/status"))
            .And(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .When(x => GivenTheConsulConfigurationIs(secondConsulConfig))
            .Then(x => ThenTheConfigIsUpdatedInOcelot())
            .BDDfy();
    }

    [Fact]
    public void Should_handle_request_to_consul_for_downstream_service_and_make_request_no_re_routes_and_rate_limit()
    {
        var consulPort = PortFinder.GetRandomPort();
        const string serviceName = "web";
        var downstreamServicePort = PortFinder.GetRandomPort();
        var downstreamServiceOneUrl = $"http://localhost:{downstreamServicePort}";
        var fakeConsulServiceDiscoveryUrl = $"http://localhost:{consulPort}";
        var serviceEntryOne = new ServiceEntry
        {
            Service = new AgentService
            {
                Service = serviceName,
                Address = "localhost",
                Port = downstreamServicePort,
                ID = "web_90_0_2_224_8080",
                Tags = new[] { "version-v1" },
            },
        };

        var consulConfig = new FileConfiguration
        {
            DynamicRoutes = new List<FileDynamicRoute>
            {
                new()
                {
                    ServiceName = serviceName,
                    RateLimitRule = new FileRateLimitRule
                    {
                        EnableRateLimiting = true,
                        ClientWhitelist = new List<string>(),
                        Limit = 3,
                        Period = "1s",
                        PeriodTimespan = 1000,
                    },
                },
            },
            GlobalConfiguration = new FileGlobalConfiguration
            {
                ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                {
                    Scheme = "http",
                    Host = "localhost",
                    Port = consulPort,
                },
                RateLimitOptions = new FileRateLimitOptions
                {
                    ClientIdHeader = "ClientId",
                    DisableRateLimitHeaders = false,
                    QuotaExceededMessage = string.Empty,
                    RateLimitCounterPrefix = string.Empty,
                    HttpStatusCode = 428,
                },
                DownstreamScheme = "http",
            },
        };

        var configuration = new FileConfiguration
        {
            GlobalConfiguration = new FileGlobalConfiguration
            {
                ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                {
                    Scheme = "http",
                    Host = "localhost",
                    Port = consulPort,
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(downstreamServiceOneUrl, "/something", 200, "Hello from Laura"))
        .And(x => GivenTheConsulConfigurationIs(consulConfig))
        .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(fakeConsulServiceDiscoveryUrl, serviceName))
        .And(x => x.GivenTheServicesAreRegisteredWithConsul(serviceEntryOne))
        .And(x => GivenThereIsAConfiguration(configuration))
        .And(x => x.GivenOcelotIsRunningUsingConsulToStoreConfig())
        .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit("/web/something", 1))
        .Then(x => ThenTheStatusCodeShouldBe(200))
        .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit("/web/something", 2))
        .Then(x => ThenTheStatusCodeShouldBe(200))
        .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit("/web/something", 1))
        .Then(x => ThenTheStatusCodeShouldBe(428))
        .BDDfy();
    }

    private async Task ThenTheConfigIsUpdatedInOcelot()
    {
        var result = await Wait.WaitFor(20000).UntilAsync(async () =>
        {
            try
            {
                await WhenIGetUrlOnTheApiGateway("/cs/status/awesome");
                ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
                ThenTheResponseBodyShouldBe("Hello from Laura");
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        });
        result.ShouldBeTrue();
    }

    private void GivenTheConsulConfigurationIs(FileConfiguration config)
    {
        _config = config;
    }

    private void GivenTheServicesAreRegisteredWithConsul(params ServiceEntry[] serviceEntries)
    {
        foreach (var serviceEntry in serviceEntries)
        {
            _consulServices.Add(serviceEntry);
        }
    }

    private void GivenOcelotIsRunningUsingConsulToStoreConfig()
    {
        _webHostBuilder = TestHostBuilder.Create()
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
                var env = hostingContext.HostingEnvironment;
                config.AddJsonFile("appsettings.json", true, false)
                    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, false);
                config.AddJsonFile(_ocelotConfigFileName, true, false);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices(s => { s.AddOcelot().AddConsul().AddConfigStoredInConsul(); })
            .Configure(app => app.UseOcelot().GetAwaiter().GetResult()); // Turning as async/await some tests got broken

        _ocelotServer = new TestServer(_webHostBuilder);
        _ocelotClient = _ocelotServer.CreateClient();
        Thread.Sleep(1000);
    }

    private Task GivenThereIsAFakeConsulServiceDiscoveryProvider(string url, string serviceName)
    {
        _fakeConsulBuilder = TestHostBuilder.Create()
                        .UseUrls(url)
                        .UseKestrel()
                        .UseContentRoot(Directory.GetCurrentDirectory())
                        .UseIISIntegration()
                        .UseUrls(url)
                        .Configure(app =>
                        {
                            app.Run(async context =>
                            {
                                if (context.Request.Method.ToLower() == "get" && context.Request.Path.Value == "/v1/kv/InternalConfiguration")
                                {
                                    var json = JsonConvert.SerializeObject(_config);

                                    var bytes = Encoding.UTF8.GetBytes(json);

                                    var base64 = Convert.ToBase64String(bytes);

                                    var kvp = new FakeConsulGetResponse(base64);
                                    json = JsonConvert.SerializeObject(new[] { kvp });
                                    context.Response.Headers.Append("Content-Type", "application/json");
                                    await context.Response.WriteAsync(json);
                                }
                                else if (context.Request.Method.ToLower() == "put" && context.Request.Path.Value == "/v1/kv/InternalConfiguration")
                                {
                                    try
                                    {
                                        var reader = new StreamReader(context.Request.Body);

                                        // Synchronous operations are disallowed. Call ReadAsync or set AllowSynchronousIO to true instead.
                                        // var json = reader.ReadToEnd();                                            
                                        var json = await reader.ReadToEndAsync();

                                        _config = JsonConvert.DeserializeObject<FileConfiguration>(json);

                                        var response = JsonConvert.SerializeObject(true);

                                        await context.Response.WriteAsync(response);
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(e);
                                        throw;
                                    }
                                }
                                else if (context.Request.Path.Value == $"/v1/health/service/{serviceName}")
                                {
                                    var json = JsonConvert.SerializeObject(_consulServices);
                                    context.Response.Headers.Append("Content-Type", "application/json");
                                    await context.Response.WriteAsync(json);
                                }
                            });
                        })
                        .Build();
        return _fakeConsulBuilder.StartAsync();
    }

    public class FakeConsulGetResponse
    {
        public FakeConsulGetResponse(string value)
        {
            Value = value;
        }

        public int CreateIndex => 100;
        public int ModifyIndex => 200;
        public int LockIndex => 200;
        public string Key => "InternalConfiguration";
        public int Flags => 0;
        public string Value { get; }
        public string Session => "adf4238a-882b-9ddc-4a9d-5b6758e4159e";
    }

    private Task GivenThereIsAServiceRunningOn(string url, string basePath, int statusCode, string responseBody)
    {
        _builder = TestHostBuilder.Create()
            .UseUrls(url)
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseIISIntegration()
            .UseUrls(url)
            .Configure(app =>
            {
                app.UsePathBase(basePath);

                app.Run(async context =>
                {
                    context.Response.StatusCode = statusCode;
                    await context.Response.WriteAsync(responseBody);
                });
            })
            .Build();
        return _builder.StartAsync();
    }

    public override void Dispose()
    {
        _builder?.Dispose();
        _fakeConsulBuilder?.Dispose();
        base.Dispose();
    }

    private class FakeCache : IOcelotCache<FileConfiguration>
    {
        public void Add(string key, FileConfiguration value, TimeSpan ttl, string region) => throw new NotImplementedException();
        public FileConfiguration Get(string key, string region) => throw new NotImplementedException();
        public void ClearRegion(string region) => throw new NotImplementedException();
        public void AddAndDelete(string key, FileConfiguration value, TimeSpan ttl, string region) => throw new NotImplementedException();
        public bool TryGetValue(string key, string region, out FileConfiguration value) => throw new NotImplementedException();
    }
}
