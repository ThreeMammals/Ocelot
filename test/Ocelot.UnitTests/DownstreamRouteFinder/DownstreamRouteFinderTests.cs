using System.Collections.Generic;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Provider;
using Ocelot.DownstreamRouteFinder;
using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.DownstreamRouteFinder
{
    public class DownstreamRouteFinderTests
    {
        private readonly IDownstreamRouteFinder _downstreamRouteFinder;
        private readonly Mock<IOcelotConfigurationProvider> _mockConfig;
        private readonly Mock<IUrlPathToUrlTemplateMatcher> _mockMatcher;
        private readonly Mock<IUrlPathPlaceholderNameAndValueFinder> _finder;
        private string _upstreamUrlPath;
        private Response<DownstreamRoute> _result;
        private List<ReRoute> _reRoutesConfig;
        private Response<UrlMatch> _match;
        private string _upstreamHttpMethod;

        public DownstreamRouteFinderTests()
        {
            _mockConfig = new Mock<IOcelotConfigurationProvider>();
            _mockMatcher = new Mock<IUrlPathToUrlTemplateMatcher>();
            _finder = new Mock<IUrlPathPlaceholderNameAndValueFinder>();
            _downstreamRouteFinder = new Ocelot.DownstreamRouteFinder.Finder.DownstreamRouteFinder(_mockConfig.Object, _mockMatcher.Object, _finder.Object);
        }

        [Fact]
        public void should_return_route()
        {
            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("matchInUrlMatcher"))
                .And(x =>x.GivenTheTemplateVariableAndNameFinderReturns(
                        new OkResponse<List<UrlPathPlaceholderNameAndValue>>(
                            new List<UrlPathPlaceholderNameAndValue>())))
                .And(x => x.GivenTheConfigurationIs(new List<ReRoute>
                {
                    new ReRouteBuilder()
                        .WithDownstreamPathTemplate("someDownstreamPath")
                        .WithUpstreamPathTemplate("someUpstreamPath")
                        .WithUpstreamHttpMethod(new List<string> { "Get" })
                        .WithUpstreamTemplatePattern("someUpstreamPath")
                        .Build()
                }, string.Empty
                ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Get"))
                .When(x => x.WhenICallTheFinder())
                .Then(
                    x => x.ThenTheFollowingIsReturned(new DownstreamRoute(
                            new List<UrlPathPlaceholderNameAndValue>(),
                            new ReRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPath")
                                .WithUpstreamHttpMethod(new List<string> { "Get" })
                                .Build()
                )))
                .And(x => x.ThenTheUrlMatcherIsCalledCorrectly())
                .BDDfy();
        }

        [Fact]
        public void should_return_route_if_upstream_path_and_upstream_template_are_the_same()
        {
            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("someUpstreamPath"))
                .And(
                    x =>
                        x.GivenTheTemplateVariableAndNameFinderReturns(
                            new OkResponse<List<UrlPathPlaceholderNameAndValue>>(new List<UrlPathPlaceholderNameAndValue>())))
                .And(x => x.GivenTheConfigurationIs(new List<ReRoute>
                {
                    new ReRouteBuilder()
                        .WithDownstreamPathTemplate("someDownstreamPath")
                        .WithUpstreamPathTemplate("someUpstreamPath")
                        .WithUpstreamHttpMethod(new List<string> { "Get" })
                        .WithUpstreamTemplatePattern("someUpstreamPath")
                        .Build()
                }, string.Empty
                    ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Get"))
                .When(x => x.WhenICallTheFinder())
                .Then(
                    x => x.ThenTheFollowingIsReturned(new DownstreamRoute(new List<UrlPathPlaceholderNameAndValue>(),
                        new ReRouteBuilder()
                            .WithDownstreamPathTemplate("someDownstreamPath")
                            .WithUpstreamHttpMethod(new List<string> { "Get" })
                            .Build()
                        )))
                .And(x => x.ThenTheUrlMatcherIsNotCalled())
                .BDDfy();
        }

        [Fact]
        public void should_return_correct_route_for_http_verb()
        {
            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("someUpstreamPath"))
                .And(
                    x =>
                        x.GivenTheTemplateVariableAndNameFinderReturns(
                            new OkResponse<List<UrlPathPlaceholderNameAndValue>>(new List<UrlPathPlaceholderNameAndValue>())))
                .And(x => x.GivenTheConfigurationIs(new List<ReRoute>
                {
                    new ReRouteBuilder()
                        .WithDownstreamPathTemplate("someDownstreamPath")
                        .WithUpstreamPathTemplate("someUpstreamPath")
                        .WithUpstreamHttpMethod(new List<string> { "Get" })
                        .WithUpstreamTemplatePattern("")
                        .Build(),
                    new ReRouteBuilder()
                        .WithDownstreamPathTemplate("someDownstreamPathForAPost")
                        .WithUpstreamPathTemplate("someUpstreamPath")
                        .WithUpstreamHttpMethod(new List<string> { "Post" })
                        .WithUpstreamTemplatePattern("")
                        .Build()
                }, string.Empty
                    ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Post"))
                .When(x => x.WhenICallTheFinder())
                .Then(
                    x => x.ThenTheFollowingIsReturned(new DownstreamRoute(new List<UrlPathPlaceholderNameAndValue>(),
                        new ReRouteBuilder()
                            .WithDownstreamPathTemplate("someDownstreamPathForAPost")
                            .WithUpstreamHttpMethod(new List<string> { "Post" })
                            .Build()
                        )))
                .BDDfy();
        }

        [Fact]
        public void should_not_return_route()
        {
            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("dontMatchPath"))
                 .And(x => x.GivenTheConfigurationIs(new List<ReRoute>
                     {
                        new ReRouteBuilder()
                        .WithDownstreamPathTemplate("somPath")
                        .WithUpstreamPathTemplate("somePath")
                        .WithUpstreamHttpMethod(new List<string> { "Get" })
                        .WithUpstreamTemplatePattern("somePath")
                        .Build(),   
                     }, string.Empty
                 ))
                 .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(false))))
                 .And(x => x.GivenTheUpstreamHttpMethodIs("Get"))
                 .When(x => x.WhenICallTheFinder())
                 .Then(
                     x => x.ThenAnErrorResponseIsReturned())
                 .And(x => x.ThenTheUrlMatcherIsCalledCorrectly())
                 .BDDfy();
        }

        [Fact]
        public void should_return_correct_route_for_http_verb_setting_multiple_upstream_http_method()
        {
            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("someUpstreamPath"))
                .And(
                    x =>
                        x.GivenTheTemplateVariableAndNameFinderReturns(
                            new OkResponse<List<UrlPathPlaceholderNameAndValue>>(new List<UrlPathPlaceholderNameAndValue>())))
                .And(x => x.GivenTheConfigurationIs(new List<ReRoute>
                {
                    new ReRouteBuilder()
                        .WithDownstreamPathTemplate("someDownstreamPath")
                        .WithUpstreamPathTemplate("someUpstreamPath")
                        .WithUpstreamHttpMethod(new List<string> { "Get", "Post" })
                        .WithUpstreamTemplatePattern("")
                        .Build()
                }, string.Empty
                    ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Post"))
                .When(x => x.WhenICallTheFinder())
                .Then(
                    x => x.ThenTheFollowingIsReturned(new DownstreamRoute(new List<UrlPathPlaceholderNameAndValue>(),
                        new ReRouteBuilder()
                            .WithDownstreamPathTemplate("someDownstreamPath")
                            .WithUpstreamHttpMethod(new List<string> { "Post" })
                            .Build()
                        )))
                .BDDfy();
        }

        [Fact]
        public void should_return_correct_route_for_http_verb_setting_all_upstream_http_method()
        {
            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("someUpstreamPath"))
                .And(
                    x =>
                        x.GivenTheTemplateVariableAndNameFinderReturns(
                            new OkResponse<List<UrlPathPlaceholderNameAndValue>>(new List<UrlPathPlaceholderNameAndValue>())))
                .And(x => x.GivenTheConfigurationIs(new List<ReRoute>
                {
                    new ReRouteBuilder()
                        .WithDownstreamPathTemplate("someDownstreamPath")
                        .WithUpstreamPathTemplate("someUpstreamPath")
                        .WithUpstreamHttpMethod(new List<string>())
                        .WithUpstreamTemplatePattern("")
                        .Build()
                }, string.Empty
                    ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Post"))
                .When(x => x.WhenICallTheFinder())
                .Then(
                    x => x.ThenTheFollowingIsReturned(new DownstreamRoute(new List<UrlPathPlaceholderNameAndValue>(),
                        new ReRouteBuilder()
                            .WithDownstreamPathTemplate("someDownstreamPath")
                            .WithUpstreamHttpMethod(new List<string> { "Post" })
                            .Build()
                        )))
                .BDDfy();
        }

        [Fact]
        public void should_not_return_route_for_http_verb_not_setting_in_upstream_http_method()
        {
            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("someUpstreamPath"))
                .And(
                    x =>
                        x.GivenTheTemplateVariableAndNameFinderReturns(
                            new OkResponse<List<UrlPathPlaceholderNameAndValue>>(new List<UrlPathPlaceholderNameAndValue>())))
                .And(x => x.GivenTheConfigurationIs(new List<ReRoute>
                {
                    new ReRouteBuilder()
                        .WithDownstreamPathTemplate("someDownstreamPath")
                        .WithUpstreamPathTemplate("someUpstreamPath")
                        .WithUpstreamHttpMethod(new List<string> { "Get", "Patch", "Delete" })
                        .WithUpstreamTemplatePattern("")
                        .Build()
                }, string.Empty
                    ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Post"))
                .When(x => x.WhenICallTheFinder())
                 .Then(
                     x => x.ThenAnErrorResponseIsReturned())
                 .And(x => x.ThenTheUrlMatcherIsNotCalled())
                 .BDDfy();
        }

        private void GivenTheTemplateVariableAndNameFinderReturns(Response<List<UrlPathPlaceholderNameAndValue>> response)
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
                .Verify(x => x.Match(_upstreamUrlPath, _reRoutesConfig[0].UpstreamPathTemplate.Value), Times.Once);
        }

        private void ThenTheUrlMatcherIsNotCalled()
        {
            _mockMatcher
                .Verify(x => x.Match(_upstreamUrlPath, _reRoutesConfig[0].UpstreamPathTemplate.Value), Times.Never);
        }

        private void GivenTheUrlMatcherReturns(Response<UrlMatch> match)
        {
            _match = match;
            _mockMatcher
                .Setup(x => x.Match(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(_match);
        }

        private void GivenTheConfigurationIs(List<ReRoute> reRoutesConfig, string adminPath)
        {
            _reRoutesConfig = reRoutesConfig;
            _mockConfig
                .Setup(x => x.Get())
                .ReturnsAsync(new OkResponse<IOcelotConfiguration>(new OcelotConfiguration(_reRoutesConfig, adminPath)));
        }

        private void GivenThereIsAnUpstreamUrlPath(string upstreamUrlPath)
        {
            _upstreamUrlPath = upstreamUrlPath;
        }

        private void WhenICallTheFinder()
        {
            _result = _downstreamRouteFinder.FindDownstreamRoute(_upstreamUrlPath, _upstreamHttpMethod).Result;
        }

        private void ThenTheFollowingIsReturned(DownstreamRoute expected)
        {
            _result.Data.ReRoute.DownstreamPathTemplate.Value.ShouldBe(expected.ReRoute.DownstreamPathTemplate.Value);

            for (int i = 0; i < _result.Data.TemplatePlaceholderNameAndValues.Count; i++)
            {
                _result.Data.TemplatePlaceholderNameAndValues[i].TemplateVariableName.ShouldBe(
                    expected.TemplatePlaceholderNameAndValues[i].TemplateVariableName);

                _result.Data.TemplatePlaceholderNameAndValues[i].TemplateVariableValue.ShouldBe(
                    expected.TemplatePlaceholderNameAndValues[i].TemplateVariableValue);
            }
            
            _result.IsError.ShouldBeFalse();
        }
    }
}
