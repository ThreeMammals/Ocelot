using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Parser;
using Ocelot.Configuration.Validator;
using Ocelot.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Configuration
{
    public class FileConfigurationCreatorTests
    {
        private readonly Mock<IOptions<FileConfiguration>> _fileConfig;
        private readonly Mock<IConfigurationValidator> _validator;
        private Response<IOcelotConfiguration> _config;
        private FileConfiguration _fileConfiguration;
        private readonly Mock<IClaimToThingConfigurationParser> _configParser;
        private readonly Mock<ILogger<FileOcelotConfigurationCreator>> _logger;
        private readonly FileOcelotConfigurationCreator _ocelotConfigurationCreator;

        public FileConfigurationCreatorTests()
        {
            _logger = new Mock<ILogger<FileOcelotConfigurationCreator>>();
            _configParser = new Mock<IClaimToThingConfigurationParser>();
            _validator = new Mock<IConfigurationValidator>();
            _fileConfig = new Mock<IOptions<FileConfiguration>>();
            _ocelotConfigurationCreator = new FileOcelotConfigurationCreator( 
                _fileConfig.Object, _validator.Object, _configParser.Object, _logger.Object);
        }

        [Fact]
        public void should_use_reroute_case_sensitivity_value()
        {
            this.Given(x => x.GivenTheConfigIs(new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        UpstreamTemplate = "/api/products/{productId}",
                        DownstreamTemplate = "/products/{productId}",
                        UpstreamHttpMethod = "Get",
                        ReRouteIsCaseSensitive = false
                    }
                }
            }))
                .And(x => x.GivenTheConfigIsValid())
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheReRoutesAre(new List<ReRoute>
                {
                    new ReRouteBuilder()
                        .WithDownstreamTemplate("/products/{productId}")
                        .WithUpstreamTemplate("/api/products/{productId}")
                        .WithUpstreamHttpMethod("Get")
                        .WithUpstreamTemplatePattern("(?i)/api/products/.*$")
                        .Build()
                }))
                .BDDfy();
        }

        [Fact]
        public void should_set_upstream_template_pattern_to_ignore_case_sensitivity()
        {
            this.Given(x => x.GivenTheConfigIs(new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        UpstreamTemplate = "/api/products/{productId}",
                        DownstreamTemplate = "/products/{productId}",
                        UpstreamHttpMethod = "Get"
                    }
                }
            }))
                .And(x => x.GivenTheConfigIsValid())
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheReRoutesAre(new List<ReRoute>
                {
                    new ReRouteBuilder()
                        .WithDownstreamTemplate("/products/{productId}")
                        .WithUpstreamTemplate("/api/products/{productId}")
                        .WithUpstreamHttpMethod("Get")
                        .WithUpstreamTemplatePattern("(?i)/api/products/.*$")
                        .Build()
                }))
                .BDDfy();
        }

        [Fact]
        public void should_set_upstream_template_pattern_to_respect_case_sensitivity()
        {
            this.Given(x => x.GivenTheConfigIs(new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        UpstreamTemplate = "/api/products/{productId}",
                        DownstreamTemplate = "/products/{productId}",
                        UpstreamHttpMethod = "Get",
                        ReRouteIsCaseSensitive = true
                    }
                }
            }))
              .And(x => x.GivenTheConfigIsValid())
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
        public void should_set_global_request_id_key()
        {
            this.Given(x => x.GivenTheConfigIs(new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        UpstreamTemplate = "/api/products/{productId}",
                        DownstreamTemplate = "/products/{productId}",
                        UpstreamHttpMethod = "Get",
                        ReRouteIsCaseSensitive = true
                    }
                },
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    RequestIdKey = "blahhhh"
                }
            }))
                .And(x => x.GivenTheConfigIsValid())
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheReRoutesAre(new List<ReRoute>
                {
                    new ReRouteBuilder()
                        .WithDownstreamTemplate("/products/{productId}")
                        .WithUpstreamTemplate("/api/products/{productId}")
                        .WithUpstreamHttpMethod("Get")
                        .WithUpstreamTemplatePattern("/api/products/.*$")
                        .WithRequestIdKey("blahhhh")
                        .Build()
                }))
                .BDDfy();
        }

        [Fact]
        public void should_create_template_pattern_that_matches_anything_to_end_of_string()
        {
            this.Given(x => x.GivenTheConfigIs(new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        UpstreamTemplate = "/api/products/{productId}",
                        DownstreamTemplate = "/products/{productId}",
                        UpstreamHttpMethod = "Get",
                        ReRouteIsCaseSensitive = true
                    }
                }
            }))
                .And(x => x.GivenTheConfigIsValid())
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

            this.Given(x => x.GivenTheConfigIs(new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        UpstreamTemplate = "/api/products/{productId}",
                        DownstreamTemplate = "/products/{productId}",
                        UpstreamHttpMethod = "Get",
                        ReRouteIsCaseSensitive = true,
                        AuthenticationOptions = new FileAuthenticationOptions
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
                .And(x => x.GivenTheConfigIsValid())
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

            this.Given(x => x.GivenTheConfigIs(new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        UpstreamTemplate = "/api/products/{productId}",
                        DownstreamTemplate = "/products/{productId}",
                        UpstreamHttpMethod = "Get",
                        ReRouteIsCaseSensitive = true,
                        AuthenticationOptions = new FileAuthenticationOptions
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
                .And(x => x.GivenTheConfigIsValid())
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheReRoutesAre(expected))
                .And(x => x.ThenTheAuthenticationOptionsAre(expected))
                .BDDfy();
        }

        [Fact]
        public void should_create_template_pattern_that_matches_more_than_one_placeholder()
        {
            this.Given(x => x.GivenTheConfigIs(new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        UpstreamTemplate = "/api/products/{productId}/variants/{variantId}",
                        DownstreamTemplate = "/products/{productId}",
                        UpstreamHttpMethod = "Get",
                        ReRouteIsCaseSensitive = true
                    }
                }
            }))
                .And(x => x.GivenTheConfigIsValid())
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
            this.Given(x => x.GivenTheConfigIs(new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        UpstreamTemplate = "/api/products/{productId}/variants/{variantId}/",
                        DownstreamTemplate = "/products/{productId}",
                        UpstreamHttpMethod = "Get",
                        ReRouteIsCaseSensitive = true
                    }
                }
            }))
                .And(x => x.GivenTheConfigIsValid())
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
            this.Given(x => x.GivenTheConfigIs(new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        UpstreamTemplate = "/",
                        DownstreamTemplate = "/api/products/",
                        UpstreamHttpMethod = "Get",
                        ReRouteIsCaseSensitive = true
                    }
                }
            }))
                .And(x => x.GivenTheConfigIsValid())
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

        private void GivenTheConfigIsValid()
        {
            _validator
                .Setup(x => x.IsValid(It.IsAny<FileConfiguration>()))
                .Returns(new OkResponse<ConfigurationValidationResult>(new ConfigurationValidationResult(false)));
        }

        private void GivenTheConfigIs(FileConfiguration fileConfiguration)
        {
            _fileConfiguration = fileConfiguration;
            _fileConfig
                .Setup(x => x.Value)
                .Returns(_fileConfiguration);
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
