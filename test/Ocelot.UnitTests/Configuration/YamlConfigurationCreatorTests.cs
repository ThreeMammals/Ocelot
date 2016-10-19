using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.Parser;
using Ocelot.Configuration.Validator;
using Ocelot.Configuration.Yaml;
using Ocelot.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Configuration
{
    public class YamlConfigurationCreatorTests
    {
        private readonly Mock<IOptions<YamlConfiguration>> _yamlConfig;
        private readonly Mock<IConfigurationValidator> _validator;
        private Response<IOcelotConfiguration> _config;
        private YamlConfiguration _yamlConfiguration;
        private readonly Mock<IClaimToThingConfigurationParser> _configParser;
        private readonly Mock<ILogger<YamlOcelotConfigurationCreator>> _logger;
        private readonly YamlOcelotConfigurationCreator _ocelotConfigurationCreator;

        public YamlConfigurationCreatorTests()
        {
            _logger = new Mock<ILogger<YamlOcelotConfigurationCreator>>();
            _configParser = new Mock<IClaimToThingConfigurationParser>();
            _validator = new Mock<IConfigurationValidator>();
            _yamlConfig = new Mock<IOptions<YamlConfiguration>>();
            _ocelotConfigurationCreator = new YamlOcelotConfigurationCreator( 
                _yamlConfig.Object, _validator.Object, _configParser.Object, _logger.Object);
        }

        [Fact]
        public void should_create_template_pattern_that_matches_anything_to_end_of_string()
        {
            this.Given(x => x.GivenTheYamlConfigIs(new YamlConfiguration
            {
                ReRoutes = new List<YamlReRoute>
                {
                    new YamlReRoute
                    {
                        UpstreamTemplate = "/api/products/{productId}",
                        DownstreamTemplate = "/products/{productId}",
                        UpstreamHttpMethod = "Get"
                    }
                }
            }))
                .And(x => x.GivenTheYamlConfigIsValid())
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheReRoutesAre(new List<ReRoute>
                {
                    new ReRouteBuilder()
                        .WithDownstreamTemplate("/products/{productId}")
                        .WithUpstreamTemplate("/api/products/{productId}")
                        .WithUpstreamHttpMethod("Get")
                        .WithUpstreamTemplatePattern("/api/products/.*$")
                        .Build()
                }))
                .BDDfy();
        }

        [Fact]
        public void should_create_with_headers_to_extract()
        {
            var expected = new List<ReRoute>
            {
                new ReRouteBuilder()
                    .WithDownstreamTemplate("/products/{productId}")
                    .WithUpstreamTemplate("/api/products/{productId}")
                    .WithUpstreamHttpMethod("Get")
                    .WithUpstreamTemplatePattern("/api/products/.*$")
                    .WithAuthenticationProvider("IdentityServer")
                    .WithAuthenticationProviderUrl("http://localhost:51888")
                    .WithRequireHttps(false)
                    .WithScopeSecret("secret")
                    .WithAuthenticationProviderScopeName("api")
                    .WithClaimsToHeaders(new List<ClaimToThing>
                    {
                        new ClaimToThing("CustomerId", "CustomerId", "", 0),
                    })
                    .Build()
            };

            this.Given(x => x.GivenTheYamlConfigIs(new YamlConfiguration
            {
                ReRoutes = new List<YamlReRoute>
                {
                    new YamlReRoute
                    {
                        UpstreamTemplate = "/api/products/{productId}",
                        DownstreamTemplate = "/products/{productId}",
                        UpstreamHttpMethod = "Get",
                        AuthenticationOptions = new YamlAuthenticationOptions
                            {
                                AdditionalScopes =  new List<string>(),
                                Provider = "IdentityServer",
                                ProviderRootUrl = "http://localhost:51888",
                                RequireHttps = false,
                                ScopeName = "api",
                                ScopeSecret = "secret"
                            },
                        AddHeadersToRequest =
                        {
                            {"CustomerId", "Claims[CustomerId] > value"},
                        }
                    }
                }
            }))
                .And(x => x.GivenTheYamlConfigIsValid())
                .And(x => x.GivenTheConfigHeaderExtractorReturns(new ClaimToThing("CustomerId", "CustomerId", "", 0)))
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheReRoutesAre(expected))
                .And(x => x.ThenTheAuthenticationOptionsAre(expected))
                .BDDfy();
        }

        private void GivenTheConfigHeaderExtractorReturns(ClaimToThing expected)
        {
            _configParser
                .Setup(x => x.Extract(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new OkResponse<ClaimToThing>(expected));
        }

        [Fact]
        public void should_create_with_authentication_properties()
        {
            var expected = new List<ReRoute>
            {
                new ReRouteBuilder()
                    .WithDownstreamTemplate("/products/{productId}")
                    .WithUpstreamTemplate("/api/products/{productId}")
                    .WithUpstreamHttpMethod("Get")
                    .WithUpstreamTemplatePattern("/api/products/.*$")
                    .WithAuthenticationProvider("IdentityServer")
                    .WithAuthenticationProviderUrl("http://localhost:51888")
                    .WithRequireHttps(false)
                    .WithScopeSecret("secret")
                    .WithAuthenticationProviderScopeName("api")
                    .Build()
            };

            this.Given(x => x.GivenTheYamlConfigIs(new YamlConfiguration
            {
                ReRoutes = new List<YamlReRoute>
                {
                    new YamlReRoute
                    {
                        UpstreamTemplate = "/api/products/{productId}",
                        DownstreamTemplate = "/products/{productId}",
                        UpstreamHttpMethod = "Get",
                        AuthenticationOptions = new YamlAuthenticationOptions
                            {
                                AdditionalScopes =  new List<string>(),
                                Provider = "IdentityServer",
                                ProviderRootUrl = "http://localhost:51888",
                                RequireHttps = false,
                                ScopeName = "api",
                                ScopeSecret = "secret"
                            }
                    }
                }
            }))
                .And(x => x.GivenTheYamlConfigIsValid())
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheReRoutesAre(expected))
                .And(x => x.ThenTheAuthenticationOptionsAre(expected))
                .BDDfy();
        }

        [Fact]
        public void should_create_template_pattern_that_matches_more_than_one_placeholder()
        {
            this.Given(x => x.GivenTheYamlConfigIs(new YamlConfiguration
            {
                ReRoutes = new List<YamlReRoute>
                {
                    new YamlReRoute
                    {
                        UpstreamTemplate = "/api/products/{productId}/variants/{variantId}",
                        DownstreamTemplate = "/products/{productId}",
                        UpstreamHttpMethod = "Get"
                    }
                }
            }))
                .And(x => x.GivenTheYamlConfigIsValid())
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheReRoutesAre(new List<ReRoute>
                {
                    new ReRouteBuilder()
                        .WithDownstreamTemplate("/products/{productId}")
                        .WithUpstreamTemplate("/api/products/{productId}/variants/{variantId}")
                        .WithUpstreamHttpMethod("Get")
                        .WithUpstreamTemplatePattern("/api/products/.*/variants/.*$")
                        .Build()
                }))
                .BDDfy();
        }

        [Fact]
        public void should_create_template_pattern_that_matches_more_than_one_placeholder_with_trailing_slash()
        {
            this.Given(x => x.GivenTheYamlConfigIs(new YamlConfiguration
            {
                ReRoutes = new List<YamlReRoute>
                {
                    new YamlReRoute
                    {
                        UpstreamTemplate = "/api/products/{productId}/variants/{variantId}/",
                        DownstreamTemplate = "/products/{productId}",
                        UpstreamHttpMethod = "Get"
                    }
                }
            }))
                .And(x => x.GivenTheYamlConfigIsValid())
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheReRoutesAre(new List<ReRoute>
                {
                    new ReRouteBuilder()
                        .WithDownstreamTemplate("/products/{productId}")
                        .WithUpstreamTemplate("/api/products/{productId}/variants/{variantId}/")
                        .WithUpstreamHttpMethod("Get")
                        .WithUpstreamTemplatePattern("/api/products/.*/variants/.*/$")
                        .Build()
                }))
                .BDDfy();
        }

        [Fact]
        public void should_create_template_pattern_that_matches_to_end_of_string()
        {
            this.Given(x => x.GivenTheYamlConfigIs(new YamlConfiguration
            {
                ReRoutes = new List<YamlReRoute>
                {
                    new YamlReRoute
                    {
                        UpstreamTemplate = "/",
                        DownstreamTemplate = "/api/products/",
                        UpstreamHttpMethod = "Get"
                    }
                }
            }))
                .And(x => x.GivenTheYamlConfigIsValid())
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheReRoutesAre(new List<ReRoute>
                {
                    new ReRouteBuilder()
                        .WithDownstreamTemplate("/api/products/")
                        .WithUpstreamTemplate("/")
                        .WithUpstreamHttpMethod("Get")
                        .WithUpstreamTemplatePattern("/$")
                        .Build()
                }))
                .BDDfy();
        }

        private void GivenTheYamlConfigIsValid()
        {
            _validator
                .Setup(x => x.IsValid(It.IsAny<YamlConfiguration>()))
                .Returns(new OkResponse<ConfigurationValidationResult>(new ConfigurationValidationResult(false)));
        }

        private void GivenTheYamlConfigIs(YamlConfiguration yamlConfiguration)
        {
            _yamlConfiguration = yamlConfiguration;
            _yamlConfig
                .Setup(x => x.Value)
                .Returns(_yamlConfiguration);
        }

        private void WhenICreateTheConfig()
        {
            _config = _ocelotConfigurationCreator.Create();
        }

        private void ThenTheReRoutesAre(List<ReRoute> expectedReRoutes)
        {
            for (int i = 0; i < _config.Data.ReRoutes.Count; i++)
            {
                var result = _config.Data.ReRoutes[i];
                var expected = expectedReRoutes[i];

                result.DownstreamTemplate.ShouldBe(expected.DownstreamTemplate);
                result.UpstreamHttpMethod.ShouldBe(expected.UpstreamHttpMethod);
                result.UpstreamTemplate.ShouldBe(expected.UpstreamTemplate);
                result.UpstreamTemplatePattern.ShouldBe(expected.UpstreamTemplatePattern);
            }
        }

        private void ThenTheAuthenticationOptionsAre(List<ReRoute> expectedReRoutes)
        {
            for (int i = 0; i < _config.Data.ReRoutes.Count; i++)
            {
                var result = _config.Data.ReRoutes[i].AuthenticationOptions;
                var expected = expectedReRoutes[i].AuthenticationOptions;

                result.AdditionalScopes.ShouldBe(expected.AdditionalScopes);
                result.Provider.ShouldBe(expected.Provider);
                result.ProviderRootUrl.ShouldBe(expected.ProviderRootUrl);
                result.RequireHttps.ShouldBe(expected.RequireHttps);
                result.ScopeName.ShouldBe(expected.ScopeName);
                result.ScopeSecret.ShouldBe(expected.ScopeSecret);

            }
        }
    }
}
