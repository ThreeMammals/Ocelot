using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Moq;
using Ocelot.Library.Infrastructure.Builder;
using Ocelot.Library.Infrastructure.Configuration;
using Ocelot.Library.Infrastructure.Configuration.Yaml;
using Ocelot.Library.Infrastructure.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Configuration
{
    public class OcelotConfigurationTests
    {
        private readonly Mock<IOptions<YamlConfiguration>> _yamlConfig;
        private readonly Mock<IConfigurationValidator> _validator;
        private OcelotConfiguration _config;
        private YamlConfiguration _yamlConfiguration;

        public OcelotConfigurationTests()
        {
            _validator = new Mock<IConfigurationValidator>();
            _yamlConfig = new Mock<IOptions<YamlConfiguration>>();
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
                .When(x => x.WhenIInstanciateTheOcelotConfig())
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
                .When(x => x.WhenIInstanciateTheOcelotConfig())
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
                .When(x => x.WhenIInstanciateTheOcelotConfig())
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
                .When(x => x.WhenIInstanciateTheOcelotConfig())
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

        private void WhenIInstanciateTheOcelotConfig()
        {
            _config = new OcelotConfiguration(_yamlConfig.Object, _validator.Object);
        }

        private void ThenTheReRoutesAre(List<ReRoute> expectedReRoutes)
        {
            for (int i = 0; i < _config.ReRoutes.Count; i++)
            {
                var result = _config.ReRoutes[i];
                var expected = expectedReRoutes[i];

                result.DownstreamTemplate.ShouldBe(expected.DownstreamTemplate);
                result.UpstreamHttpMethod.ShouldBe(expected.UpstreamHttpMethod);
                result.UpstreamTemplate.ShouldBe(expected.UpstreamTemplate);
                result.UpstreamTemplatePattern.ShouldBe(expected.UpstreamTemplatePattern);
            }
        }
    }
}
