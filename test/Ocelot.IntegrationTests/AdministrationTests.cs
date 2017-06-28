using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Ocelot.Cache;
using Ocelot.Configuration.File;
using Ocelot.ManualTest;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace Ocelot.IntegrationTests
{
    public class AdministrationTests : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly HttpClient _httpClientTwo;
        private HttpResponseMessage _response;
        private IWebHost _builder;
        private IWebHostBuilder _webHostBuilder;
        private readonly string _ocelotBaseUrl;
        private BearerToken _token;
        private IWebHostBuilder _webHostBuilderTwo;
        private IWebHost _builderTwo;

        public AdministrationTests()
        {
            _httpClient = new HttpClient();
            _httpClientTwo = new HttpClient();
            _ocelotBaseUrl = "http://localhost:5000";
            _httpClient.BaseAddress = new Uri(_ocelotBaseUrl);
        }

        [Fact]
        public void should_return_response_401_with_call_re_routes_controller()
        {
            var configuration = new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    AdministrationPath = "/administration"
                }
            };

            this.Given(x => GivenThereIsAConfiguration(configuration))
                .And(x => GivenOcelotIsRunning())
                .When(x => WhenIGetUrlOnTheApiGateway("/administration/configuration"))
                .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Unauthorized))
                .BDDfy();
        }

         [Fact]
         public void should_return_response_200_with_call_re_routes_controller()
         {
             var configuration = new FileConfiguration
             {
                 GlobalConfiguration = new FileGlobalConfiguration
                 {
                     AdministrationPath = "/administration"
                 }
             };

             this.Given(x => GivenThereIsAConfiguration(configuration))
                 .And(x => GivenOcelotIsRunning())
                 .And(x => GivenIHaveAnOcelotToken("/administration"))
                 .And(x => GivenIHaveAddedATokenToMyRequest())
                 .When(x => WhenIGetUrlOnTheApiGateway("/administration/configuration"))
                 .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                 .BDDfy();
         }

        [Fact]
        public void should_be_able_to_use_token_from_ocelot_a_on_ocelot_b()
        {
            var configuration = new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    AdministrationPath = "/administration"
                }
            };

            this.Given(x => GivenThereIsAConfiguration(configuration))
                .And(x => GivenIdentityServerSigningEnvironmentalVariablesAreSet())
                .And(x => GivenOcelotIsRunning())
                .And(x => GivenIHaveAnOcelotToken("/administration"))
                .And(x => GivenAnotherOcelotIsRunning("http://localhost:5007"))
                .When(x => WhenIGetUrlOnTheSecondOcelot("/administration/configuration"))
                .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .BDDfy();
        }

        [Fact]
        public void should_return_file_configuration()
        {
            var configuration = new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    AdministrationPath = "/administration",
                    RequestIdKey = "RequestId",
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                    {
                        Host = "127.0.0.1",
                        Provider = "test"
                    }

                },
                ReRoutes = new List<FileReRoute>()
                {
                    new FileReRoute()
                    {
                        DownstreamHost = "localhost",
                        DownstreamPort = 80,
                        DownstreamScheme = "https",
                        DownstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "get" },
                        UpstreamPathTemplate = "/",
                        FileCacheOptions = new FileCacheOptions
                        {
                            TtlSeconds = 10,
                            Region = "Geoff"
                        }
                    },
                    new FileReRoute()
                    {
                        DownstreamHost = "localhost",
                        DownstreamPort = 80,
                        DownstreamScheme = "https",
                        DownstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "get" },
                        UpstreamPathTemplate = "/test",
                        FileCacheOptions = new FileCacheOptions
                        {
                            TtlSeconds = 10,
                            Region = "Dave"
                        }
                    }
                }
            };

            this.Given(x => GivenThereIsAConfiguration(configuration))
                .And(x => GivenOcelotIsRunning())
                .And(x => GivenIHaveAnOcelotToken("/administration"))
                .And(x => GivenIHaveAddedATokenToMyRequest())
                .When(x => WhenIGetUrlOnTheApiGateway("/administration/configuration"))
                .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => ThenTheResponseShouldBe(configuration))
                .BDDfy();
        }

        [Fact]
        public void should_get_file_configuration_edit_and_post_updated_version()
        {
            var initialConfiguration = new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    AdministrationPath = "/administration"
                },
                ReRoutes = new List<FileReRoute>()
                {
                    new FileReRoute()
                    {
                        DownstreamHost = "localhost",
                        DownstreamPort = 80,
                        DownstreamScheme = "https",
                        DownstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "get" },
                        UpstreamPathTemplate = "/"
                    },
                    new FileReRoute()
                    {
                        DownstreamHost = "localhost",
                        DownstreamPort = 80,
                        DownstreamScheme = "https",
                        DownstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "get" },
                        UpstreamPathTemplate = "/test"
                    }
                }
            };

             var updatedConfiguration = new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    AdministrationPath = "/administration"
                },
                ReRoutes = new List<FileReRoute>()
                {
                    new FileReRoute()
                    {
                        DownstreamHost = "127.0.0.1",
                        DownstreamPort = 80,
                        DownstreamScheme = "http",
                        DownstreamPathTemplate = "/geoffrey",
                        UpstreamHttpMethod = new List<string> { "get" },
                        UpstreamPathTemplate = "/"
                    },
                    new FileReRoute()
                    {
                        DownstreamHost = "123.123.123",
                        DownstreamPort = 443,
                        DownstreamScheme = "https",
                        DownstreamPathTemplate = "/blooper/{productId}",
                        UpstreamHttpMethod = new List<string> { "post" },
                        UpstreamPathTemplate = "/test"
                    }
                }
            };

            this.Given(x => GivenThereIsAConfiguration(initialConfiguration))
                .And(x => GivenOcelotIsRunning())
                .And(x => GivenIHaveAnOcelotToken("/administration"))
                .And(x => GivenIHaveAddedATokenToMyRequest())
                .When(x => WhenIGetUrlOnTheApiGateway("/administration/configuration"))
                .When(x => WhenIPostOnTheApiGateway("/administration/configuration", updatedConfiguration))
                .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => ThenTheResponseShouldBe(updatedConfiguration))
                .When(x => WhenIGetUrlOnTheApiGateway("/administration/configuration"))
                .And(x => ThenTheResponseShouldBe(updatedConfiguration))
                .BDDfy();
        }

        [Fact]
        public void should_clear_region()
        {
            var initialConfiguration = new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    AdministrationPath = "/administration"
                },
                ReRoutes = new List<FileReRoute>()
                {
                    new FileReRoute()
                    {
                        DownstreamHost = "localhost",
                        DownstreamPort = 80,
                        DownstreamScheme = "https",
                        DownstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "get" },
                        UpstreamPathTemplate = "/",
                        FileCacheOptions = new FileCacheOptions
                        {
                            TtlSeconds = 10
                        }
                    },
                    new FileReRoute()
                    {
                        DownstreamHost = "localhost",
                        DownstreamPort = 80,
                        DownstreamScheme = "https",
                        DownstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "get" },
                        UpstreamPathTemplate = "/test",
                        FileCacheOptions = new FileCacheOptions
                        {
                            TtlSeconds = 10
                        }
                    }
                }
            };

            var regionToClear = "gettest";

            this.Given(x => GivenThereIsAConfiguration(initialConfiguration))
                .And(x => GivenOcelotIsRunning())
                .And(x => GivenIHaveAnOcelotToken("/administration"))
                .And(x => GivenIHaveAddedATokenToMyRequest())
                .When(x => WhenIDeleteOnTheApiGateway($"/administration/outputcache/{regionToClear}"))
                .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.NoContent))
                .BDDfy();
        }

        private void GivenAnotherOcelotIsRunning(string baseUrl)
        {
            _httpClientTwo.BaseAddress = new Uri(baseUrl);

            _webHostBuilderTwo = new WebHostBuilder()
               .UseUrls(baseUrl)
               .UseKestrel()
               .UseContentRoot(Directory.GetCurrentDirectory())
               .ConfigureServices(x => {
                   x.AddSingleton(_webHostBuilderTwo);
               })
               .UseStartup<Startup>();

            _builderTwo = _webHostBuilderTwo.Build();

            _builderTwo.Start();
        }

        private void GivenIdentityServerSigningEnvironmentalVariablesAreSet()
        {
            Environment.SetEnvironmentVariable("OCELOT_CERTIFICATE", "idsrv3test.pfx");
            Environment.SetEnvironmentVariable("OCELOT_CERTIFICATE_PASSWORD", "idsrv3test");
        }

        private void WhenIGetUrlOnTheSecondOcelot(string url)
        {
            _httpClientTwo.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token.AccessToken);
            _response = _httpClientTwo.GetAsync(url).Result;
        }

        private void WhenIPostOnTheApiGateway(string url, FileConfiguration updatedConfiguration)
        {
            var json = JsonConvert.SerializeObject(updatedConfiguration);
            var content = new StringContent(json);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            _response = _httpClient.PostAsync(url, content).Result;
        }

        private void ThenTheResponseShouldBe(List<string> expected)
        {
            var content = _response.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<Regions>(content);
            result.Value.ShouldBe(expected);
        }

        private void ThenTheResponseShouldBe(FileConfiguration expected)
        {
            var response = JsonConvert.DeserializeObject<FileConfiguration>(_response.Content.ReadAsStringAsync().Result);

            response.GlobalConfiguration.AdministrationPath.ShouldBe(expected.GlobalConfiguration.AdministrationPath);
            response.GlobalConfiguration.RequestIdKey.ShouldBe(expected.GlobalConfiguration.RequestIdKey);
            response.GlobalConfiguration.ServiceDiscoveryProvider.Host.ShouldBe(expected.GlobalConfiguration.ServiceDiscoveryProvider.Host);
            response.GlobalConfiguration.ServiceDiscoveryProvider.Port.ShouldBe(expected.GlobalConfiguration.ServiceDiscoveryProvider.Port);
            response.GlobalConfiguration.ServiceDiscoveryProvider.Provider.ShouldBe(expected.GlobalConfiguration.ServiceDiscoveryProvider.Provider);

            for (var i = 0; i < response.ReRoutes.Count; i++)
            {
                response.ReRoutes[i].DownstreamHost.ShouldBe(expected.ReRoutes[i].DownstreamHost);
                response.ReRoutes[i].DownstreamPathTemplate.ShouldBe(expected.ReRoutes[i].DownstreamPathTemplate);
                response.ReRoutes[i].DownstreamPort.ShouldBe(expected.ReRoutes[i].DownstreamPort);
                response.ReRoutes[i].DownstreamScheme.ShouldBe(expected.ReRoutes[i].DownstreamScheme);
                response.ReRoutes[i].UpstreamPathTemplate.ShouldBe(expected.ReRoutes[i].UpstreamPathTemplate);
                response.ReRoutes[i].UpstreamHttpMethod.ShouldBe(expected.ReRoutes[i].UpstreamHttpMethod);
            }
        }

        private void GivenIHaveAddedATokenToMyRequest()
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token.AccessToken);
        }

        private void GivenIHaveAnOcelotToken(string adminPath)
        {
            var tokenUrl = $"{adminPath}/connect/token";
            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("client_id", "admin"),
                new KeyValuePair<string, string>("client_secret", "secret"),
                new KeyValuePair<string, string>("scope", "admin"),
                new KeyValuePair<string, string>("username", "admin"),
                new KeyValuePair<string, string>("password", "secret"),
                new KeyValuePair<string, string>("grant_type", "password")
            };
            var content = new FormUrlEncodedContent(formData);

            var response = _httpClient.PostAsync(tokenUrl, content).Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;
            response.EnsureSuccessStatusCode();
            _token = JsonConvert.DeserializeObject<BearerToken>(responseContent);
            var configPath = $"{adminPath}/.well-known/openid-configuration";
            response = _httpClient.GetAsync(configPath).Result;
            response.EnsureSuccessStatusCode();
        }

        private void GivenOcelotIsRunning()
        {
            _webHostBuilder = new WebHostBuilder()
                .UseUrls(_ocelotBaseUrl)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureServices(x => {
                    x.AddSingleton(_webHostBuilder);
                })
                .UseStartup<Startup>();

              _builder = _webHostBuilder.Build();

            _builder.Start();
        }

        private void GivenThereIsAConfiguration(FileConfiguration fileConfiguration)
        {
            var configurationPath = $"{Directory.GetCurrentDirectory()}/configuration.json";

            var jsonConfiguration = JsonConvert.SerializeObject(fileConfiguration);

            if (File.Exists(configurationPath))
            {
                File.Delete(configurationPath);
            }

            File.WriteAllText(configurationPath, jsonConfiguration);

            var text = File.ReadAllText(configurationPath);

            configurationPath = $"{AppContext.BaseDirectory}/configuration.json";

            if (File.Exists(configurationPath))
            {
                File.Delete(configurationPath);
            }

            File.WriteAllText(configurationPath, jsonConfiguration);

            text = File.ReadAllText(configurationPath);
        }

        private void WhenIGetUrlOnTheApiGateway(string url)
        {
            _response = _httpClient.GetAsync(url).Result;
        }

        private void WhenIDeleteOnTheApiGateway(string url)
        {
            _response = _httpClient.DeleteAsync(url).Result;
        }

        private void ThenTheStatusCodeShouldBe(HttpStatusCode expectedHttpStatusCode)
        {
            _response.StatusCode.ShouldBe(expectedHttpStatusCode);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("OCELOT_CERTIFICATE", "");
            Environment.SetEnvironmentVariable("OCELOT_CERTIFICATE_PASSWORD", "");
            _builder?.Dispose();
            _httpClient?.Dispose();
        }
    }
}
