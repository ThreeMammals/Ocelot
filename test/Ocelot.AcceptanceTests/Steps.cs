﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using CacheManager.Core;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.ServiceDiscovery;
using Shouldly;
using ConfigurationBuilder = Microsoft.Extensions.Configuration.ConfigurationBuilder;
using Ocelot.AcceptanceTests.Caching;

namespace Ocelot.AcceptanceTests
{
    public class Steps : IDisposable
    {
        private TestServer _ocelotServer;
        private HttpClient _ocelotClient;
        private HttpResponseMessage _response;
        private HttpContent _postContent;
        private BearerToken _token;
        public HttpClient OcelotClient => _ocelotClient;
        public string RequestIdKey = "OcRequestId";
        private readonly Random _random;
        private IWebHostBuilder _webHostBuilder;

        public Steps()
        {
            _random = new Random();
        }

        public void GivenThereIsAConfiguration(FileConfiguration fileConfiguration)
        {
            var configurationPath = TestConfiguration.ConfigurationPath;

            var jsonConfiguration = JsonConvert.SerializeObject(fileConfiguration);

            if (File.Exists(configurationPath))
            {
                File.Delete(configurationPath);
            }

            File.WriteAllText(configurationPath, jsonConfiguration);
        }

        public void GivenThereIsAConfiguration(FileConfiguration fileConfiguration, string configurationPath)
        {
            var jsonConfiguration = JsonConvert.SerializeObject(fileConfiguration);

            if (File.Exists(configurationPath))
            {
                File.Delete(configurationPath);
            }

            File.WriteAllText(configurationPath, jsonConfiguration);
        }

        /// <summary>
        /// This is annoying cos it should be in the constructor but we need to set up the file before calling startup so its a step.
        /// </summary>
        public void GivenOcelotIsRunning()
        {
            _webHostBuilder = new WebHostBuilder();

            _webHostBuilder.ConfigureServices(s =>
            {
                s.AddSingleton(_webHostBuilder);
            });

            _ocelotServer = new TestServer(_webHostBuilder
                .UseStartup<AcceptanceTestsStartup>());

            _ocelotClient = _ocelotServer.CreateClient();
        }

        /// <summary>
        /// This is annoying cos it should be in the constructor but we need to set up the file before calling startup so its a step.
        /// </summary>
        public void GivenOcelotIsRunning(Action<IdentityServerAuthenticationOptions> options, string authenticationProviderKey)
        {
            _webHostBuilder = new WebHostBuilder();

            _webHostBuilder.ConfigureServices(s =>
            {
                s.AddSingleton(_webHostBuilder);
                s.AddAuthentication()
                    .AddIdentityServerAuthentication(authenticationProviderKey, options);
            });

            _ocelotServer = new TestServer(_webHostBuilder
                .UseStartup<AcceptanceTestsStartup>());

            _ocelotClient = _ocelotServer.CreateClient();
        }

        public void ThenTheResponseHeaderIs(string key, string value)
        {
            var header = _response.Headers.GetValues(key);
            header.First().ShouldBe(value);
        }

        public void GivenOcelotIsRunningUsingJsonSerializedCache()
        {
            _webHostBuilder = new WebHostBuilder();

            _webHostBuilder.ConfigureServices(s =>
            {
                s.AddSingleton(_webHostBuilder);
            });

            _ocelotServer = new TestServer(_webHostBuilder
                .UseStartup<StartupWithCustomCacheHandle>());

            _ocelotClient = _ocelotServer.CreateClient();
        }

        public void GivenOcelotIsRunningUsingConsulToStoreConfig()
        {
            _webHostBuilder = new WebHostBuilder();

            _webHostBuilder.ConfigureServices(s =>
            {
                s.AddSingleton(_webHostBuilder);
            });

            _ocelotServer = new TestServer(_webHostBuilder
                .UseStartup<ConsulStartup>());

            _ocelotClient = _ocelotServer.CreateClient();
        }

        public void GivenOcelotIsRunningUsingConsulToStoreConfigAndJsonSerializedCache()
        {
            _webHostBuilder = new WebHostBuilder();

            _webHostBuilder.ConfigureServices(s =>
            {
                s.AddSingleton(_webHostBuilder);
            });

            _ocelotServer = new TestServer(_webHostBuilder
                .UseStartup<StartupWithConsulAndCustomCacheHandle>());

            _ocelotClient = _ocelotServer.CreateClient();
        }

        internal void ThenTheResponseShouldBe(FileConfiguration expecteds)
        {
            var response = JsonConvert.DeserializeObject<FileConfiguration>(_response.Content.ReadAsStringAsync().Result);

            response.GlobalConfiguration.RequestIdKey.ShouldBe(expecteds.GlobalConfiguration.RequestIdKey);
            response.GlobalConfiguration.ServiceDiscoveryProvider.Host.ShouldBe(expecteds.GlobalConfiguration.ServiceDiscoveryProvider.Host);
            response.GlobalConfiguration.ServiceDiscoveryProvider.Port.ShouldBe(expecteds.GlobalConfiguration.ServiceDiscoveryProvider.Port);

            for (var i = 0; i < response.ReRoutes.Count; i++)
            {
                for (var j = 0; j < response.ReRoutes[i].DownstreamHostAndPorts.Count; j++)
                {
                    var result = response.ReRoutes[i].DownstreamHostAndPorts[j];
                    var expected = expecteds.ReRoutes[i].DownstreamHostAndPorts[j];
                    result.Host.ShouldBe(expected.Host);
                    result.Port.ShouldBe(expected.Port);
                }

                response.ReRoutes[i].DownstreamPathTemplate.ShouldBe(expecteds.ReRoutes[i].DownstreamPathTemplate);
                response.ReRoutes[i].DownstreamScheme.ShouldBe(expecteds.ReRoutes[i].DownstreamScheme);
                response.ReRoutes[i].UpstreamPathTemplate.ShouldBe(expecteds.ReRoutes[i].UpstreamPathTemplate);
                response.ReRoutes[i].UpstreamHttpMethod.ShouldBe(expecteds.ReRoutes[i].UpstreamHttpMethod);
            }
        }

        /// <summary>
        /// This is annoying cos it should be in the constructor but we need to set up the file before calling startup so its a step.
        /// </summary>
        public void GivenOcelotIsRunning(OcelotMiddlewareConfiguration ocelotMiddlewareConfig)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("configuration.json")
                .AddEnvironmentVariables();

            var configuration = builder.Build();
            _webHostBuilder = new WebHostBuilder();
            _webHostBuilder.ConfigureServices(s =>
            {
                s.AddSingleton(_webHostBuilder);
            });

            _ocelotServer = new TestServer(_webHostBuilder
                .UseConfiguration(configuration)
                .ConfigureServices(s =>
                {
                    Action<ConfigurationBuilderCachePart> settings = (x) =>
                    {
                        x.WithMicrosoftLogging(log =>
                        {
                            log.AddConsole(LogLevel.Debug);
                        })
                        .WithDictionaryHandle();
                    };

                    s.AddOcelot(configuration);
                })
                .ConfigureLogging(l =>
                {
                    l.AddConsole();
                    l.AddDebug();
                })
                .Configure(a =>
                {
                    a.UseOcelot(ocelotMiddlewareConfig).Wait();
                }));

            _ocelotClient = _ocelotServer.CreateClient();
        }

        public void GivenIHaveAddedATokenToMyRequest()
        {
            _ocelotClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token.AccessToken);
        }

        public void GivenIHaveAToken(string url)
        {
            var tokenUrl = $"{url}/connect/token";
            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("client_id", "client"),
                new KeyValuePair<string, string>("client_secret", "secret"),
                new KeyValuePair<string, string>("scope", "api"),
                new KeyValuePair<string, string>("username", "test"),
                new KeyValuePair<string, string>("password", "test"),
                new KeyValuePair<string, string>("grant_type", "password")
            };
            var content = new FormUrlEncodedContent(formData);

            using (var httpClient = new HttpClient())
            {
                var response = httpClient.PostAsync(tokenUrl, content).Result;
                var responseContent = response.Content.ReadAsStringAsync().Result;
                response.EnsureSuccessStatusCode();
                _token = JsonConvert.DeserializeObject<BearerToken>(responseContent);
            }
        }

        public void GivenIHaveATokenForApiReadOnlyScope(string url)
        {
            var tokenUrl = $"{url}/connect/token";
            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("client_id", "client"),
                new KeyValuePair<string, string>("client_secret", "secret"),
                new KeyValuePair<string, string>("scope", "api.readOnly"),
                new KeyValuePair<string, string>("username", "test"),
                new KeyValuePair<string, string>("password", "test"),
                new KeyValuePair<string, string>("grant_type", "password")
            };
            var content = new FormUrlEncodedContent(formData);

            using (var httpClient = new HttpClient())
            {
                var response = httpClient.PostAsync(tokenUrl, content).Result;
                var responseContent = response.Content.ReadAsStringAsync().Result;
                response.EnsureSuccessStatusCode();
                _token = JsonConvert.DeserializeObject<BearerToken>(responseContent);
            }
        }

        public void GivenIHaveATokenForApi2(string url)
        {
            var tokenUrl = $"{url}/connect/token";
            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("client_id", "client"),
                new KeyValuePair<string, string>("client_secret", "secret"),
                new KeyValuePair<string, string>("scope", "api2"),
                new KeyValuePair<string, string>("username", "test"),
                new KeyValuePair<string, string>("password", "test"),
                new KeyValuePair<string, string>("grant_type", "password")
            };
            var content = new FormUrlEncodedContent(formData);

            using (var httpClient = new HttpClient())
            {
                var response = httpClient.PostAsync(tokenUrl, content).Result;
                var responseContent = response.Content.ReadAsStringAsync().Result;
                response.EnsureSuccessStatusCode();
                _token = JsonConvert.DeserializeObject<BearerToken>(responseContent);
            }
        }

        public void GivenIHaveAnOcelotToken(string adminPath)
        {
            var tokenUrl = $"{adminPath}/connect/token";
            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("client_id", "admin"),
                new KeyValuePair<string, string>("client_secret", "secret"),
                new KeyValuePair<string, string>("scope", "admin"),
                new KeyValuePair<string, string>("username", "admin"),
                new KeyValuePair<string, string>("password", "admin"),
                new KeyValuePair<string, string>("grant_type", "password")
            };
            var content = new FormUrlEncodedContent(formData);

            var response = _ocelotClient.PostAsync(tokenUrl, content).Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;
            response.EnsureSuccessStatusCode();
            _token = JsonConvert.DeserializeObject<BearerToken>(responseContent);
        }

        public void VerifyIdentiryServerStarted(string url)
        {
            using (var httpClient = new HttpClient())
            {
                var response = httpClient.GetAsync($"{url}/.well-known/openid-configuration").Result;
                response.EnsureSuccessStatusCode();
            }
        }

        public void WhenIGetUrlOnTheApiGateway(string url)
        {
            _response = _ocelotClient.GetAsync(url).Result;
        }

        public void GivenIAddAHeader(string key, string value)
        {
            _ocelotClient.DefaultRequestHeaders.Add(key, value);
        }

        public void WhenIGetUrlOnTheApiGatewayMultipleTimes(string url, int times)
        {
            var tasks = new Task[times];

            for (int i = 0; i < times; i++)
            {
                var urlCopy = url;
                tasks[i] = GetForServiceDiscoveryTest(urlCopy);
                Thread.Sleep(_random.Next(40, 60));
            }

            Task.WaitAll(tasks);
        }

        private async Task GetForServiceDiscoveryTest(string url)
        {
            var response = await _ocelotClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            int count = int.Parse(content);
            count.ShouldBeGreaterThan(0);
        }

        public void WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit(string url, int times)
        {
            for (int i = 0; i < times; i++)
            {
                var clientId = "ocelotclient1";
                var request = new HttpRequestMessage(new HttpMethod("GET"), url);
                request.Headers.Add("ClientId", clientId);
                _response = _ocelotClient.SendAsync(request).Result;
            }
        }

        public void WhenIGetUrlOnTheApiGateway(string url, string requestId)
        {
            _ocelotClient.DefaultRequestHeaders.TryAddWithoutValidation(RequestIdKey, requestId);

            _response = _ocelotClient.GetAsync(url).Result;
        }

        public void WhenIPostUrlOnTheApiGateway(string url)
        {
            _response = _ocelotClient.PostAsync(url, _postContent).Result;
        }

        public void GivenThePostHasContent(string postcontent)
        {
            _postContent = new StringContent(postcontent);
        }

        public void ThenTheResponseBodyShouldBe(string expectedBody)
        {
            _response.Content.ReadAsStringAsync().Result.ShouldBe(expectedBody);
        }

        public void ThenTheStatusCodeShouldBe(HttpStatusCode expectedHttpStatusCode)
        {
            _response.StatusCode.ShouldBe(expectedHttpStatusCode);
        }


        public void ThenTheStatusCodeShouldBe(int expectedHttpStatusCode)
        {
            var responseStatusCode = (int)_response.StatusCode;
            responseStatusCode.ShouldBe(expectedHttpStatusCode);
        }

        public void Dispose()
        {
            _ocelotClient?.Dispose();
            _ocelotServer?.Dispose();
        }

        public void ThenTheRequestIdIsReturned()
        {
            _response.Headers.GetValues(RequestIdKey).First().ShouldNotBeNullOrEmpty();
        }

        public void ThenTheRequestIdIsReturned(string expected)
        {
            _response.Headers.GetValues(RequestIdKey).First().ShouldBe(expected);
        }
    }
}
