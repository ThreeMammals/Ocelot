using System;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Xunit;
using Ocelot.AcceptanceTests.Fake;
using Shouldly;

namespace Ocelot.AcceptanceTests
{
    using System.Net;

    public class RouterTests : IDisposable
    {
        private readonly FakeService _fakeService;
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private HttpResponseMessage _response;

        public RouterTests()
        {
            _server = new TestServer(new WebHostBuilder()
                .UseStartup<Startup>());

            _client = _server.CreateClient();

            _fakeService = new FakeService();
        }

        [Fact]
        public void should_return_response_404()
        {
            WhenIRequestTheUrl("/");
            ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound);
        }

        private void WhenIRequestTheUrl(string url)
        {
            _response = _client.GetAsync("/").Result;
        }

        private void ThenTheStatusCodeShouldBe(HttpStatusCode expectedHttpStatusCode)
        {
            _response.StatusCode.ShouldBe(expectedHttpStatusCode);
        }

        public void Dispose()
        {               
            _fakeService.Stop();
            _client.Dispose();
            _server.Dispose();
        }
    }
}
