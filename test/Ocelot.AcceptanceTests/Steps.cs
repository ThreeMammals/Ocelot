using System;
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
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Shouldly;
using ConfigurationBuilder = Microsoft.Extensions.Configuration.ConfigurationBuilder;
using Ocelot.AcceptanceTests.Caching;
using System.IO.Compression;
using System.Text;
using static Ocelot.AcceptanceTests.HttpDelegatingHandlersTests;
using Ocelot.Requester;
using Ocelot.Middleware.Multiplexer;

namespace Ocelot.AcceptanceTests
{
    using Microsoft.Net.Http.Headers;
    using MediaTypeHeaderValue = System.Net.Http.Headers.MediaTypeHeaderValue;

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
        private WebHostBuilder _ocelotBuilder;
        private IWebHost _ocelotHost;

        public Steps()
        {
            _random = new Random();
        }

        public async Task StartFakeOcelotWithWebSockets()
        {
            _ocelotBuilder = new WebHostBuilder();
            _ocelotBuilder.ConfigureServices(s =>
            {
                s.AddSingleton(_ocelotBuilder);
                s.AddOcelot();
            });
            _ocelotBuilder.UseKestrel()
                .UseUrls("http://localhost:5000")
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
                    var env = hostingContext.HostingEnvironment;
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    config.AddJsonFile("ocelot.json");
                    config.AddEnvironmentVariables();
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                })
                .Configure(app =>
                {
                    app.UseWebSockets();
                    app.UseOcelot().Wait();
                })
                .UseIISIntegration();
            _ocelotHost = _ocelotBuilder.Build();
            await _ocelotHost.StartAsync();
        }

        public void GivenThereIsAConfiguration(FileConfiguration fileConfiguration)
        {
            var configurationPath = TestConfiguration.ConfigurationPath;

            var jsonConfiguration = JsonConvert.SerializeObject(fileConfiguration, Formatting.Indented);

            if (File.Exists(configurationPath))
            {
                File.Delete(configurationPath);
            }

            File.WriteAllText(configurationPath, jsonConfiguration);
        }

        public void GivenThereIsAConfiguration(FileConfiguration fileConfiguration, string configurationPath)
        {
            var jsonConfiguration = JsonConvert.SerializeObject(fileConfiguration, Formatting.Indented);

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

            _webHostBuilder
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
                    var env = hostingContext.HostingEnvironment;
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    config.AddJsonFile("ocelot.json");
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices(s =>
                {
                    s.AddOcelot();
                })
                .Configure(app =>
                {
                    app.UseOcelot().Wait();
                });

            _ocelotServer = new TestServer(_webHostBuilder);

            _ocelotClient = _ocelotServer.CreateClient();
        }

        internal void GivenOcelotIsRunningUsingButterfly(string butterflyUrl)
        {
            _webHostBuilder = new WebHostBuilder();

            _webHostBuilder
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
                    var env = hostingContext.HostingEnvironment;
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    config.AddJsonFile("ocelot.json");
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices(s =>
                {
                    s.AddOcelot()
                    .AddOpenTracing(option =>
                    {
                        //this is the url that the butterfly collector server is running on...
                        option.CollectorUrl = butterflyUrl;
                        option.Service = "Ocelot";
                    });
                })
                .Configure(app =>
                {
                    app.Use(async (context, next) =>
                    {
                        await next.Invoke();
                    });
                    app.UseOcelot().Wait();
                });

            _ocelotServer = new TestServer(_webHostBuilder);

            _ocelotClient = _ocelotServer.CreateClient();
        }

        public void GivenOcelotIsRunningWithMiddleareBeforePipeline<T>(Func<object, Task> callback)
        {
            _webHostBuilder = new WebHostBuilder();

            _webHostBuilder
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
                    var env = hostingContext.HostingEnvironment;
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    config.AddJsonFile("ocelot.json");
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices(s =>
                {
                    s.AddOcelot();
                })
                .Configure(app =>
                {
                    app.UseMiddleware<T>(callback);
                    app.UseOcelot().Wait();
                });

            _ocelotServer = new TestServer(_webHostBuilder);

            _ocelotClient = _ocelotServer.CreateClient();
        }

        public void GivenOcelotIsRunningWithSpecficHandlersRegisteredInDi<TOne, TWo>()
            where TOne : DelegatingHandler
            where TWo : DelegatingHandler
        {
            _webHostBuilder = new WebHostBuilder();

            _webHostBuilder
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
                    var env = hostingContext.HostingEnvironment;
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    config.AddJsonFile("ocelot.json");
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices(s =>
                {
                    s.AddSingleton(_webHostBuilder);
                    s.AddOcelot()
                        .AddSingletonDelegatingHandler<TOne>()
                        .AddSingletonDelegatingHandler<TWo>();
                })
                .Configure(a =>
                {
                    a.UseOcelot().Wait();
                });

            _ocelotServer = new TestServer(_webHostBuilder);

            _ocelotClient = _ocelotServer.CreateClient();
        }

        public void GivenOcelotIsRunningWithSpecficAggregatorsRegisteredInDi<TAggregator, TDepedency>()
            where TAggregator : class, IDefinedAggregator
            where TDepedency : class
        {
            _webHostBuilder = new WebHostBuilder();

            _webHostBuilder
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
                    var env = hostingContext.HostingEnvironment;
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    config.AddJsonFile("ocelot.json");
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices(s =>
                {
                    s.AddSingleton(_webHostBuilder);
                    s.AddSingleton<TDepedency>();
                    s.AddOcelot()
                        .AddSingletonDefinedAggregator<TAggregator>();
                })
                .Configure(a =>
                {
                    a.UseOcelot().Wait();
                });

            _ocelotServer = new TestServer(_webHostBuilder);

            _ocelotClient = _ocelotServer.CreateClient();
        }

        public void GivenOcelotIsRunningWithGlobalHandlersRegisteredInDi<TOne, TWo>() 
            where TOne : DelegatingHandler
            where TWo : DelegatingHandler
        {
            _webHostBuilder = new WebHostBuilder();

            _webHostBuilder
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
                    var env = hostingContext.HostingEnvironment;
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    config.AddJsonFile("ocelot.json");
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices(s =>
                {
                    s.AddSingleton(_webHostBuilder);
                    s.AddOcelot()
                        .AddSingletonDelegatingHandler<TOne>(true)
                        .AddSingletonDelegatingHandler<TWo>(true);
                })
                .Configure(a =>
                {
                    a.UseOcelot().Wait();
                });

            _ocelotServer = new TestServer(_webHostBuilder);

            _ocelotClient = _ocelotServer.CreateClient();
        }

        public void GivenOcelotIsRunningWithGlobalHandlersRegisteredInDi<TOne>(FakeDependency dependency) 
            where TOne : DelegatingHandler
        {
            _webHostBuilder = new WebHostBuilder();

            _webHostBuilder
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
                    var env = hostingContext.HostingEnvironment;
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    config.AddJsonFile("ocelot.json");
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices(s =>
                {
                    s.AddSingleton(_webHostBuilder);
                    s.AddSingleton<FakeDependency>(dependency);
                    s.AddOcelot()
                        .AddSingletonDelegatingHandler<TOne>(true);
                })
                .Configure(a =>
                {
                    a.UseOcelot().Wait();
                });

            _ocelotServer = new TestServer(_webHostBuilder);

            _ocelotClient = _ocelotServer.CreateClient();
        }

        internal void GivenIAddCookieToMyRequest(string cookie)
        {
            _ocelotClient.DefaultRequestHeaders.Add("Set-Cookie", cookie);
        }

        /// <summary>
        /// This is annoying cos it should be in the constructor but we need to set up the file before calling startup so its a step.
        /// </summary>
        public void GivenOcelotIsRunning(Action<IdentityServerAuthenticationOptions> options, string authenticationProviderKey)
        {
            _webHostBuilder = new WebHostBuilder();

            _webHostBuilder
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
                    var env = hostingContext.HostingEnvironment;
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    config.AddJsonFile("ocelot.json");
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices(s =>
                {
                    s.AddOcelot();
                    s.AddAuthentication()
                        .AddIdentityServerAuthentication(authenticationProviderKey, options);
                })
                .Configure(app =>
                {
                    app.UseOcelot().Wait();
                });

            _ocelotServer = new TestServer(_webHostBuilder);

            _ocelotClient = _ocelotServer.CreateClient();
        }

        public void ThenTheResponseHeaderIs(string key, string value)
        {
            var header = _response.Headers.GetValues(key);
            header.First().ShouldBe(value);
        }

        public void ThenTheTraceHeaderIsSet(string key)
        {
            var header = _response.Headers.GetValues(key);
            header.First().ShouldNotBeNullOrEmpty();
        }

        public void GivenOcelotIsRunningUsingJsonSerializedCache()
        {
            _webHostBuilder = new WebHostBuilder();

            _webHostBuilder
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
                    var env = hostingContext.HostingEnvironment;
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    config.AddJsonFile("ocelot.json");
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices(s =>
                {
                    s.AddOcelot()
                        .AddCacheManager((x) =>
                        {
                            x.WithMicrosoftLogging(log =>
                                {
                                    log.AddConsole(LogLevel.Debug);
                                })
                                .WithJsonSerializer()
                                .WithHandle(typeof(InMemoryJsonHandle<>));
                        });
                })
                .Configure(app =>
                {
                    app.UseOcelot().Wait();
                }); 

            _ocelotServer = new TestServer(_webHostBuilder);

            _ocelotClient = _ocelotServer.CreateClient();
        }

        public void GivenOcelotIsRunningUsingConsulToStoreConfig()
        {
            _webHostBuilder = new WebHostBuilder();

            _webHostBuilder
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
                    var env = hostingContext.HostingEnvironment;
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    config.AddJsonFile("ocelot.json");
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices(s =>
                {
                    s.AddOcelot().AddStoreOcelotConfigurationInConsul();
                })
                .Configure(app =>
                {
                    app.UseOcelot().Wait();
                });

            _ocelotServer = new TestServer(_webHostBuilder);

            _ocelotClient = _ocelotServer.CreateClient();
        }

        public void GivenOcelotIsRunningUsingConsulToStoreConfigAndJsonSerializedCache()
        {
            _webHostBuilder = new WebHostBuilder();

            _webHostBuilder
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
                    var env = hostingContext.HostingEnvironment;
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    config.AddJsonFile("ocelot.json");
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices(s =>
                {
                    s.AddOcelot()
                        .AddCacheManager((x) =>
                        {
                            x.WithMicrosoftLogging(log =>
                                {
                                    log.AddConsole(LogLevel.Debug);
                                })
                                .WithJsonSerializer()
                                .WithHandle(typeof(InMemoryJsonHandle<>));
                        })
                        .AddStoreOcelotConfigurationInConsul();
                })
                .Configure(app =>
                {
                    app.UseOcelot().Wait();
                }); 

            _ocelotServer = new TestServer(_webHostBuilder);

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
        public void GivenOcelotIsRunning(OcelotPipelineConfiguration ocelotPipelineConfig)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("ocelot.json")
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
                    a.UseOcelot(ocelotPipelineConfig).Wait();
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
                var response = httpClient.GetAsync($"{url}/.well-known/openid-configuration").GetAwaiter().GetResult();
                var content = response.Content.ReadAsStringAsync().GetAwaiter();
                response.EnsureSuccessStatusCode();
            }
        }

        public void WhenIGetUrlOnTheApiGateway(string url)
        {
            _response = _ocelotClient.GetAsync(url).Result;
        }

        public void WhenIGetUrlOnTheApiGateway(string url, string cookie, string value)
        {
            var request = _ocelotServer.CreateRequest(url);
            request.And(x => { x.Headers.Add("Cookie", new CookieHeaderValue(cookie, value).ToString()); });
            var response = request.GetAsync().Result;
            _response = response;
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

        public void WhenIGetUrlOnTheApiGatewayMultipleTimes(string url, int times, string cookie, string value)
        {
            var tasks = new Task[times];

            for (int i = 0; i < times; i++)
            {
                var urlCopy = url;
                tasks[i] = GetForServiceDiscoveryTest(urlCopy, cookie, value);
                Thread.Sleep(_random.Next(40, 60));
            }

            Task.WaitAll(tasks);
        }

        private async Task GetForServiceDiscoveryTest(string url, string cookie, string value)
        {
            var request = _ocelotServer.CreateRequest(url);
            request.And(x => { x.Headers.Add("Cookie", new CookieHeaderValue(cookie, value).ToString()); });
            var response = await request.GetAsync();
            var content = await response.Content.ReadAsStringAsync();
            int count = int.Parse(content);
            count.ShouldBeGreaterThan(0);
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

        public void GivenThePostHasGzipContent(object input)
        {
            var json = JsonConvert.SerializeObject(input);
            var jsonBytes = Encoding.UTF8.GetBytes(json);
            var ms = new MemoryStream();
            using (var gzip = new GZipStream(ms, CompressionMode.Compress, true))
            {
                gzip.Write(jsonBytes, 0, jsonBytes.Length);
            }

            ms.Position = 0;
            var content = new StreamContent(ms);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            content.Headers.ContentEncoding.Add("gzip");
            _postContent = content;
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
            _ocelotHost?.Dispose();
        }

        public void ThenTheRequestIdIsReturned()
        {
            _response.Headers.GetValues(RequestIdKey).First().ShouldNotBeNullOrEmpty();
        }

        public void ThenTheRequestIdIsReturned(string expected)
        {
            _response.Headers.GetValues(RequestIdKey).First().ShouldBe(expected);
        }

        public void ThenTheContentLengthIs(int expected)
        {
            _response.Content.Headers.ContentLength.ShouldBe(expected);
        }

        public void WhenIMakeLotsOfDifferentRequestsToTheApiGateway()
        {
            int numberOfRequests = 100;
            var aggregateUrl = "/";
            var aggregateExpected = "{\"Laura\":{Hello from Laura},\"Tom\":{Hello from Tom}}";
            var tomUrl = "/tom";
            var tomExpected = "{Hello from Tom}";
            var lauraUrl = "/laura";
            var lauraExpected = "{Hello from Laura}";
            var random = new Random();

            var aggregateTasks = new Task[numberOfRequests];

            for (int i = 0; i < numberOfRequests; i++)
            {
                aggregateTasks[i] = Fire(aggregateUrl, aggregateExpected, random);
            }

            var tomTasks = new Task[numberOfRequests];

            for (int i = 0; i < numberOfRequests; i++)
            {
                tomTasks[i] = Fire(tomUrl, tomExpected, random);
            }

            var lauraTasks = new Task[numberOfRequests];

            for (int i = 0; i < numberOfRequests; i++)
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
            var response = await _ocelotClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            content.ShouldBe(expectedBody);
        }
    }
}
