using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.AcceptanceTests
{
    public class AdministrationTests : IDisposable
    {
        private IWebHost _builder;
        private readonly Steps _steps;

        public AdministrationTests()
        {
            _steps = new Steps();
        }

        [Fact]
        public void should_return_response_200_with_call_re_routes_controller()
        {
            var configuration = new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    AdministrationPath = "/administration"
                }
            };

            this.Given(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/administration/configuration"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .BDDfy();
        }

        [Fact]
        public void should_return_file_configuration()
        {
            var configuration = new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    AdministrationPath = "/administration"
                },
                ReRoutes  = new List<FileReRoute>()
                {
                    new FileReRoute()
                    {
                        DownstreamHost = "localhost",
                        DownstreamPort = 80,
                        DownstreamScheme = "https",
                        DownstreamPathTemplate = "/",
                        UpstreamHttpMethod = "get",
                        UpstreamPathTemplate = "/"
                    },
                     new FileReRoute()
                    {
                        DownstreamHost = "localhost",
                        DownstreamPort = 80,
                        DownstreamScheme = "https",
                        DownstreamPathTemplate = "/",
                        UpstreamHttpMethod = "get",
                        UpstreamPathTemplate = "/test"
                    }
                }
            };

            this.Given(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/administration/configuration"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseShouldBe(configuration))
                .BDDfy();
        }

        public void Dispose()
        {
            _builder?.Dispose();
            _steps.Dispose();
        }
    }
}
