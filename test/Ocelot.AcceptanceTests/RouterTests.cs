using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Xunit;
using Ocelot.AcceptanceTests.Fake;
using Shouldly;

namespace Ocelot.AcceptanceTests
{
    public class RouterTests : IDisposable
    {
        private FakeService _fakeService;
        private readonly TestServer _server;
        private readonly HttpClient _client;

        public RouterTests()
        {
            _server = new TestServer(new WebHostBuilder()
                .UseStartup<Startup>());
            _client = _server.CreateClient();
            _fakeService = new FakeService();
        }

        [Fact]
        public void hello_world()
        {
            var response = _client.GetAsync("/").Result;
            response.EnsureSuccessStatusCode();

            var responseString = response.Content.ReadAsStringAsync().Result;
            responseString.ShouldBe("Hello from Tom");      
        }

        [Fact]
        public async Task can_route_request()
        {            
            _fakeService.Start("http://localhost:5001");

            // Act
            var response = await _client.GetAsync("/");
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            responseString.ShouldBe("Hello from Laura");
        }

        public void Dispose()
        {               
            _fakeService.Stop();
            _client.Dispose();
            _server.Dispose();
        }
    }
}
