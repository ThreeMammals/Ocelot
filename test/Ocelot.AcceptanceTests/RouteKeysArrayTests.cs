using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.AcceptanceTests
{
    public class RouteKeysArrayTests : IDisposable
    {
        private readonly Steps _steps;
        private readonly ServiceHandler _serviceHandler;
        private readonly int _portUser;
        private readonly int _portProduct;

        public RouteKeysArrayTests()
        {
            _steps = new Steps();
            _serviceHandler = new ServiceHandler();
            _portUser = PortFinder.GetRandomPort();
            _portProduct = PortFinder.GetRandomPort();
        }

        [Fact]
        public void should_match_downstream_routes_using_route_keys_array()
        {
            _serviceHandler.GivenThereIsAServiceRunningOn(
                $"http://localhost:{_portUser}",
                "/user",
                async ctx =>
                {
                    ctx.Response.StatusCode = 200;
                    await ctx.Response.WriteAsync("OK-user");
                });

            _serviceHandler.GivenThereIsAServiceRunningOn(
                $"http://localhost:{_portProduct}",
                "/product",
                async ctx =>
                {
                    ctx.Response.StatusCode = 200;
                    await ctx.Response.WriteAsync("OK-product");
                });

            var userRoute = new FileRoute
            {
                DownstreamPathTemplate = "/user",
                DownstreamScheme = "http",
                DownstreamHostAndPorts = new List<FileHostAndPort> {
                    new FileHostAndPort("localhost", _portUser)
                },
                UpstreamPathTemplate = "/user",
                UpstreamHttpMethod = new List<string> { "Get" },
                Key = "User"
            };

            var productRoute = new FileRoute
            {
                DownstreamPathTemplate = "/product",
                DownstreamScheme = "http",
                DownstreamHostAndPorts = new List<FileHostAndPort> {
                    new FileHostAndPort("localhost", _portProduct)
                },
                UpstreamPathTemplate = "/product",
                UpstreamHttpMethod = new List<string> { "Get" },
                Key = "Product"
            };

            var aggregate = new FileAggregateRoute
            {
                RouteKeys = new HashSet<string> { "User", "Product" },
                UpstreamPathTemplate = "/composite",
                UpstreamHttpMethod = new List<string> { "Get" }
            };

            var config = new FileConfiguration
            {
                Routes = new List<FileRoute> { userRoute, productRoute },
                Aggregates = new List<FileAggregateRoute> { aggregate }
            };

            this.Given(_ => _steps.GivenThereIsAConfiguration(config))
                .And(_ => _steps.WhenIGetUrlOnTheApiGateway("/composite"))
                .Then(_ => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .BDDfy();
        }

        public void Dispose() => _serviceHandler.Dispose();
    }
}