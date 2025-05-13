using CacheManager.Core;
using Consul;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Ocelot.AcceptanceTests.Caching;
using Ocelot.Cache.CacheManager;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Provider.Consul;
using System.Text;

namespace Ocelot.AcceptanceTests.Configuration;

public sealed class ConfigurationInConsulTests : Steps
{
    private FileConfiguration _config;
    private readonly List<ServiceEntry> _consulServices;

    public ConfigurationInConsulTests()
    {
        _consulServices = new List<ServiceEntry>();
    }

    [Fact]
    public void Should_return_response_200_with_simple_url_when_using_jsonserialized_cache()
    {
        var consulPort = PortFinder.GetRandomPort();
        var servicePort = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(servicePort);
        var configuration = GivenConfiguration(route);
        configuration.GlobalConfiguration.ServiceDiscoveryProvider = new()
        {
            Scheme = Uri.UriSchemeHttp,
            Host = "localhost",
            Port = consulPort,
        };
        this.Given(x => GivenThereIsAFakeConsulServiceDiscoveryProvider(consulPort, string.Empty))
            .And(x => x.GivenThereIsAServiceRunningOn(servicePort, string.Empty, HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => x.GivenOcelotIsRunningUsingConsulToStoreConfigAndJsonSerializedCache())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    private void GivenOcelotIsRunningUsingConsulToStoreConfigAndJsonSerializedCache()
    {
        static void WithConsulToStoreConfigAndJsonSerializedCache(IServiceCollection services) => services
            .AddOcelot()
            .AddCacheManager(x => x

                //.WithMicrosoftLogging(_ => { /*log.AddConsole(LogLevel.Debug);*/ })
                .WithJsonSerializer()
                .WithHandle(typeof(InMemoryJsonHandle<>)))
            .AddConsul()
            .AddConfigStoredInConsul();
        GivenOcelotIsRunning(WithConsulToStoreConfigAndJsonSerializedCache);
    }

    private void GivenThereIsAFakeConsulServiceDiscoveryProvider(int consulPort, string serviceName)
    {
        handler.GivenThereIsAServiceRunningOn(consulPort, async context =>
        {
            if (context.Request.Method.Equals(HttpMethods.Get, StringComparison.InvariantCultureIgnoreCase) &&
                context.Request.Path.Value == "/v1/kv/InternalConfiguration")
            {
                var json = JsonConvert.SerializeObject(_config);
                var bytes = Encoding.UTF8.GetBytes(json);
                var base64 = Convert.ToBase64String(bytes);
                var kvp = new FakeConsulGetResponse(base64);

                // await context.Response.WriteJsonAsync(new[] { kvp });
            }
            else if (context.Request.Method.Equals(HttpMethods.Put, StringComparison.InvariantCultureIgnoreCase) &&
                context.Request.Path.Value == "/v1/kv/InternalConfiguration")
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
                //await context.Response.WriteJsonAsync(_consulServices);
            }
        });
    }

    public class FakeConsulGetResponse
    {
        public FakeConsulGetResponse(string value) => Value = value;

        public int CreateIndex => 100;
        public int ModifyIndex => 200;
        public int LockIndex => 200;
        public string Key => "InternalConfiguration";
        public int Flags => 0;
        public string Value { get; }
        public string Session => "adf4238a-882b-9ddc-4a9d-5b6758e4159e";
    }

    private void GivenThereIsAServiceRunningOn(int port, string basePath, HttpStatusCode statusCode, string responseBody)
    {
        handler.GivenThereIsAServiceRunningOn(port, basePath, context =>
        {
            context.Response.StatusCode = (int)statusCode;
            return context.Response.WriteAsync(responseBody);
        });
    }
}
