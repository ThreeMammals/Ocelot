using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using Ocelot.Library.Infrastructure.Configuration;
using Ocelot.Library.Infrastructure.DownstreamRouteFinder;
using Ocelot.Library.Infrastructure.Responses;
using Ocelot.Library.Infrastructure.UrlMatcher;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests
{
    public class DownstreamRouteFinderTests
    {
        private readonly IDownstreamRouteFinder _downstreamRouteFinder;
        private readonly Mock<IOptions<Configuration>> _mockConfig;
        private readonly Mock<IUrlPathToUrlTemplateMatcher> _mockMatcher;
        private string _upstreamUrlPath;
        private Response<DownstreamRoute> _result;
        private Response<DownstreamRoute> _response;
        private Configuration _configuration;
        private UrlMatch _match;

        public DownstreamRouteFinderTests()
        {
            _mockConfig = new Mock<IOptions<Configuration>>();
            _mockMatcher = new Mock<IUrlPathToUrlTemplateMatcher>();
            _downstreamRouteFinder = new DownstreamRouteFinder(_mockConfig.Object, _mockMatcher.Object);
        }

        [Fact]
        public void should_return_route()
        {
            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("someUpstreamPath"))
                .And(x => x.GivenTheConfigurationIs(new Configuration {
                    ReRoutes = new List<ReRoute>
                    {
                        new ReRoute()
                        {
                            UpstreamTemplate = "someUpstreamPath",
                            DownstreamTemplate = "someDownstreamPath"
                        }
                    }
                }))
                .And(x => x.GivenTheUrlMatcherReturns(new UrlMatch(true, new List<TemplateVariableNameAndValue>(), "someDownstreamPath")))
                .When(x => x.WhenICallTheFinder())
                .Then(
                    x => x.ThenTheFollowingIsReturned(new DownstreamRoute(new List<TemplateVariableNameAndValue>(), "someDownstreamPath")))
                .And(x => x.ThenTheUrlMatcherIsCalledCorrectly())
                .BDDfy();
        }

        [Fact]
        public void should_not_return_route()
        {
            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("somePath"))
                 .And(x => x.GivenTheConfigurationIs(new Configuration
                 {
                     ReRoutes = new List<ReRoute>
                     {
                        new ReRoute()
                        {
                            UpstreamTemplate = "somePath",
                            DownstreamTemplate = "somPath"
                        }
                     }
                 }))
                 .And(x => x.GivenTheUrlMatcherReturns(new UrlMatch(false, new List<TemplateVariableNameAndValue>(), null)))
                 .When(x => x.WhenICallTheFinder())
                 .Then(
                     x => x.ThenAnErrorResponseIsReturned())
                 .And(x => x.ThenTheUrlMatcherIsCalledCorrectly())
                 .BDDfy();
        }

        private void ThenAnErrorResponseIsReturned()
        {
            _result.IsError.ShouldBeTrue();
        }

        private void ThenTheUrlMatcherIsCalledCorrectly()
        {
            _mockMatcher
                .Verify(x => x.Match(_upstreamUrlPath, _configuration.ReRoutes[0].UpstreamTemplate), Times.Once);
        }

        private void GivenTheUrlMatcherReturns(UrlMatch match)
        {
            _match = match;
            _mockMatcher
                .Setup(x => x.Match(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(_match);
        }

        private void GivenTheConfigurationIs(Configuration configuration)
        {
            _configuration = configuration;
            _mockConfig
                .Setup(x => x.Value)
                .Returns(_configuration);
        }

        private void GivenThereIsAnUpstreamUrlPath(string upstreamUrlPath)
        {
            _upstreamUrlPath = upstreamUrlPath;
        }

        private void WhenICallTheFinder()
        {
            _result = _downstreamRouteFinder.FindDownstreamRoute(_upstreamUrlPath);
        }

        private void ThenTheFollowingIsReturned(DownstreamRoute expected)
        {
            _result.Data.DownstreamUrlTemplate.ShouldBe(expected.DownstreamUrlTemplate);

            for (int i = 0; i < _result.Data.TemplateVariableNameAndValues.Count; i++)
            {
                _result.Data.TemplateVariableNameAndValues[i].TemplateVariableName.ShouldBe(
                    expected.TemplateVariableNameAndValues[i].TemplateVariableName);

                _result.Data.TemplateVariableNameAndValues[i].TemplateVariableValue.ShouldBe(
                    expected.TemplateVariableNameAndValues[i].TemplateVariableValue);
            }
            
            _result.IsError.ShouldBeFalse();
        }
    }
}
