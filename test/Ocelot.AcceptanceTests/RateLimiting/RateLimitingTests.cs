#if NET7_0_OR_GREATER
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.RateLimiting;
#endif
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;

namespace Ocelot.AcceptanceTests.RateLimiting
{
    public class RateLimitingTests: Steps
    {
        private const string _rateLimitPolicyName = "RateLimitPolicy";
        private const int _rateLimitLimit = 3;
        private const int _rateLimitWindow = 1;
        private const string _quotaExceededMessage = "woah!";
        private readonly ServiceHandler _serviceHandler = new ();
        
        public override void Dispose()
        {
            _serviceHandler.Dispose();
            base.Dispose();
        }

#if NET7_0_OR_GREATER
        [Fact]
        [Trait("Feat", "2138")]
        public void Should_RateLimit()
        {
            var port = PortFinder.GetRandomPort();
            var route = GivenRoute(port, _rateLimitPolicyName);
            var configuration = GivenConfigurationWithRateLimitOptions(route);

            var ocelotServices = GivenOcelotServices();
            
            this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), "/"))
                .And(x => GivenThereIsAConfiguration(configuration))
                .And(x => GivenOcelotIsRunningWithServices(ocelotServices))
                .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit("/", 1))
                .Then(x => ThenTheStatusCodeShouldBe((int)HttpStatusCode.OK))
                .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit("/", 2))
                .Then(x => ThenTheStatusCodeShouldBe((int)HttpStatusCode.OK))
                .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit("/", 1))
                .Then(x => ThenTheStatusCodeShouldBe((int)HttpStatusCode.TooManyRequests))
                .Then(x => x.ThenTheResponseBodyShouldBe(_quotaExceededMessage))
                .And(x => GivenIWait((1000 * _rateLimitWindow) + 100))
                .When(x => x.WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit("/", 1))
                .Then(x => ThenTheStatusCodeShouldBe((int)HttpStatusCode.OK))
                .BDDfy();
        }
        
        private FileRoute GivenRoute(int port, string rateLimitPolicyName) => new()
        {
            DownstreamHostAndPorts = new() { new("localhost", port) },
            DownstreamPathTemplate = "/",
            DownstreamScheme = Uri.UriSchemeHttp,
            UpstreamHttpMethod = new() { HttpMethods.Get },
            UpstreamPathTemplate = "/",
            RateLimitOptions = new FileRateLimitRule()
            {
                EnableRateLimiting = true,
                Policy = rateLimitPolicyName,
            },
        };
        
        private static FileConfiguration GivenConfigurationWithRateLimitOptions(params FileRoute[] routes)
        {
            var config = GivenConfiguration(routes);
            config.GlobalConfiguration = new()
            {
                RateLimitOptions = new()
                {
                    QuotaExceededMessage = _quotaExceededMessage,
                    HttpStatusCode = (int)HttpStatusCode.TooManyRequests,
                },
            };
            return config;
        }

        private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath)
        {
            _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, basePath, context =>
            {
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.WriteAsync("test response");
                return Task.CompletedTask;
            });
        }

        private Action<IServiceCollection> GivenOcelotServices() => services =>
        {
            services.AddOcelot();
            services.AddRateLimiter(op =>
            {
                op.AddFixedWindowLimiter(_rateLimitPolicyName, options =>
                {
                    options.PermitLimit = _rateLimitLimit;
                    options.Window = TimeSpan.FromSeconds(_rateLimitWindow);
                    options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    options.QueueLimit = 0;
                });
            });
        };
#endif
    }
}
