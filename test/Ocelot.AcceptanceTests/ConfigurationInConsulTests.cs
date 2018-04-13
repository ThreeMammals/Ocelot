using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Ocelot.Configuration.File;
using Shouldly;
using TestStack.BDDfy;
using Xunit;
using static Ocelot.Infrastructure.Wait;

namespace Ocelot.AcceptanceTests
{
    public class ConfigurationInConsulTests : IDisposable
    {
        private IWebHost _builder;
        private readonly Steps _steps;
        private IWebHost _fakeConsulBuilder;
        private FileConfiguration _config;

        public ConfigurationInConsulTests()
        {
            _steps = new Steps();
        }

        [Fact]
        public void should_return_response_200_with_simple_url()
        {
            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new FileHostAndPort
                                {
                                    Host = "localhost",
                                    Port = 51779,
                                }
                            },
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        }
                    },
                GlobalConfiguration = new FileGlobalConfiguration()
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider()
                    {
                        Host = "localhost",
                        Port = 9500
                    }
                }
            };

            var fakeConsulServiceDiscoveryUrl = "http://localhost:9500";

            this.Given(x => GivenThereIsAFakeConsulServiceDiscoveryProvider(fakeConsulServiceDiscoveryUrl))
                .And(x => x.GivenThereIsAServiceRunningOn("http://localhost:51779", "", 200, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunningUsingConsulToStoreConfig())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200_with_simple_url_when_using_jsonserialized_cache()
        {
            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new FileHostAndPort
                                {
                                    Host = "localhost",
                                    Port = 51779,
                                }
                            },
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        }
                    },
                GlobalConfiguration = new FileGlobalConfiguration()
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider()
                    {
                        Host = "localhost",
                        Port = 9502
                    }
                }
            };

            var fakeConsulServiceDiscoveryUrl = "http://localhost:9502";

            this.Given(x => GivenThereIsAFakeConsulServiceDiscoveryProvider(fakeConsulServiceDiscoveryUrl))
                .And(x => x.GivenThereIsAServiceRunningOn("http://localhost:51779", "", 200, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunningUsingConsulToStoreConfigAndJsonSerializedCache())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Fact]
        public void should_load_configuration_out_of_consul()
        {
            var consulPort = 8500;
            var configuration = new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration()
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider()
                    {
                        Host = "localhost",
                        Port = consulPort
                    }
                }
            };

            var fakeConsulServiceDiscoveryUrl = $"http://localhost:{consulPort}";

            var consulConfig = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/status",
                        DownstreamScheme = "http",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "localhost",
                                Port = 51779,
                            }
                        },
                        UpstreamPathTemplate = "/cs/status",
                        UpstreamHttpMethod = new List<string> {"Get"}
                    }
                },
                GlobalConfiguration = new FileGlobalConfiguration()
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider()
                    {
                        Host = "localhost",
                        Port = consulPort
                    }
                }
            };

            this.Given(x => GivenTheConsulConfigurationIs(consulConfig))
                .And(x => GivenThereIsAFakeConsulServiceDiscoveryProvider(fakeConsulServiceDiscoveryUrl))
                .And(x => x.GivenThereIsAServiceRunningOn("http://localhost:51779", "/status", 200, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunningUsingConsulToStoreConfig())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/cs/status"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Fact]
        public void should_load_configuration_out_of_consul_if_it_is_changed()
        {
            var consulPort = 8506;
            var configuration = new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration()
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider()
                    {
                        Host = "localhost",
                        Port = consulPort
                    }
                }
            };

            var fakeConsulServiceDiscoveryUrl = $"http://localhost:{consulPort}";

            var consulConfig = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/status",
                        DownstreamScheme = "http",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "localhost",
                                Port = 51780,
                            }
                        },
                        UpstreamPathTemplate = "/cs/status",
                        UpstreamHttpMethod = new List<string> {"Get"}
                    }
                },
                GlobalConfiguration = new FileGlobalConfiguration()
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider()
                    {
                        Host = "localhost",
                        Port = consulPort
                    }
                }
            };

            var secondConsulConfig = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/status",
                        DownstreamScheme = "http",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "localhost",
                                Port = 51780,
                            }
                        },
                        UpstreamPathTemplate = "/cs/status/awesome",
                        UpstreamHttpMethod = new List<string> {"Get"}
                    }
                },
                GlobalConfiguration = new FileGlobalConfiguration()
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider()
                    {
                        Host = "localhost",
                        Port = consulPort
                    }
                }
            };

            this.Given(x => GivenTheConsulConfigurationIs(consulConfig))
                .And(x => GivenThereIsAFakeConsulServiceDiscoveryProvider(fakeConsulServiceDiscoveryUrl))
                .And(x => x.GivenThereIsAServiceRunningOn("http://localhost:51780", "/status", 200, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunningUsingConsulToStoreConfig())
                .And(x => _steps.WhenIGetUrlOnTheApiGateway("/cs/status"))
                .And(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .When(x => GivenTheConsulConfigurationIs(secondConsulConfig))
                .Then(x => ThenTheConfigIsUpdatedInOcelot())
                .BDDfy();
        }

        private void ThenTheConfigIsUpdatedInOcelot()
        {
            var result = WaitFor(20000).Until(() => {
                try
                {
                    _steps.WhenIGetUrlOnTheApiGateway("/cs/status/awesome");
                    _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
                    _steps.ThenTheResponseBodyShouldBe("Hello from Laura");
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

        private void GivenThereIsAFakeConsulServiceDiscoveryProvider(string url)
        {
            _fakeConsulBuilder = new WebHostBuilder()
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

                                        await context.Response.WriteJsonAsync(new FakeConsulGetResponse[] { kvp });
                                    }                               
                                    else if (context.Request.Method.ToLower() == "put" && context.Request.Path.Value == "/v1/kv/InternalConfiguration")
                                    {
                                        try
                                        {
                                            var reader = new StreamReader(context.Request.Body);

                                            var json = reader.ReadToEnd();

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
                                });
                            })
                            .Build();

            _fakeConsulBuilder.Start();
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
            public string Value { get; private set; }
            public string Session => "adf4238a-882b-9ddc-4a9d-5b6758e4159e";
        }

        private void GivenThereIsAServiceRunningOn(string url, string basePath, int statusCode, string responseBody)
        {
            _builder = new WebHostBuilder()
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

            _builder.Start();
        }

        public void Dispose()
        {
            _builder?.Dispose();
            _steps.Dispose();
        }
    }
}
