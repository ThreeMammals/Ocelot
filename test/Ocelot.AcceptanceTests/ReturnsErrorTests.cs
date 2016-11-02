using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Ocelot.Configuration.File;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.AcceptanceTests
{
    public class ReturnsErrorTests : IDisposable
    {
        private IWebHost _servicebuilder;
        private readonly Steps _steps;

        public ReturnsErrorTests()
        {
            _steps = new Steps();
        }

        [Fact]
        public void should_return_response_200_and_foward_claim_as_header()
        {
            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamTemplate = "http://localhost:53876/",
                            UpstreamTemplate = "/",
                            UpstreamHttpMethod = "Get"
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:53876"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError))
                .BDDfy();
        }

        private void GivenThereIsAServiceRunningOn(string url)
        {
            _servicebuilder = new WebHostBuilder()
                .UseUrls(url)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseUrls(url)
                .Configure(app =>
                {
                    app.Run(context =>
                    {
                        throw new Exception("BLAMMMM");
                    });
                })
                .Build();

            _servicebuilder.Start();
        }

        public void Dispose()
        {
            _servicebuilder?.Dispose();
            _steps.Dispose();
        }
    }
}
