using Microsoft.Extensions.Configuration;
using Ocelot.DependencyInjection;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.DependencyInjection
{
    using System.Collections.Generic;
    using System.IO;
    using Newtonsoft.Json;
    using Ocelot.Configuration.File;

    public class ConfigurationBuilderExtensionsTests
    {
        private IConfigurationRoot _configuration;
        private string _result;

        [Fact]
        public void should_add_base_url_to_config()
        {
            this.Given(x => GivenTheBaseUrl("test"))
                .When(x => WhenIGet("BaseUrl"))
                .Then(x => ThenTheResultIs("test"))
                .BDDfy();
        }

        [Fact]
        public void should_merge_files()
        {
            var globalConfig = new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    BaseUrl = "BaseUrl",
                    RateLimitOptions = new FileRateLimitOptions
                    {
                        HttpStatusCode = 500,
                        ClientIdHeader = "ClientIdHeader",
                        DisableRateLimitHeaders = true,
                        QuotaExceededMessage = "QuotaExceededMessage",
                        RateLimitCounterPrefix = "RateLimitCounterPrefix"
                    }
                }
            };

            var reRoute = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamScheme = "DownstreamScheme",
                        Key = "Key",
                        UpstreamHost = "UpstreamHost",
                        UpstreamHttpMethod = new List<string>
                        {
                            "UpstreamHttpMethod"
                        },
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "Host",
                                Port = 80
                            }
                        }
                    }
                }
            };

            var globalJson = JsonConvert.SerializeObject(globalConfig);
            //File.WriteAllText("ocelot.global.json", globalJson);

            var reRouteJson = JsonConvert.SerializeObject(reRoute);
            //File.WriteAllText("ocelot.reRoute.json", reRouteJson);

            IConfigurationBuilder builder = new ConfigurationBuilder();
            //builder.AddOcelot();
            
        }

        private void GivenTheBaseUrl(string baseUrl)
        {
            #pragma warning disable CS0618
            var builder = new ConfigurationBuilder()
                .AddOcelotBaseUrl(baseUrl);
            #pragma warning restore CS0618
            _configuration = builder.Build();
        }

        private void WhenIGet(string key)
        {
            _result = _configuration.GetValue("BaseUrl", "");
        }

        private void ThenTheResultIs(string expected)
        {
            _result.ShouldBe(expected);
        }
    }
}
