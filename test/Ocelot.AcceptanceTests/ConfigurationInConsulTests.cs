using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.ServiceDiscovery;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.AcceptanceTests
{
    public class ConfigurationInConsul : IDisposable
    {
        private IWebHost _builder;
        private readonly Steps _steps;
        private IWebHost _fakeConsulBuilder;
        private IOcelotConfiguration _config;

        public ConfigurationInConsul()
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
                            DownstreamHost = "localhost",
                            DownstreamPort = 51779,
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        }
                    },
                GlobalConfiguration = new FileGlobalConfiguration()
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider()
                    {
                        Provider = "Consul",
                        Host = "localhost",
                        Port = 9500
                    }
                }
            };

            var fakeConsulServiceDiscoveryUrl = "http://localhost:9500";

            var consulConfig = new ConsulRegistryConfiguration("localhost", 9500, "Ocelot");

            this.Given(x => GivenThereIsAFakeConsulServiceDiscoveryProvider(fakeConsulServiceDiscoveryUrl))
                .And(x => x.GivenThereIsAServiceRunningOn("http://localhost:51779", 200, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunningUsingConsulToStoreConfig(consulConfig))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
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
                                    if (context.Request.Method.ToLower() == "get" && context.Request.Path.Value == "/v1/kv/OcelotConfiguration")
                                    {
                                        var json = JsonConvert.SerializeObject(_config);

                                        var bytes = Encoding.UTF8.GetBytes(json);

                                        var base64 = Convert.ToBase64String(bytes);

                                        var kvp = new FakeConsulGetResponse(base64);

                                        await context.Response.WriteJsonAsync(new FakeConsulGetResponse[]{kvp});
                                    }

                                    else if (context.Request.Method.ToLower() == "put" && context.Request.Path.Value == "/v1/kv/OcelotConfiguration")
                                    {
                                        try
                                        {
                                            var reader = new StreamReader(context.Request.Body);

                                            var json = reader.ReadToEnd();

                                            var settings = new JsonSerializerSettings();
                                            settings.Converters.Add(new AuthenticationConfigConverter());
                                            _config = JsonConvert.DeserializeObject<OcelotConfiguration>(json, settings);

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
            public string Key => "OcelotConfiguration";
            public int Flags => 0;
            public string Value { get; private set; }
            public string Session => "adf4238a-882b-9ddc-4a9d-5b6758e4159e";
        }

        private void GivenThereIsAServiceRunningOn(string url, int statusCode, string responseBody)
        {
            _builder = new WebHostBuilder()
                .UseUrls(url)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseUrls(url)
                .Configure(app =>
                {
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