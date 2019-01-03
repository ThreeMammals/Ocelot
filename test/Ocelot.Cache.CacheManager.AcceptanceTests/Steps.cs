namespace Ocelot.Cache.CacheManager.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Caching;
    using Configuration.Creator;
    using Configuration.File;
    using Configuration.Repository;
    using DependencyInjection;
    using global::CacheManager.Core;
    using IdentityServer4.AccessTokenValidation;
    using Infrastructure;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Ocelot.Middleware;
    using Ocelot.Middleware.Multiplexer;
    using Shouldly;
    using CookieHeaderValue = Microsoft.Net.Http.Headers.CookieHeaderValue;
    using MediaTypeHeaderValue = System.Net.Http.Headers.MediaTypeHeaderValue;

    public class Steps : IDisposable
    {
        private TestServer _ocelotServer;
        private HttpClient _ocelotClient;
        private HttpResponseMessage _response;
        private HttpContent _postContent;
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
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: false);
                    config.AddJsonFile("ocelot.json", false, false);
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices(s =>
                {
                    s.AddOcelot().AddCacheManager(x => x.WithDictionaryHandle());
                })
                .Configure(app =>
                {
                    app.UseOcelot().Wait();
                });

            _ocelotServer = new TestServer(_webHostBuilder);

            _ocelotClient = _ocelotServer.CreateClient();
        }

        public void ThenTheResponseBodyHeaderIs(string key, string value)
        {
            var header = _response.Content.Headers.GetValues(key);
            header.First().ShouldBe(value);
        }

        public void GivenOcelotIsRunningUsingJsonSerializedCache()
        {
            _webHostBuilder = new WebHostBuilder();

            _webHostBuilder
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
                    var env = hostingContext.HostingEnvironment;
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: false);
                    config.AddJsonFile("ocelot.json", false, false);
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
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: false);
                    config.AddJsonFile("ocelot.json", optional: true, reloadOnChange: false);
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
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: false);
                    config.AddJsonFile("ocelot.json", optional: true, reloadOnChange: false);
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

        public void WhenIGetUrlOnTheApiGateway(string url)
        {
            _response = _ocelotClient.GetAsync(url).Result;
        }

        public void ThenTheResponseBodyShouldBe(string expectedBody)
        {
            _response.Content.ReadAsStringAsync().Result.ShouldBe(expectedBody);
        }

        public void ThenTheStatusCodeShouldBe(HttpStatusCode expectedHttpStatusCode)
        {
            _response.StatusCode.ShouldBe(expectedHttpStatusCode);
        }

        public void Dispose()
        {
            _ocelotClient?.Dispose();
            _ocelotServer?.Dispose();
            _ocelotHost?.Dispose();
        }

        public void ThenTheContentLengthIs(int expected)
        {
            _response.Content.Headers.ContentLength.ShouldBe(expected);
        }
    }
}
