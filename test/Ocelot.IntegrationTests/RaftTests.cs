using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Ocelot.Configuration.File;
using Ocelot.Raft;
using Rafty.Concensus;
using Rafty.FiniteStateMachine;
using Rafty.Infrastructure;
using Shouldly;
using Xunit;
using static Rafty.Infrastructure.Wait;

namespace Ocelot.IntegrationTests
{
    public class Tests : IDisposable
    {
        private List<IWebHost> _builders;
        private List<IWebHostBuilder> _webHostBuilders;
        private List<Thread> _threads;
        private FilePeers _peers;
        private HttpClient _httpClient;
        private string _ocelotBaseUrl;
        private BearerToken _token;

        public Tests()
        {
            _httpClient = new HttpClient();
            _ocelotBaseUrl = "http://localhost:5000";
            _httpClient.BaseAddress = new Uri(_ocelotBaseUrl);
            _webHostBuilders = new List<IWebHostBuilder>();
            _builders = new List<IWebHost>();
            _threads = new List<Thread>();
        }
        public void Dispose()
        {
            foreach (var builder in _builders)
            {
                builder?.Dispose();
            }

            foreach (var peer in _peers.Peers)
            {
                File.Delete(peer.Id);
                File.Delete($"{peer.Id.ToString()}.db");
            }
        }

        [Fact]
        public void should_persist_command_to_five_servers()
        {
             var configuration = new FileConfiguration
             { 
                 GlobalConfiguration = new FileGlobalConfiguration
                 {
                     AdministrationPath = "/administration"
                 }
             };
             
            var command = new FakeCommand("WHATS UP DOC?");
            GivenThereIsAConfiguration(configuration);
            GivenFiveServersAreRunning();
            GivenALeaderIsElected();
            GivenIHaveAnOcelotToken("/administration");
            WhenISendACommandIntoTheCluster(command);
            ThenTheCommandIsReplicatedToAllStateMachines(command);
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

        private void GivenAServerIsRunning(string url, string id)
        {
            var guid = Guid.Parse(id);

            IWebHostBuilder webHostBuilder = new WebHostBuilder();
            webHostBuilder.UseUrls(url)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureServices(x =>
                {
                    x.AddSingleton(webHostBuilder);
                    x.AddSingleton(new NodeId(guid));
                })
                .UseStartup<RaftStartup>();

            var builder = webHostBuilder.Build();
            builder.Start();

            _webHostBuilders.Add(webHostBuilder);
            _builders.Add(builder);
        }

        private void GivenFiveServersAreRunning()
        {
            var bytes = File.ReadAllText("peers.json");
            _peers = JsonConvert.DeserializeObject<FilePeers>(bytes);

            foreach (var peer in _peers.Peers)
            {
                var thread = new Thread(() => GivenAServerIsRunning(peer.HostAndPort, peer.Id));
                thread.Start();
                _threads.Add(thread);
            }
        }

        private void GivenALeaderIsElected()
        {
            //dirty sleep to make sure we have a leader
            var stopwatch = Stopwatch.StartNew();
            while(stopwatch.ElapsedMilliseconds < 20000)
            {

            }
        }

        private void WhenISendACommandIntoTheCluster(FakeCommand command)
        {
            var p = _peers.Peers.First();
            var json = JsonConvert.SerializeObject(command,new JsonSerializerSettings() { 
                TypeNameHandling = TypeNameHandling.All
            });
            var httpContent = new StringContent(json);
            httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            using(var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token.AccessToken);
                var response = httpClient.PostAsync($"{p.HostAndPort}/administration/raft/command", httpContent).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
                var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                var result = JsonConvert.DeserializeObject<OkResponse<FakeCommand>>(content);
                result.Command.Value.ShouldBe(command.Value);
            }

            //dirty sleep to make sure command replicated...
            var stopwatch = Stopwatch.StartNew();
            while(stopwatch.ElapsedMilliseconds < 10000)
            {

            }
        }

        private void ThenTheCommandIsReplicatedToAllStateMachines(FakeCommand command)
        {
            //dirty sleep to give a chance to replicate...
            var stopwatch = Stopwatch.StartNew();
            while(stopwatch.ElapsedMilliseconds < 2000)
            {

            }
            
             bool CommandCalledOnAllStateMachines()
            {
                try
                {
                    var passed = 0;
                    foreach (var peer in _peers.Peers)
                    {
                        string fsmData;
                        fsmData = File.ReadAllText(peer.Id);
                        fsmData.ShouldNotBeNullOrEmpty();
                        var fakeCommand = JsonConvert.DeserializeObject<FakeCommand>(fsmData);
                        fakeCommand.Value.ShouldBe(command.Value);
                        passed++;
                    }

                    return passed == 5;
                }
                catch(Exception e)
                {
                    return false;
                }
            }

            var commandOnAllStateMachines = WaitFor(20000).Until(() => CommandCalledOnAllStateMachines());
            commandOnAllStateMachines.ShouldBeTrue();   
        }
    }
}
