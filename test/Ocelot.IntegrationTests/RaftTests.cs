using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Ocelot.Configuration.File;
using Ocelot.Raft;
using Rafty.Concensus;
using Rafty.Infrastructure;
using Shouldly;
using Xunit;
using static Rafty.Infrastructure.Wait;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

namespace Ocelot.IntegrationTests
{
    using Xunit.Abstractions;

    public class RaftTests : IDisposable
    {
        private readonly List<IWebHost> _builders;
        private readonly List<IWebHostBuilder> _webHostBuilders;
        private readonly List<Thread> _threads;
        private FilePeers _peers;
        private readonly HttpClient _httpClient;
        private readonly HttpClient _httpClientForAssertions;
        private BearerToken _token;
        private HttpResponseMessage _response;
        private static readonly object _lock = new object();
        private ITestOutputHelper _output;

        public RaftTests(ITestOutputHelper output)
        {
            _output = output;
            _httpClientForAssertions = new HttpClient();
            _httpClient = new HttpClient();
            var ocelotBaseUrl = "http://localhost:5000";
            _httpClient.BaseAddress = new Uri(ocelotBaseUrl);
            _webHostBuilders = new List<IWebHostBuilder>();
            _builders = new List<IWebHost>();
            _threads = new List<Thread>();
        }
        
        [Fact(Skip = "still broken waiting for work in rafty")]
        public void should_persist_command_to_five_servers()
        {
             var configuration = new FileConfiguration
             { 
                 GlobalConfiguration = new FileGlobalConfiguration
                 {
                 }
             };

            var updatedConfiguration = new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration
                {
                },
                ReRoutes = new List<FileReRoute>()
                {
                    new FileReRoute()
                    {
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "127.0.0.1",
                                Port = 80,
                            }
                        },
                        DownstreamScheme = "http",
                        DownstreamPathTemplate = "/geoffrey",
                        UpstreamHttpMethod = new List<string> { "get" },
                        UpstreamPathTemplate = "/"
                    },
                    new FileReRoute()
                    {
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "123.123.123",
                                Port = 443,
                            }
                        },
                        DownstreamScheme = "https",
                        DownstreamPathTemplate = "/blooper/{productId}",
                        UpstreamHttpMethod = new List<string> { "post" },
                        UpstreamPathTemplate = "/test"
                    }
                }
            };
             
            var command = new UpdateFileConfiguration(updatedConfiguration);
            GivenThereIsAConfiguration(configuration);
            GivenFiveServersAreRunning();
            GivenIHaveAnOcelotToken("/administration");
            WhenISendACommandIntoTheCluster(command);
            Thread.Sleep(5000);
            ThenTheCommandIsReplicatedToAllStateMachines(command);
        }

        [Fact(Skip = "still broken waiting for work in rafty")]
        public void should_persist_command_to_five_servers_when_using_administration_api()
        {
             var configuration = new FileConfiguration
             { 
             };

            var updatedConfiguration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>()
                {
                    new FileReRoute()
                    {
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "127.0.0.1",
                                Port = 80,
                            }
                        },
                        DownstreamScheme = "http",
                        DownstreamPathTemplate = "/geoffrey",
                        UpstreamHttpMethod = new List<string> { "get" },
                        UpstreamPathTemplate = "/"
                    },
                    new FileReRoute()
                    {
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "123.123.123",
                                Port = 443,
                            }
                        },
                        DownstreamScheme = "https",
                        DownstreamPathTemplate = "/blooper/{productId}",
                        UpstreamHttpMethod = new List<string> { "post" },
                        UpstreamPathTemplate = "/test"
                    }
                }
            };
             
            var command = new UpdateFileConfiguration(updatedConfiguration);
            GivenThereIsAConfiguration(configuration);
            GivenFiveServersAreRunning();
            GivenIHaveAnOcelotToken("/administration");
            GivenIHaveAddedATokenToMyRequest();
            WhenIPostOnTheApiGateway("/administration/configuration", updatedConfiguration);
            ThenTheCommandIsReplicatedToAllStateMachines(command);
        }

        private void WhenISendACommandIntoTheCluster(UpdateFileConfiguration command)
        {
            bool SendCommand()
            {
                try
                {
                    var p = _peers.Peers.First();
                    var json = JsonConvert.SerializeObject(command, new JsonSerializerSettings()
                    {
                        TypeNameHandling = TypeNameHandling.All
                    });
                    var httpContent = new StringContent(json);
                    httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    using (var httpClient = new HttpClient())
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token.AccessToken);
                        var response = httpClient.PostAsync($"{p.HostAndPort}/administration/raft/command", httpContent).GetAwaiter().GetResult();
                        response.EnsureSuccessStatusCode();
                        var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                        var errorResult = JsonConvert.DeserializeObject<ErrorResponse<UpdateFileConfiguration>>(content);

                        if (!string.IsNullOrEmpty(errorResult.Error))
                        {
                            return false;
                        }

                        var okResult = JsonConvert.DeserializeObject<OkResponse<UpdateFileConfiguration>>(content);

                        if (okResult.Command.Configuration.ReRoutes.Count == 2)
                        {
                            return true;
                        }
                    }

                    return false;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return false;
                }
            }

            var commandSent = WaitFor(20000).Until(() => SendCommand());
            commandSent.ShouldBeTrue();   
        }

        private void ThenTheCommandIsReplicatedToAllStateMachines(UpdateFileConfiguration expecteds)
        {            
            bool CommandCalledOnAllStateMachines()
            {
                try
                {
                    var passed = 0;
                    foreach (var peer in _peers.Peers)
                    {
                        var path = $"{peer.HostAndPort.Replace("/","").Replace(":","")}.db";
                        using(var connection = new SqliteConnection($"Data Source={path};"))
                        {
                            connection.Open();
                            var sql = @"select count(id) from logs";
                            using(var command = new SqliteCommand(sql, connection))
                            {
                                var index = Convert.ToInt32(command.ExecuteScalar());
                                index.ShouldBe(1);
                            }
                        }

                        _httpClientForAssertions.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token.AccessToken);
                        var result = _httpClientForAssertions.GetAsync($"{peer.HostAndPort}/administration/configuration").Result;
                        var json = result.Content.ReadAsStringAsync().Result;
                        var response = JsonConvert.DeserializeObject<FileConfiguration>(json, new JsonSerializerSettings{TypeNameHandling = TypeNameHandling.All});
                        response.GlobalConfiguration.RequestIdKey.ShouldBe(expecteds.Configuration.GlobalConfiguration.RequestIdKey);
                        response.GlobalConfiguration.ServiceDiscoveryProvider.Host.ShouldBe(expecteds.Configuration.GlobalConfiguration.ServiceDiscoveryProvider.Host);
                        response.GlobalConfiguration.ServiceDiscoveryProvider.Port.ShouldBe(expecteds.Configuration.GlobalConfiguration.ServiceDiscoveryProvider.Port);

                        for (var i = 0; i < response.ReRoutes.Count; i++)
                        {
                            for (var j = 0; j < response.ReRoutes[i].DownstreamHostAndPorts.Count; j++)
                            {
                                var res = response.ReRoutes[i].DownstreamHostAndPorts[j];
                                var expected = expecteds.Configuration.ReRoutes[i].DownstreamHostAndPorts[j];
                                res.Host.ShouldBe(expected.Host);
                                res.Port.ShouldBe(expected.Port);
                            }

                            response.ReRoutes[i].DownstreamPathTemplate.ShouldBe(expecteds.Configuration.ReRoutes[i].DownstreamPathTemplate);
                            response.ReRoutes[i].DownstreamScheme.ShouldBe(expecteds.Configuration.ReRoutes[i].DownstreamScheme);
                            response.ReRoutes[i].UpstreamPathTemplate.ShouldBe(expecteds.Configuration.ReRoutes[i].UpstreamPathTemplate);
                            response.ReRoutes[i].UpstreamHttpMethod.ShouldBe(expecteds.Configuration.ReRoutes[i].UpstreamHttpMethod);
                        }

                        passed++;
                    }

                    return passed == 5;
                }
                catch(Exception e)
                {
                    //_output.WriteLine($"{e.Message}, {e.StackTrace}");
                    Console.WriteLine(e);
                    return false;
                }
            }

            var commandOnAllStateMachines = WaitFor(20000).Until(() => CommandCalledOnAllStateMachines());
            commandOnAllStateMachines.ShouldBeTrue();   
        }

        private void WhenIPostOnTheApiGateway(string url, FileConfiguration updatedConfiguration)
        {
            bool SendCommand()
            {
                var json = JsonConvert.SerializeObject(updatedConfiguration);
                var content = new StringContent(json);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                _response = _httpClient.PostAsync(url, content).Result;
                var responseContent = _response.Content.ReadAsStringAsync().Result;

                if(responseContent == "There was a problem. This error message sucks raise an issue in GitHub.")
                {
                    return false;
                }

                if(string.IsNullOrEmpty(responseContent))
                {
                    return false;
                }

                return _response.IsSuccessStatusCode;
            }

            var commandSent = WaitFor(20000).Until(() => SendCommand());
            commandSent.ShouldBeTrue();  
        }

        private void GivenIHaveAddedATokenToMyRequest()
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token.AccessToken);
        }

        private void GivenIHaveAnOcelotToken(string adminPath)
        {
            bool AddToken()
            {
                try
                {
                    var tokenUrl = $"{adminPath}/connect/token";
                    var formData = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("client_id", "admin"),
                        new KeyValuePair<string, string>("client_secret", "secret"),
                        new KeyValuePair<string, string>("scope", "admin"),
                        new KeyValuePair<string, string>("grant_type", "client_credentials")
                    };
                    var content = new FormUrlEncodedContent(formData);

                    var response = _httpClient.PostAsync(tokenUrl, content).Result;
                    var responseContent = response.Content.ReadAsStringAsync().Result;
                    if(!response.IsSuccessStatusCode)
                    {
                        return false;
                    }

                    _token = JsonConvert.DeserializeObject<BearerToken>(responseContent);
                    var configPath = $"{adminPath}/.well-known/openid-configuration";
                    response = _httpClient.GetAsync(configPath).Result;
                    return response.IsSuccessStatusCode;
                }
                catch(Exception)
                {
                    return false;
                }
            }

            var addToken = WaitFor(20000).Until(() => AddToken());
            addToken.ShouldBeTrue();   
        }

        private void GivenThereIsAConfiguration(FileConfiguration fileConfiguration)
        {
            var configurationPath = $"{Directory.GetCurrentDirectory()}/ocelot.json";

            var jsonConfiguration = JsonConvert.SerializeObject(fileConfiguration);

            if (File.Exists(configurationPath))
            {
                File.Delete(configurationPath);
            }

            File.WriteAllText(configurationPath, jsonConfiguration);

            var text = File.ReadAllText(configurationPath);

            configurationPath = $"{AppContext.BaseDirectory}/ocelot.json";

            if (File.Exists(configurationPath))
            {
                File.Delete(configurationPath);
            }

            File.WriteAllText(configurationPath, jsonConfiguration);

            text = File.ReadAllText(configurationPath);
        }

        private void GivenAServerIsRunning(string url)
        {
            lock(_lock)
            {
                IWebHostBuilder webHostBuilder = new WebHostBuilder();
                webHostBuilder.UseUrls(url)
                    .UseKestrel()
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .ConfigureAppConfiguration((hostingContext, config) =>
                    {
                        config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
                        var env = hostingContext.HostingEnvironment;
                        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                        config.AddJsonFile("ocelot.json");
                        config.AddJsonFile("peers.json", optional: true, reloadOnChange: true);
                        #pragma warning disable CS0618
                        config.AddOcelotBaseUrl(url);
                        #pragma warning restore CS0618
                        config.AddEnvironmentVariables();
                    })
                    .ConfigureServices(x =>
                    {
                        x.AddSingleton(new NodeId(url));
                        x
                            .AddOcelot()
                            .AddAdministration("/administration", "secret")
                            .AddRafty();
                    })
                    .Configure(app =>
                    {
                        app.UseOcelot().Wait();
                    }); 

                var builder = webHostBuilder.Build();
                builder.Start();

                _webHostBuilders.Add(webHostBuilder);
                _builders.Add(builder);
            }
        }

        private void GivenFiveServersAreRunning()
        {
            var bytes = File.ReadAllText("peers.json");
            _peers = JsonConvert.DeserializeObject<FilePeers>(bytes);

            foreach (var peer in _peers.Peers)
            {
                File.Delete(peer.HostAndPort.Replace("/", "").Replace(":", ""));
                File.Delete($"{peer.HostAndPort.Replace("/", "").Replace(":", "")}.db");
                var thread = new Thread(() => GivenAServerIsRunning(peer.HostAndPort));
                thread.Start();
                _threads.Add(thread);
            }
        }

        public void Dispose()
        {
            foreach (var builder in _builders)
            {
                builder?.Dispose();
            }

            foreach (var peer in _peers.Peers)
            {
                try
                {
                    File.Delete(peer.HostAndPort.Replace("/", "").Replace(":", ""));
                    File.Delete($"{peer.HostAndPort.Replace("/", "").Replace(":", "")}.db");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}
