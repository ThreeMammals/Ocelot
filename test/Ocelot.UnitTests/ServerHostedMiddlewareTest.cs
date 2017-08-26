namespace Ocelot.UnitTests
{
    using System;
    using System.IO;
    using System.Net.Http;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.AspNetCore.Builder;
    using Moq;
    using Ocelot.Infrastructure.RequestData;

    public abstract class ServerHostedMiddlewareTest : IDisposable
    {
        protected TestServer Server { get; private set; }
        protected HttpClient Client { get; private set; }
        protected string Url { get; private set; }
        protected HttpResponseMessage ResponseMessage { get; private set; }
        protected Mock<IRequestScopedDataRepository> ScopedRepository { get; private set; }

        public ServerHostedMiddlewareTest()
        {
            Url = "http://localhost:51879";
            ScopedRepository = new Mock<IRequestScopedDataRepository>();
        }

        protected virtual void GivenTheTestServerIsConfigured()
        {
            var builder = new WebHostBuilder()
              .ConfigureServices(x => GivenTheTestServerServicesAreConfigured(x))
              .UseUrls(Url)
              .UseKestrel()
              .UseContentRoot(Directory.GetCurrentDirectory())
              .UseIISIntegration()
              .Configure(app => GivenTheTestServerPipelineIsConfigured(app));

            Server = new TestServer(builder);
            Client = Server.CreateClient();
        }

        protected virtual void GivenTheTestServerServicesAreConfigured(IServiceCollection services)
        {
            // override this in your test fixture to set up service dependencies
        }

        protected virtual void GivenTheTestServerPipelineIsConfigured(IApplicationBuilder app)
        {
            // override this in your test fixture to set up the test server pipeline
        }

        protected void WhenICallTheMiddleware()
        {
            ResponseMessage = Client.GetAsync(Url).Result;
        }

        public void Dispose()
        {
            Client.Dispose();
            Server.Dispose();
        }
    }
}
