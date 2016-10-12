using System.Collections.Generic;
using Moq;
using Ocelot.Library.Infrastructure.Configuration;
using Ocelot.Library.Infrastructure.DownstreamRouteFinder;
using Ocelot.Library.Infrastructure.Responses;
using Ocelot.Library.Infrastructure.UrlMatcher;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.DownstreamRouteFinder
{
    public class DownstreamRouteFinderTests
    {
        private readonly IDownstreamRouteFinder _downstreamRouteFinder;
        private readonly Mock<IOcelotConfiguration> _mockConfig;
        private readonly Mock<IUrlPathToUrlTemplateMatcher> _mockMatcher;
        private readonly Mock<ITemplateVariableNameAndValueFinder> _finder;
        private string _upstreamUrlPath;
        private Response<DownstreamRoute> _result;
        private Response<DownstreamRoute> _response;
        private List<ReRoute> _reRoutesConfig;
        private Response<UrlMatch> _match;
        private string _upstreamHttpMethod;

        public DownstreamRouteFinderTests()
        {
            _mockConfig = new Mock<IOcelotConfiguration>();
            _mockMatcher = new Mock<IUrlPathToUrlTemplateMatcher>();
            _finder = new Mock<ITemplateVariableNameAndValueFinder>();
            _downstreamRouteFinder = new Library.Infrastructure.DownstreamRouteFinder.DownstreamRouteFinder(_mockConfig.Object, _mockMatcher.Object, _finder.Object);
        }

        [Fact]
        public void should_return_route()
        {
            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("someUpstreamPath"))
                .And(x => x.GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<TemplateVariableNameAndValue>>(new List<TemplateVariableNameAndValue>())))
                .And(x => x.GivenTheConfigurationIs(new List<ReRoute>
                    {
                        new ReRoute("someDownstreamPath","someUpstreamPath", "Get", "someUpstreamPath", false)
                    }
                ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Get"))
                .When(x => x.WhenICallTheFinder())
                .Then(
                    x => x.ThenTheFollowingIsReturned(new DownstreamRoute(new List<TemplateVariableNameAndValue>(), "someDownstreamPath")))
                .And(x => x.ThenTheUrlMatcherIsCalledCorrectly())
                .BDDfy();
        }

        [Fact]
        public void should_return_correct_route_for_http_verb()
        {
            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("someUpstreamPath"))
                .And(x => x.GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<TemplateVariableNameAndValue>>(new List<TemplateVariableNameAndValue>())))
                .And(x => x.GivenTheConfigurationIs(new List<ReRoute>
                    {
                        new ReRoute("someDownstreamPath", "someUpstreamPath", "Get", string.Empty, false),
                        new ReRoute("someDownstreamPathForAPost", "someUpstreamPath", "Post", string.Empty, false)
                    }
                ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Post"))
                .When(x => x.WhenICallTheFinder())
                .Then(
                    x => x.ThenTheFollowingIsReturned(new DownstreamRoute(new List<TemplateVariableNameAndValue>(), "someDownstreamPathForAPost")))
                .BDDfy();
        }

        [Fact]
        public void should_not_return_route()
        {
            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("somePath"))
                 .And(x => x.GivenTheConfigurationIs(new List<ReRoute>
                     {
                        new ReRoute("somPath", "somePath", "Get", "somePath", false)
                     }
                 ))
                 .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(false))))
                 .And(x => x.GivenTheUpstreamHttpMethodIs("Get"))
                 .When(x => x.WhenICallTheFinder())
                 .Then(
                     x => x.ThenAnErrorResponseIsReturned())
                 .And(x => x.ThenTheUrlMatcherIsCalledCorrectly())
                 .BDDfy();
        }

        private void GivenTheTemplateVariableAndNameFinderReturns(Response<List<TemplateVariableNameAndValue>> response)
        {
            _finder
                .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(response);
        }

        private void GivenTheUpstreamHttpMethodIs(string upstreamHttpMethod)
        {
            _upstreamHttpMethod = upstreamHttpMethod;
        }

        private void ThenAnErrorResponseIsReturned()
        {
            _result.IsError.ShouldBeTrue();
        }

        private void ThenTheUrlMatcherIsCalledCorrectly()
        {
            _mockMatcher
                .Verify(x => x.Match(_upstreamUrlPath, _reRoutesConfig[0].UpstreamTemplate), Times.Once);
        }

        private void GivenTheUrlMatcherReturns(Response<UrlMatch> match)
        {
            _match = match;
            _mockMatcher
                .Setup(x => x.Match(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(_match);
        }

        private void GivenTheConfigurationIs(List<ReRoute> reRoutesConfig)
        {
            _reRoutesConfig = reRoutesConfig;
            _mockConfig
                .Setup(x => x.ReRoutes)
                .Returns(_reRoutesConfig);
        }

        private void GivenThereIsAnUpstreamUrlPath(string upstreamUrlPath)
        {
            _upstreamUrlPath = upstreamUrlPath;
        }

        private void WhenICallTheFinder()
        {
            _result = _downstreamRouteFinder.FindDownstreamRoute(_upstreamUrlPath, _upstreamHttpMethod);
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
