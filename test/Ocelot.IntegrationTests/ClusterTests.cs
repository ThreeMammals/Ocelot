using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Ocelot.Configuration.File;
using Ocelot.ManualTest;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.IntegrationTests
{
    public class ClusterTests : IDisposable
    {
        private readonly HttpClient _httpClient;
        private HttpResponseMessage _response;
        private BearerToken _token;
        private List<string> _remoteServerLocations;
        private List<IWebHost> _builders;

        public ClusterTests()
        {
            _remoteServerLocations = new List<string>();
            _builders = new List<IWebHost>();
            _httpClient = new HttpClient();
        }

        [Fact]
        public void should_get_file_configuration_edit_and_post_updated_version()
        {
            Thread.Sleep(10000);

              var remoteServers = new List<string>
             {
                 "http://localhost:4231",
                 "http://localhost:4232",
                 "http://localhost:4233",
                 "http://localhost:4234",
                 "http://localhost:4235",
             };

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
                        UpstreamHttpMethod = "get",
                        UpstreamPathTemplate = "/"
                    },
                    new FileReRoute()
                    {
                        DownstreamHost = "localhost",
                        DownstreamPort = 80,
                        DownstreamScheme = "https",
                        DownstreamPathTemplate = "/",
                        UpstreamHttpMethod = "get",
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
                        UpstreamHttpMethod = "get",
                        UpstreamPathTemplate = "/"
                    },
                    new FileReRoute()
                    {
                        DownstreamHost = "123.123.123",
                        DownstreamPort = 443,
                        DownstreamScheme = "https",
                        DownstreamPathTemplate = "/blooper/{productId}",
                        UpstreamHttpMethod = "post",
                        UpstreamPathTemplate = "/test"
                    }
                }
            };

            this.Given(x => GivenThereIsAConfiguration(initialConfiguration))
                .And(x => GivenFiveOcelotsAreRunning(remoteServers))
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


        private void WhenIPostOnTheApiGateway(string url, FileConfiguration updatedConfiguration)
        {
            var json = JsonConvert.SerializeObject(updatedConfiguration);
            var content = new StringContent(json);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            _response = _httpClient.PostAsync(url, content).Result;
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

        private void GivenFiveOcelotsAreRunning(List<string> remoteServers)
        {
            _remoteServerLocations = remoteServers;

            var tasks = new Task[_remoteServerLocations.Count];
            for (int i = 0; i < tasks.Length; i++)
            {
                var remoteServerLocation = _remoteServerLocations[i];
                Thread.Sleep(500);
                tasks[i] = GivenAnOcelotIsRunning(remoteServerLocation);
            }

            Task.WaitAll(tasks);
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

        private void ThenTheStatusCodeShouldBe(HttpStatusCode expectedHttpStatusCode)
        {
            _response.StatusCode.ShouldBe(expectedHttpStatusCode);
        }

        private async Task GivenAnOcelotIsRunning(string baseUrl)
        {
            IWebHost webHost = new WebHostBuilder();
            webHost
                .UseUrls(baseUrl)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureServices(x =>
                {
                    x.AddSingleton(webHost);
                })
                .UseStartup<Startup>()
                .Build();

                webHost.Start();

            _builders.Add(webHost);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}