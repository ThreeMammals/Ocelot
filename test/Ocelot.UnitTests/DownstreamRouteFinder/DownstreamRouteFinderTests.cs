﻿using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder;
using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.DownstreamRouteFinder.HeaderMatcher;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Responses;
using Ocelot.Values;

namespace Ocelot.UnitTests.DownstreamRouteFinder
{
    public class DownstreamRouteFinderTests : UnitTest
    {
        private readonly IDownstreamRouteProvider _downstreamRouteFinder;
        private readonly Mock<IUrlPathToUrlTemplateMatcher> _mockUrlMatcher;
        private readonly Mock<IHeadersToHeaderTemplatesMatcher> _mockHeadersMatcher;
        private readonly Mock<IPlaceholderNameAndValueFinder> _urlPlaceholderFinder;
        private readonly Mock<IHeaderPlaceholderNameAndValueFinder> _headerPlaceholderFinder;
        private string _upstreamUrlPath;
        private Response<DownstreamRouteHolder> _result;
        private List<Route> _routesConfig;
        private InternalConfiguration _config;
        private Response<UrlMatch> _match;
        private string _upstreamHttpMethod;
        private string _upstreamHost;
        private Dictionary<string, string> _upstreamHeaders;
        private string _upstreamQuery;

        public DownstreamRouteFinderTests()
        {
            _mockUrlMatcher = new Mock<IUrlPathToUrlTemplateMatcher>();
            _mockHeadersMatcher = new Mock<IHeadersToHeaderTemplatesMatcher>();
            _urlPlaceholderFinder = new Mock<IPlaceholderNameAndValueFinder>();
            _headerPlaceholderFinder = new Mock<IHeaderPlaceholderNameAndValueFinder>();
            _downstreamRouteFinder = new Ocelot.DownstreamRouteFinder.Finder.DownstreamRouteFinder(_mockUrlMatcher.Object, _urlPlaceholderFinder.Object, _mockHeadersMatcher.Object, _headerPlaceholderFinder.Object);
        }

        [Fact]
        public void should_return_highest_priority_when_first()
        {
            var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();

            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("someUpstreamPath"))
                .And(x => x.GivenTheTemplateVariableAndNameFinderReturns(
                            new OkResponse<List<PlaceholderNameAndValue>>(new List<PlaceholderNameAndValue>())))
                .And(x => x.GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>()))
                .And(x => x.GivenTheConfigurationIs(new List<Route>
                {
                    new RouteBuilder()
                        .WithDownstreamRoute(new DownstreamRouteBuilder()
                            .WithDownstreamPathTemplate("someDownstreamPath")
                            .WithUpstreamHttpMethod(new List<string> { "Post" })
                            .WithUpstreamPathTemplate(new UpstreamPathTemplate("test", 1, false, "someUpstreamPath"))
                            .Build())
                        .WithUpstreamHttpMethod(new List<string> { "Post" })
                        .WithUpstreamPathTemplate(new UpstreamPathTemplate("test", 1, false, "someUpstreamPath"))
                        .Build(),
                    new RouteBuilder()
                        .WithDownstreamRoute(new DownstreamRouteBuilder()
                            .WithDownstreamPathTemplate("someDownstreamPath")
                            .WithUpstreamHttpMethod(new List<string> { "Post" })
                            .WithUpstreamPathTemplate(new UpstreamPathTemplate("test", 0, false, "someUpstreamPath"))
                            .Build())
                        .WithUpstreamHttpMethod(new List<string> { "Post" })
                        .WithUpstreamPathTemplate(new UpstreamPathTemplate("test", 0, false, "someUpstreamPath"))
                        .Build(),
                }, string.Empty, serviceProviderConfig))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheHeadersMatcherReturns(true))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Post"))
                .When(x => x.WhenICallTheFinder())
                .Then(x => x.ThenTheFollowingIsReturned(new DownstreamRouteHolder(new List<PlaceholderNameAndValue>(),
                        new RouteBuilder()
                            .WithDownstreamRoute(new DownstreamRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPath")
                                .WithUpstreamPathTemplate(new UpstreamPathTemplate("test", 1, false, "someUpstreamPath"))
                                .WithUpstreamHttpMethod(new List<string> { "Post" })
                                .Build())
                            .WithUpstreamPathTemplate(new UpstreamPathTemplate("test", 1, false, "someUpstreamPath"))
                            .WithUpstreamHttpMethod(new List<string> { "Post" })
                            .Build()
                        )))
                .BDDfy();
        }

        [Fact]
        public void should_return_highest_priority_when_lowest()
        {
            var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();

            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("someUpstreamPath"))
                .And(x => x.GivenTheTemplateVariableAndNameFinderReturns(
                            new OkResponse<List<PlaceholderNameAndValue>>(new List<PlaceholderNameAndValue>())))
                .And(x => x.GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>()))
                .And(x => x.GivenTheConfigurationIs(new List<Route>
                {
                    new RouteBuilder()
                        .WithDownstreamRoute(new DownstreamRouteBuilder()
                            .WithDownstreamPathTemplate("someDownstreamPath")
                            .WithUpstreamHttpMethod(new List<string> { "Post" })
                            .WithUpstreamPathTemplate(new UpstreamPathTemplate("test", 0, false, "someUpstreamPath"))
                            .Build())
                        .WithUpstreamHttpMethod(new List<string> { "Post" })
                        .WithUpstreamPathTemplate(new UpstreamPathTemplate("test", 0, false, "someUpstreamPath"))
                        .Build(),
                    new RouteBuilder()
                        .WithDownstreamRoute(new DownstreamRouteBuilder()
                            .WithDownstreamPathTemplate("someDownstreamPath")
                            .WithUpstreamHttpMethod(new List<string> { "Post" })
                            .WithUpstreamPathTemplate(new UpstreamPathTemplate("test", 1, false, "someUpstreamPath"))
                            .Build())
                        .WithUpstreamHttpMethod(new List<string> { "Post" })
                        .WithUpstreamPathTemplate(new UpstreamPathTemplate("test", 1, false, "someUpstreamPath"))
                        .Build(),
                }, string.Empty, serviceProviderConfig))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheHeadersMatcherReturns(true))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Post"))
                .When(x => x.WhenICallTheFinder())
                .Then(x => x.ThenTheFollowingIsReturned(new DownstreamRouteHolder(new List<PlaceholderNameAndValue>(),
                        new RouteBuilder()
                            .WithDownstreamRoute(new DownstreamRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPath")
                                .WithUpstreamPathTemplate(new UpstreamPathTemplate("test", 1, false, "someUpstreamPath"))
                                .WithUpstreamHttpMethod(new List<string> { "Post" })
                                .Build())
                            .WithUpstreamPathTemplate(new UpstreamPathTemplate("test", 1, false, "someUpstreamPath"))
                            .WithUpstreamHttpMethod(new List<string> { "Post" })
                            .Build()
                        )))
                .BDDfy();
        }

        [Fact]
        public void should_return_route()
        {
            var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();

            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("matchInUrlMatcher/"))
                .And(x => x.GivenTheTemplateVariableAndNameFinderReturns(
                        new OkResponse<List<PlaceholderNameAndValue>>(
                            new List<PlaceholderNameAndValue>())))
                .And(x => x.GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>()))
                .And(x => x.GivenTheConfigurationIs(new List<Route>
                {
                    new RouteBuilder()
                        .WithDownstreamRoute(new DownstreamRouteBuilder()
                            .WithDownstreamPathTemplate("someDownstreamPath")
                            .WithUpstreamHttpMethod(new List<string> { "Get" })
                            .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                            .Build())
                        .WithUpstreamHttpMethod(new List<string> { "Get" })
                        .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                        .Build(),
                }, string.Empty, serviceProviderConfig
                ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheHeadersMatcherReturns(true))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Get"))
                .When(x => x.WhenICallTheFinder())
                .Then(
                    x => x.ThenTheFollowingIsReturned(new DownstreamRouteHolder(
                            new List<PlaceholderNameAndValue>(),
                            new RouteBuilder()
                                .WithDownstreamRoute(new DownstreamRouteBuilder()
                                    .WithDownstreamPathTemplate("someDownstreamPath")
                                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                                    .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                                    .Build())
                                .WithUpstreamHttpMethod(new List<string> { "Get" })
                                .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                                .Build()
                )))
                .And(x => x.ThenTheUrlMatcherIsCalledCorrectly())
                .BDDfy();
        }

        [Fact]
        public void should_not_append_slash_to_upstream_url_path()
        {
            var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();

            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("matchInUrlMatcher"))
                .And(x => x.GivenTheTemplateVariableAndNameFinderReturns(
                        new OkResponse<List<PlaceholderNameAndValue>>(
                            new List<PlaceholderNameAndValue>())))
                .And(x => x.GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>()))
                .And(x => x.GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>()))
                .And(x => x.GivenTheConfigurationIs(new List<Route>
                {
                    new RouteBuilder()
                        .WithDownstreamRoute(new DownstreamRouteBuilder()
                            .WithDownstreamPathTemplate("someDownstreamPath")
                            .WithUpstreamHttpMethod(new List<string> { "Get" })
                            .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                            .Build())
                        .WithUpstreamHttpMethod(new List<string> { "Get" })
                        .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                        .Build(),
                }, string.Empty, serviceProviderConfig
                ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheHeadersMatcherReturns(true))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Get"))
                .When(x => x.WhenICallTheFinder())
                .Then(
                    x => x.ThenTheFollowingIsReturned(new DownstreamRouteHolder(
                            new List<PlaceholderNameAndValue>(),
                            new RouteBuilder()
                                .WithDownstreamRoute(new DownstreamRouteBuilder()
                                    .WithDownstreamPathTemplate("someDownstreamPath")
                                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                                    .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                                    .Build())
                                .WithUpstreamHttpMethod(new List<string> { "Get" })
                                .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                                .Build()
                )))
                .And(x => x.ThenTheUrlMatcherIsCalledCorrectly("matchInUrlMatcher"))
                .BDDfy();
        }

        [Fact]
        public void should_return_route_if_upstream_path_and_upstream_template_are_the_same()
        {
            var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();

            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("someUpstreamPath"))
                .And(
                    x =>
                        x.GivenTheTemplateVariableAndNameFinderReturns(
                            new OkResponse<List<PlaceholderNameAndValue>>(new List<PlaceholderNameAndValue>())))
                .And(x => x.GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>()))
                .And(x => x.GivenTheConfigurationIs(new List<Route>
                {
                    new RouteBuilder()
                        .WithDownstreamRoute(new DownstreamRouteBuilder()
                            .WithDownstreamPathTemplate("someDownstreamPath")
                            .WithUpstreamHttpMethod(new List<string> { "Get" })
                            .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                            .Build())
                        .WithUpstreamHttpMethod(new List<string> { "Get" })
                        .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                        .Build(),
                }, string.Empty, serviceProviderConfig
                    ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheHeadersMatcherReturns(true))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Get"))
                .When(x => x.WhenICallTheFinder())
                .Then(
                    x => x.ThenTheFollowingIsReturned(new DownstreamRouteHolder(new List<PlaceholderNameAndValue>(),
                        new RouteBuilder()
                            .WithDownstreamRoute(new DownstreamRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPath")
                                .WithUpstreamHttpMethod(new List<string> { "Get" })
                                .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                                .Build())
                            .WithUpstreamHttpMethod(new List<string> { "Get" })
                            .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                            .Build()
                        )))
                .BDDfy();
        }

        [Fact]
        public void should_return_correct_route_for_http_verb()
        {
            var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();

            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("someUpstreamPath"))
                .And(
                    x =>
                        x.GivenTheTemplateVariableAndNameFinderReturns(
                            new OkResponse<List<PlaceholderNameAndValue>>(new List<PlaceholderNameAndValue>())))
                .And(x => x.GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>()))
                .And(x => x.GivenTheConfigurationIs(new List<Route>
                {
                    new RouteBuilder()
                        .WithDownstreamRoute(new DownstreamRouteBuilder()
                            .WithDownstreamPathTemplate("someDownstreamPath")
                            .WithUpstreamHttpMethod(new List<string> { "Get" })
                            .WithUpstreamPathTemplate(new UpstreamPathTemplate(string.Empty, 1, false, "someUpstreamPath"))
                            .Build())
                        .WithUpstreamHttpMethod(new List<string> { "Get" })
                        .WithUpstreamPathTemplate(new UpstreamPathTemplate(string.Empty, 1, false, "someUpstreamPath"))
                        .Build(),
                    new RouteBuilder()
                        .WithDownstreamRoute(new DownstreamRouteBuilder()
                            .WithDownstreamPathTemplate("someDownstreamPathForAPost")
                            .WithUpstreamHttpMethod(new List<string> { "Post" })
                            .WithUpstreamPathTemplate(new UpstreamPathTemplate(string.Empty, 1, false, "someUpstreamPath"))
                            .Build())
                        .WithUpstreamHttpMethod(new List<string> { "Post" })
                        .WithUpstreamPathTemplate(new UpstreamPathTemplate(string.Empty, 1, false, "someUpstreamPath"))
                        .Build(),
                }, string.Empty, serviceProviderConfig
                    ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheHeadersMatcherReturns(true))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Post"))
                .When(x => x.WhenICallTheFinder())
                .Then(
                    x => x.ThenTheFollowingIsReturned(new DownstreamRouteHolder(new List<PlaceholderNameAndValue>(),
                        new RouteBuilder()
                            .WithDownstreamRoute(new DownstreamRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPathForAPost")
                                .WithUpstreamHttpMethod(new List<string> { "Post" })
                                .WithUpstreamPathTemplate(new UpstreamPathTemplate(string.Empty, 1, false, "someUpstreamPath"))
                                .Build())
                            .WithUpstreamHttpMethod(new List<string> { "Post" })
                            .WithUpstreamPathTemplate(new UpstreamPathTemplate(string.Empty, 1, false, "someUpstreamPath"))
                            .Build()
                        )))
                .BDDfy();
        }

        [Fact]
        public void should_not_return_route()
        {
            var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();

            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("dontMatchPath/"))
                 .And(x => x.GivenTheConfigurationIs(new List<Route>
                     {
                        new RouteBuilder()
                        .WithDownstreamRoute(new DownstreamRouteBuilder()
                                .WithDownstreamPathTemplate("somPath")
                                .WithUpstreamHttpMethod(new List<string> { "Get" })
                                .WithUpstreamPathTemplate(new UpstreamPathTemplate("somePath", 1, false, "someUpstreamPath"))
                                .Build())
                        .WithUpstreamHttpMethod(new List<string> { "Get" })
                        .WithUpstreamPathTemplate(new UpstreamPathTemplate("somePath", 1, false, "someUpstreamPath"))
                        .Build(),
                     }, string.Empty, serviceProviderConfig
                 ))
                 .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(false))))
                 .And(x => x.GivenTheHeadersMatcherReturns(true))
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
            var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();

            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("someUpstreamPath"))
                .And(
                    x =>
                        x.GivenTheTemplateVariableAndNameFinderReturns(
                            new OkResponse<List<PlaceholderNameAndValue>>(new List<PlaceholderNameAndValue>())))
                .And(x => x.GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>()))
                .And(x => x.GivenTheConfigurationIs(new List<Route>
                {
                    new RouteBuilder()
                        .WithDownstreamRoute(new DownstreamRouteBuilder()
                            .WithDownstreamPathTemplate("someDownstreamPath")
                            .WithUpstreamHttpMethod(new List<string> { "Get", "Post" })
                            .WithUpstreamPathTemplate(new UpstreamPathTemplate(string.Empty, 1, false, "someUpstreamPath"))
                            .Build())
                        .WithUpstreamHttpMethod(new List<string> { "Get", "Post" })
                        .WithUpstreamPathTemplate(new UpstreamPathTemplate(string.Empty, 1, false, "someUpstreamPath"))
                        .Build(),
                }, string.Empty, serviceProviderConfig
                    ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheHeadersMatcherReturns(true))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Post"))
                .When(x => x.WhenICallTheFinder())
                .Then(
                    x => x.ThenTheFollowingIsReturned(new DownstreamRouteHolder(new List<PlaceholderNameAndValue>(),
                        new RouteBuilder()
                            .WithDownstreamRoute(new DownstreamRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPath")
                                .WithUpstreamHttpMethod(new List<string> { "Post" })
                                .WithUpstreamPathTemplate(new UpstreamPathTemplate(string.Empty, 1, false, "someUpstreamPath"))
                                .Build())
                            .WithUpstreamHttpMethod(new List<string> { "Post" })
                            .WithUpstreamPathTemplate(new UpstreamPathTemplate(string.Empty, 1, false, "someUpstreamPath"))
                            .Build()
                        )))
                .BDDfy();
        }

        [Fact]
        public void should_return_correct_route_for_http_verb_setting_all_upstream_http_method()
        {
            var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();

            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("someUpstreamPath"))
                .And(
                    x =>
                        x.GivenTheTemplateVariableAndNameFinderReturns(
                            new OkResponse<List<PlaceholderNameAndValue>>(new List<PlaceholderNameAndValue>())))
                .And(x => x.GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>()))
                .And(x => x.GivenTheConfigurationIs(new List<Route>
                {
                    new RouteBuilder()
                        .WithDownstreamRoute(new DownstreamRouteBuilder()
                            .WithDownstreamPathTemplate("someDownstreamPath")
                            .WithUpstreamHttpMethod(new List<string>())
                            .WithUpstreamPathTemplate(new UpstreamPathTemplate(string.Empty, 1, false, "someUpstreamPath"))
                            .Build())
                        .WithUpstreamHttpMethod(new List<string>())
                        .WithUpstreamPathTemplate(new UpstreamPathTemplate(string.Empty, 1, false, "someUpstreamPath"))
                        .Build(),
                }, string.Empty, serviceProviderConfig
                    ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheHeadersMatcherReturns(true))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Post"))
                .When(x => x.WhenICallTheFinder())
                .Then(
                    x => x.ThenTheFollowingIsReturned(new DownstreamRouteHolder(new List<PlaceholderNameAndValue>(),
                        new RouteBuilder()
                            .WithDownstreamRoute(new DownstreamRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPath")
                                .WithUpstreamHttpMethod(new List<string> { "Post" })
                                .WithUpstreamPathTemplate(new UpstreamPathTemplate(string.Empty, 1, false, "someUpstreamPath"))
                                .Build())
                            .WithUpstreamHttpMethod(new List<string> { "Post" })
                            .WithUpstreamPathTemplate(new UpstreamPathTemplate(string.Empty, 1, false, "someUpstreamPath"))
                            .Build()
                        )))
                .BDDfy();
        }

        [Fact]
        public void should_not_return_route_for_http_verb_not_setting_in_upstream_http_method()
        {
            var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();

            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("someUpstreamPath"))
                .And(
                    x =>
                        x.GivenTheTemplateVariableAndNameFinderReturns(
                            new OkResponse<List<PlaceholderNameAndValue>>(new List<PlaceholderNameAndValue>())))
                .And(x => x.GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>()))
                .And(x => x.GivenTheConfigurationIs(new List<Route>
                {
                    new RouteBuilder()
                        .WithDownstreamRoute(new DownstreamRouteBuilder()
                            .WithDownstreamPathTemplate("someDownstreamPath")
                            .WithUpstreamHttpMethod(new List<string> { "Get", "Patch", "Delete" })
                            .WithUpstreamPathTemplate(new UpstreamPathTemplate(string.Empty, 1, false, "someUpstreamPath"))
                            .Build())
                        .WithUpstreamHttpMethod(new List<string> { "Get", "Patch", "Delete" })
                        .WithUpstreamPathTemplate(new UpstreamPathTemplate(string.Empty, 1, false, "someUpstreamPath"))
                        .Build(),
                }, string.Empty, serviceProviderConfig
                    ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheHeadersMatcherReturns(true))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Post"))
                .When(x => x.WhenICallTheFinder())
                 .Then(x => x.ThenAnErrorResponseIsReturned())
                 .And(x => x.ThenTheUrlMatcherIsNotCalled())
                 .BDDfy();
        }

        [Fact]
        public void should_return_route_when_host_matches()
        {
            var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();

            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("matchInUrlMatcher/"))
                .And(x => GivenTheUpstreamHostIs("MATCH"))
                .And(x => x.GivenTheTemplateVariableAndNameFinderReturns(
                    new OkResponse<List<PlaceholderNameAndValue>>(
                        new List<PlaceholderNameAndValue>())))
                .And(x => x.GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>()))
                .And(x => x.GivenTheConfigurationIs(new List<Route>
                    {
                        new RouteBuilder()
                            .WithDownstreamRoute(new DownstreamRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPath")
                                .WithUpstreamHttpMethod(new List<string> { "Get" })
                                .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                                .Build())
                            .WithUpstreamHttpMethod(new List<string> { "Get" })
                            .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                            .WithUpstreamHost("MATCH")
                            .Build(),
                    }, string.Empty, serviceProviderConfig
                ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheHeadersMatcherReturns(true))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Get"))
                .When(x => x.WhenICallTheFinder())
                .Then(
                    x => x.ThenTheFollowingIsReturned(new DownstreamRouteHolder(
                        new List<PlaceholderNameAndValue>(),
                        new RouteBuilder()
                            .WithDownstreamRoute(new DownstreamRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPath")
                                .WithUpstreamHttpMethod(new List<string> { "Get" })
                                .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                                .Build())
                            .WithUpstreamHttpMethod(new List<string> { "Get" })
                            .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                            .Build()
                    )))
                .And(x => x.ThenTheUrlMatcherIsCalledCorrectly())
                .BDDfy();
        }

        [Fact]
        public void should_return_route_when_upstreamhost_is_null()
        {
            var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();

            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("matchInUrlMatcher/"))
                .And(x => GivenTheUpstreamHostIs("MATCH"))
                .And(x => x.GivenTheTemplateVariableAndNameFinderReturns(
                    new OkResponse<List<PlaceholderNameAndValue>>(
                        new List<PlaceholderNameAndValue>())))
                .And(x => x.GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>()))
                .And(x => x.GivenTheConfigurationIs(new List<Route>
                    {
                        new RouteBuilder()
                            .WithDownstreamRoute(new DownstreamRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPath")
                                .WithUpstreamHttpMethod(new List<string> { "Get" })
                                .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                                .Build())
                            .WithUpstreamHttpMethod(new List<string> { "Get" })
                            .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                            .Build(),
                    }, string.Empty, serviceProviderConfig
                ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheHeadersMatcherReturns(true))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Get"))
                .When(x => x.WhenICallTheFinder())
                .Then(
                    x => x.ThenTheFollowingIsReturned(new DownstreamRouteHolder(
                        new List<PlaceholderNameAndValue>(),
                        new RouteBuilder()
                            .WithDownstreamRoute(new DownstreamRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPath")
                                .WithUpstreamHttpMethod(new List<string> { "Get" })
                                .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                                .Build())
                            .WithUpstreamHttpMethod(new List<string> { "Get" })
                            .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                            .Build()
                    )))
                .And(x => x.ThenTheUrlMatcherIsCalledCorrectly())
                .BDDfy();
        }

        [Fact]
        public void should_not_return_route_when_host_doesnt_match()
        {
            var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();

            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("matchInUrlMatcher/"))
                .And(x => GivenTheUpstreamHostIs("DONTMATCH"))
                .And(x => x.GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new List<PlaceholderNameAndValue>())))
                .And(x => x.GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>()))
                .And(x => x.GivenTheConfigurationIs(new List<Route>
                    {
                        new RouteBuilder()
                            .WithDownstreamRoute(new DownstreamRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPath")
                                .WithUpstreamHttpMethod(new List<string> { "Get" })
                                .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                                .Build())
                            .WithUpstreamHttpMethod(new List<string> { "Get" })
                            .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                            .WithUpstreamHost("MATCH")
                            .Build(),
                        new RouteBuilder()
                            .WithDownstreamRoute(new DownstreamRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPath")
                                .WithUpstreamHttpMethod(new List<string>()) // empty list of methods
                                .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                                .Build())
                            .WithUpstreamHttpMethod(new List<string>()) // empty list of methods
                            .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                            .WithUpstreamHost("MATCH")
                            .Build(),
                    }, string.Empty, serviceProviderConfig
                ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheHeadersMatcherReturns(true))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Get"))
                .When(x => x.WhenICallTheFinder())
                .Then(x => x.ThenAnErrorResponseIsReturned())
                .And(x => x.ThenTheUrlMatcherIsNotCalled())
                .BDDfy();
        }

        [Fact]
        public void should_not_return_route_when_host_doesnt_match_with_empty_upstream_http_method()
        {
            var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();

            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("matchInUrlMatcher/"))
                .And(x => GivenTheUpstreamHostIs("DONTMATCH"))
                .And(x => x.GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new List<PlaceholderNameAndValue>())))
                .And(x => x.GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>()))
                .And(x => x.GivenTheConfigurationIs(new List<Route>
                    {
                        new RouteBuilder()
                            .WithDownstreamRoute(new DownstreamRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPath")
                                .WithUpstreamHttpMethod(new List<string>())
                                .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                                .Build())
                            .WithUpstreamHttpMethod(new List<string>())
                            .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                            .WithUpstreamHost("MATCH")
                            .Build(),
                    }, string.Empty, serviceProviderConfig
                ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheHeadersMatcherReturns(true))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Get"))
                .When(x => x.WhenICallTheFinder())
                .Then(x => x.ThenAnErrorResponseIsReturned())
                .And(x => x.ThenTheUrlMatcherIsNotCalled())
                .BDDfy();
        }

        [Fact]
        public void should_return_route_when_host_does_match_with_empty_upstream_http_method()
        {
            var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();

            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("matchInUrlMatcher/"))
                .And(x => GivenTheUpstreamHostIs("MATCH"))
                .And(x => x.GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new List<PlaceholderNameAndValue>())))
                .And(x => x.GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>()))
                .And(x => x.GivenTheConfigurationIs(new List<Route>
                    {
                        new RouteBuilder()
                            .WithDownstreamRoute(new DownstreamRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPath")
                                .WithUpstreamHttpMethod(new List<string>())
                                .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                                .Build())
                            .WithUpstreamHttpMethod(new List<string>())
                            .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                            .WithUpstreamHost("MATCH")
                            .Build(),
                    }, string.Empty, serviceProviderConfig
                ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheHeadersMatcherReturns(true))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Get"))
                .When(x => x.WhenICallTheFinder())
                .And(x => x.ThenTheUrlMatcherIsCalledCorrectly(1, 0))
                .BDDfy();
        }

        [Fact]
        public void should_return_route_when_host_matches_but_null_host_on_same_path_first()
        {
            var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();

            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("matchInUrlMatcher/"))
                .And(x => GivenTheUpstreamHostIs("MATCH"))
                .And(x => x.GivenTheTemplateVariableAndNameFinderReturns(
                    new OkResponse<List<PlaceholderNameAndValue>>(
                        new List<PlaceholderNameAndValue>())))
                .And(x => x.GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>()))
                .And(x => x.GivenTheConfigurationIs(new List<Route>
                    {
                        new RouteBuilder()
                            .WithDownstreamRoute(new DownstreamRouteBuilder()
                                .WithDownstreamPathTemplate("THENULLPATH")
                                .WithUpstreamHttpMethod(new List<string> { "Get" })
                                .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                                .Build())
                            .WithUpstreamHttpMethod(new List<string> { "Get" })
                            .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                            .Build(),
                        new RouteBuilder()
                            .WithDownstreamRoute(new DownstreamRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPath")
                                .WithUpstreamHttpMethod(new List<string> { "Get" })
                                .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                                .Build())
                            .WithUpstreamHttpMethod(new List<string> { "Get" })
                            .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                            .WithUpstreamHost("MATCH")
                            .Build(),
                    }, string.Empty, serviceProviderConfig
                ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheHeadersMatcherReturns(true))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Get"))
                .When(x => x.WhenICallTheFinder())
                .Then(
                    x => x.ThenTheFollowingIsReturned(new DownstreamRouteHolder(
                        new List<PlaceholderNameAndValue>(),
                        new RouteBuilder()
                            .WithDownstreamRoute(new DownstreamRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPath")
                                .WithUpstreamHttpMethod(new List<string> { "Get" })
                                .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "test"))
                                .Build())
                            .WithUpstreamHttpMethod(new List<string> { "Get" })
                            .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "test"))
                            .Build()
                    )))
                .And(x => x.ThenTheUrlMatcherIsCalledCorrectly(1, 0))
                .And(x => x.ThenTheUrlMatcherIsCalledCorrectly(1, 1))
                .BDDfy();
        }

        [Fact]
        [Trait("PR", "1312")]
        [Trait("Feat", "360")]
        public void Should_return_route_when_upstream_headers_match()
        {
            // Arrange
            var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
            var upstreamHeaders = new Dictionary<string, string>()
            {
                ["header1"] = "headerValue1",
                ["header2"] = "headerValue2",
                ["header3"] = "headerValue3",
            };
            var upstreamHeadersConfig = new Dictionary<string, UpstreamHeaderTemplate>()
            {
                ["header1"] = new UpstreamHeaderTemplate("headerValue1", "headerValue1"),
                ["header2"] = new UpstreamHeaderTemplate("headerValue2", "headerValue2"),
            };
            var urlPlaceholders = new List<PlaceholderNameAndValue> { new PlaceholderNameAndValue("url", "urlValue") };
            var headerPlaceholders = new List<PlaceholderNameAndValue> { new PlaceholderNameAndValue("header", "headerValue") };

            GivenThereIsAnUpstreamUrlPath("matchInUrlMatcher/");
            GivenTheUpstreamHeadersIs(upstreamHeaders);
            GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(urlPlaceholders));
            GivenTheHeaderPlaceholderAndNameFinderReturns(headerPlaceholders);
            GivenTheConfigurationIs(
                new()
                {
                    new RouteBuilder()
                    .WithDownstreamRoute(new DownstreamRouteBuilder()
                        .WithDownstreamPathTemplate("someDownstreamPath")
                        .WithUpstreamHttpMethod(new() {"Get"})
                        .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                        .Build())
                    .WithUpstreamHttpMethod(new() {"Get"})
                    .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                    .WithUpstreamHeaders(upstreamHeadersConfig)
                    .Build(),
                },
                string.Empty,
                serviceProviderConfig);
            GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true)));
            GivenTheHeadersMatcherReturns(true);
            GivenTheUpstreamHttpMethodIs("Get");

            // Act
            WhenICallTheFinder();

            // Assert
            ThenTheFollowingIsReturned(new DownstreamRouteHolder(
                    urlPlaceholders.Union(headerPlaceholders).ToList(),
                    new RouteBuilder()
                        .WithDownstreamRoute(new DownstreamRouteBuilder()
                            .WithDownstreamPathTemplate("someDownstreamPath")
                            .WithUpstreamHttpMethod(new() { "Get" })
                            .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                            .Build())
                        .WithUpstreamHttpMethod(new() { "Get" })
                        .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                        .Build()
                ));
            ThenTheUrlMatcherIsCalledCorrectly();
        }

        [Fact]
        [Trait("PR", "1312")]
        [Trait("Feat", "360")]
        public void Should_not_return_route_when_upstream_headers_dont_match()
        {
            // Arrange
            var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
            var upstreamHeadersConfig = new Dictionary<string, UpstreamHeaderTemplate>()
            {
                ["header1"] = new UpstreamHeaderTemplate("headerValue1", "headerValue1"),
                ["header2"] = new UpstreamHeaderTemplate("headerValue2", "headerValue2"),
            };

            GivenThereIsAnUpstreamUrlPath("matchInUrlMatcher/");
            GivenTheUpstreamHeadersIs(new Dictionary<string, string>() { { "header1", "headerValue1" } });
            GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new List<PlaceholderNameAndValue>()));
            GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
            GivenTheConfigurationIs(new List<Route>
                    {
                        new RouteBuilder()
                            .WithDownstreamRoute(new DownstreamRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPath")
                                .WithUpstreamHttpMethod(new() { "Get" })
                                .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                                .Build())
                            .WithUpstreamHttpMethod(new() { "Get" })
                            .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                            .WithUpstreamHeaders(upstreamHeadersConfig)
                            .Build(),
                        new RouteBuilder()
                            .WithDownstreamRoute(new DownstreamRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPath")
                                .WithUpstreamHttpMethod(new() { "Get" })
                                .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                                .Build())
                            .WithUpstreamHttpMethod(new() { "Get" })
                            .WithUpstreamPathTemplate(new UpstreamPathTemplate("someUpstreamPath", 1, false, "someUpstreamPath"))
                            .WithUpstreamHeaders(upstreamHeadersConfig)
                            .Build(),
                    }, string.Empty, serviceProviderConfig
            );
            GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true)));
            GivenTheHeadersMatcherReturns(false);
            GivenTheUpstreamHttpMethodIs("Get");

            // Act
            WhenICallTheFinder();

            // Assert
            ThenAnErrorResponseIsReturned();
        }

        private void GivenTheUpstreamHostIs(string upstreamHost)
        {
            _upstreamHost = upstreamHost;
        }

        private void GivenTheTemplateVariableAndNameFinderReturns(Response<List<PlaceholderNameAndValue>> response)
        {
            _urlPlaceholderFinder
                .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(response);
        }

        private void GivenTheHeaderPlaceholderAndNameFinderReturns(List<PlaceholderNameAndValue> placeholders)
        {
            _headerPlaceholderFinder
                .Setup(x => x.Find(It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, UpstreamHeaderTemplate>>()))
                .Returns(placeholders);
        }

        private void GivenTheUpstreamHttpMethodIs(string upstreamHttpMethod)
        {
            _upstreamHttpMethod = upstreamHttpMethod;
        }

        private void GivenTheUpstreamHeadersIs(Dictionary<string, string> upstreamHeaders)
        {
            _upstreamHeaders = upstreamHeaders;
        }

        private void ThenAnErrorResponseIsReturned()
        {
            _result.IsError.ShouldBeTrue();
        }

        private void ThenTheUrlMatcherIsCalledCorrectly()
        {
            _mockUrlMatcher
                .Verify(x => x.Match(_upstreamUrlPath, _upstreamQuery, _routesConfig[0].UpstreamTemplatePattern), Times.Once);
        }

        private void ThenTheUrlMatcherIsCalledCorrectly(int times, int index = 0)
        {
            _mockUrlMatcher
                .Verify(x => x.Match(_upstreamUrlPath, _upstreamQuery, _routesConfig[index].UpstreamTemplatePattern), Times.Exactly(times));
        }

        private void ThenTheUrlMatcherIsCalledCorrectly(string expectedUpstreamUrlPath)
        {
            _mockUrlMatcher
                .Verify(x => x.Match(expectedUpstreamUrlPath, _upstreamQuery, _routesConfig[0].UpstreamTemplatePattern), Times.Once);
        }

        private void ThenTheUrlMatcherIsNotCalled()
        {
            _mockUrlMatcher
                .Verify(x => x.Match(_upstreamUrlPath, _upstreamQuery, _routesConfig[0].UpstreamTemplatePattern), Times.Never);
        }

        private void GivenTheUrlMatcherReturns(Response<UrlMatch> match)
        {
            _match = match;
            _mockUrlMatcher
                .Setup(x => x.Match(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UpstreamPathTemplate>()))
                .Returns(_match);
        }

        private void GivenTheHeadersMatcherReturns(bool headersMatch)
        {
            _mockHeadersMatcher
                .Setup(x => x.Match(It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, UpstreamHeaderTemplate>>()))
                .Returns(headersMatch);
        }

        private void GivenTheConfigurationIs(List<Route> routesConfig, string adminPath, ServiceProviderConfiguration serviceProviderConfig)
        {
            _routesConfig = routesConfig;
            _config = new InternalConfiguration(
                _routesConfig,
                adminPath,
                serviceProviderConfig,
                string.Empty,
                new LoadBalancerOptionsBuilder().Build(),
                string.Empty,
                new QoSOptionsBuilder().Build(),
                new HttpHandlerOptionsBuilder().Build(),
                new Version("1.1"),
                HttpVersionPolicy.RequestVersionOrLower);
        }

        private void GivenThereIsAnUpstreamUrlPath(string upstreamUrlPath)
        {
            _upstreamUrlPath = upstreamUrlPath;
            _upstreamQuery = string.Empty;
        }

        private void WhenICallTheFinder()
        {
            _result = _downstreamRouteFinder.Get(_upstreamUrlPath, _upstreamQuery, _upstreamHttpMethod, _config, _upstreamHost, _upstreamHeaders);
        }

        private void ThenTheFollowingIsReturned(DownstreamRouteHolder expected)
        {
            _result.Data.Route.DownstreamRoute[0].DownstreamPathTemplate.Value.ShouldBe(expected.Route.DownstreamRoute[0].DownstreamPathTemplate.Value);
            _result.Data.Route.UpstreamTemplatePattern.Priority.ShouldBe(expected.Route.UpstreamTemplatePattern.Priority);

            for (var i = 0; i < _result.Data.TemplatePlaceholderNameAndValues.Count; i++)
            {
                _result.Data.TemplatePlaceholderNameAndValues[i].Name.ShouldBe(expected.TemplatePlaceholderNameAndValues[i].Name);
                _result.Data.TemplatePlaceholderNameAndValues[i].Value.ShouldBe(expected.TemplatePlaceholderNameAndValues[i].Value);
            }

            _result.IsError.ShouldBeFalse();
        }
    }
}
