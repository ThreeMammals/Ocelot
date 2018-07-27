﻿using System.Collections.Generic;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder;
using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Responses;
using Ocelot.Values;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.DownstreamRouteFinder
{
    public class DownstreamRouteFinderTests
    {
        private readonly IDownstreamRouteProvider _downstreamRouteFinder;
        private readonly Mock<IUrlPathToUrlTemplateMatcher> _mockMatcher;
        private readonly Mock<IPlaceholderNameAndValueFinder> _finder;
        private string _upstreamUrlPath;
        private Response<DownstreamRoute> _result;
        private List<ReRoute> _reRoutesConfig;
        private InternalConfiguration _config;
        private Response<UrlMatch> _match;
        private string _upstreamHttpMethod;
        private string _upstreamHost;
        private string _upstreamQuery;

        public DownstreamRouteFinderTests()
        {
            _mockMatcher = new Mock<IUrlPathToUrlTemplateMatcher>();
            _finder = new Mock<IPlaceholderNameAndValueFinder>();
            _downstreamRouteFinder = new Ocelot.DownstreamRouteFinder.Finder.DownstreamRouteFinder(_mockMatcher.Object, _finder.Object);
        }

        [Fact]
        public void should_return_highest_priority_when_first()
        {
            var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();

            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("someUpstreamPath"))
                .And(x => x.GivenTheTemplateVariableAndNameFinderReturns(
                            new OkResponse<List<PlaceholderNameAndValue>>(new List<PlaceholderNameAndValue>())))
                .And(x => x.GivenTheConfigurationIs(new List<ReRoute>
                {
                    new ReRouteBuilder()
                        .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                            .WithDownstreamPathTemplate("someDownstreamPath")
                            .WithUpstreamPathTemplate("someUpstreamPath")
                            .WithUpstreamHttpMethod(new List<string> { "Post" })
                            .WithUpstreamTemplatePattern(new UpstreamPathTemplate("test", 1, false))
                            .Build())
                        .WithUpstreamPathTemplate("someUpstreamPath")
                        .WithUpstreamHttpMethod(new List<string> { "Post" })
                        .WithUpstreamTemplatePattern(new UpstreamPathTemplate("test", 1, false))
                        .Build(),
                    new ReRouteBuilder()
                        .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                            .WithDownstreamPathTemplate("someDownstreamPath")
                            .WithUpstreamPathTemplate("someUpstreamPath")
                            .WithUpstreamHttpMethod(new List<string> { "Post" })
                            .WithUpstreamTemplatePattern(new UpstreamPathTemplate("test", 0, false))
                            .Build())
                        .WithUpstreamPathTemplate("someUpstreamPath")
                        .WithUpstreamHttpMethod(new List<string> { "Post" })
                        .WithUpstreamTemplatePattern(new UpstreamPathTemplate("test", 0, false))
                        .Build()
                }, string.Empty, serviceProviderConfig))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Post"))
                .When(x => x.WhenICallTheFinder())
                .Then(x => x.ThenTheFollowingIsReturned(new DownstreamRoute(new List<PlaceholderNameAndValue>(),
                        new ReRouteBuilder()
                            .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPath")
                                .WithUpstreamTemplatePattern(new UpstreamPathTemplate("test", 1, false))
                                .WithUpstreamHttpMethod(new List<string> { "Post" })
                                .WithUpstreamTemplatePattern(new UpstreamPathTemplate("test", 1, false))
                                .Build())
                            .WithUpstreamTemplatePattern(new UpstreamPathTemplate("test", 1, false))
                            .WithUpstreamHttpMethod(new List<string> { "Post" })
                            .WithUpstreamTemplatePattern(new UpstreamPathTemplate("test", 1, false))
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
                .And(x => x.GivenTheConfigurationIs(new List<ReRoute>
                {
                    new ReRouteBuilder()
                        .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                            .WithDownstreamPathTemplate("someDownstreamPath")
                            .WithUpstreamPathTemplate("someUpstreamPath")
                            .WithUpstreamHttpMethod(new List<string> { "Post" })
                            .WithUpstreamTemplatePattern(new UpstreamPathTemplate("test", 0, false))
                            .Build())
                        .WithUpstreamPathTemplate("someUpstreamPath")
                        .WithUpstreamHttpMethod(new List<string> { "Post" })
                        .WithUpstreamTemplatePattern(new UpstreamPathTemplate("test", 0, false))
                        .Build(),
                    new ReRouteBuilder()
                        .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                            .WithDownstreamPathTemplate("someDownstreamPath")
                            .WithUpstreamPathTemplate("someUpstreamPath")
                            .WithUpstreamHttpMethod(new List<string> { "Post" })
                            .WithUpstreamTemplatePattern(new UpstreamPathTemplate("test", 1, false))
                            .Build())
                        .WithUpstreamPathTemplate("someUpstreamPath")
                        .WithUpstreamHttpMethod(new List<string> { "Post" })
                        .WithUpstreamTemplatePattern(new UpstreamPathTemplate("test", 1, false))
                        .Build()
                }, string.Empty, serviceProviderConfig))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Post"))
                .When(x => x.WhenICallTheFinder())
                .Then(x => x.ThenTheFollowingIsReturned(new DownstreamRoute(new List<PlaceholderNameAndValue>(),
                        new ReRouteBuilder()
                            .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPath")
                                .WithUpstreamTemplatePattern(new UpstreamPathTemplate("test", 1, false))
                                .WithUpstreamHttpMethod(new List<string> { "Post" })
                                .Build())
                            .WithUpstreamTemplatePattern(new UpstreamPathTemplate("test", 1, false))
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
                .And(x => x.GivenTheConfigurationIs(new List<ReRoute>
                {
                    new ReRouteBuilder()
                        .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                            .WithDownstreamPathTemplate("someDownstreamPath")
                            .WithUpstreamPathTemplate("someUpstreamPath")
                            .WithUpstreamHttpMethod(new List<string> { "Get" })
                            .WithUpstreamTemplatePattern(new UpstreamPathTemplate("someUpstreamPath", 1, false))
                            .Build())
                        .WithUpstreamPathTemplate("someUpstreamPath")
                        .WithUpstreamHttpMethod(new List<string> { "Get" })
                        .WithUpstreamTemplatePattern(new UpstreamPathTemplate("someUpstreamPath", 1, false))
                        .Build()
                }, string.Empty, serviceProviderConfig
                ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Get"))
                .When(x => x.WhenICallTheFinder())
                .Then(
                    x => x.ThenTheFollowingIsReturned(new DownstreamRoute(
                            new List<PlaceholderNameAndValue>(),
                            new ReRouteBuilder()
                                .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                                    .WithDownstreamPathTemplate("someDownstreamPath")
                                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                                    .WithUpstreamTemplatePattern(new UpstreamPathTemplate("someUpstreamPath", 1, false))
                                    .Build())
                                .WithUpstreamHttpMethod(new List<string> { "Get" })
                                .WithUpstreamTemplatePattern(new UpstreamPathTemplate("someUpstreamPath", 1, false))
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
                .And(x => x.GivenTheConfigurationIs(new List<ReRoute>
                {
                    new ReRouteBuilder()
                        .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                            .WithDownstreamPathTemplate("someDownstreamPath")
                            .WithUpstreamPathTemplate("someUpstreamPath")
                            .WithUpstreamHttpMethod(new List<string> { "Get" })
                            .WithUpstreamTemplatePattern(new UpstreamPathTemplate("someUpstreamPath", 1, false))
                            .Build())
                        .WithUpstreamPathTemplate("someUpstreamPath")
                        .WithUpstreamHttpMethod(new List<string> { "Get" })
                        .WithUpstreamTemplatePattern(new UpstreamPathTemplate("someUpstreamPath", 1, false))
                        .Build()
                }, string.Empty, serviceProviderConfig
                ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Get"))
                .When(x => x.WhenICallTheFinder())
                .Then(
                    x => x.ThenTheFollowingIsReturned(new DownstreamRoute(
                            new List<PlaceholderNameAndValue>(),
                            new ReRouteBuilder()
                                .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                                    .WithDownstreamPathTemplate("someDownstreamPath")
                                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                                    .WithUpstreamTemplatePattern(new UpstreamPathTemplate("someUpstreamPath", 1, false))
                                    .Build())
                                .WithUpstreamHttpMethod(new List<string> { "Get" })
                                .WithUpstreamTemplatePattern(new UpstreamPathTemplate("someUpstreamPath", 1, false))
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
                .And(x => x.GivenTheConfigurationIs(new List<ReRoute>
                {
                    new ReRouteBuilder()
                        .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                            .WithDownstreamPathTemplate("someDownstreamPath")
                            .WithUpstreamPathTemplate("someUpstreamPath")
                            .WithUpstreamHttpMethod(new List<string> { "Get" })
                            .WithUpstreamTemplatePattern(new UpstreamPathTemplate("someUpstreamPath", 1, false))
                            .Build())
                        .WithUpstreamPathTemplate("someUpstreamPath")
                        .WithUpstreamHttpMethod(new List<string> { "Get" })
                        .WithUpstreamTemplatePattern(new UpstreamPathTemplate("someUpstreamPath", 1, false))
                        .Build()
                }, string.Empty, serviceProviderConfig
                    ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Get"))
                .When(x => x.WhenICallTheFinder())
                .Then(
                    x => x.ThenTheFollowingIsReturned(new DownstreamRoute(new List<PlaceholderNameAndValue>(),
                        new ReRouteBuilder()
                            .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPath")
                                .WithUpstreamHttpMethod(new List<string> { "Get" })
                                .WithUpstreamTemplatePattern(new UpstreamPathTemplate("someUpstreamPath", 1, false))
                                .Build())
                            .WithUpstreamHttpMethod(new List<string> { "Get" })
                            .WithUpstreamTemplatePattern(new UpstreamPathTemplate("someUpstreamPath", 1, false))
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
                .And(x => x.GivenTheConfigurationIs(new List<ReRoute>
                {
                    new ReRouteBuilder()
                        .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                            .WithDownstreamPathTemplate("someDownstreamPath")
                            .WithUpstreamPathTemplate("someUpstreamPath")
                            .WithUpstreamHttpMethod(new List<string> { "Get" })
                            .WithUpstreamTemplatePattern(new UpstreamPathTemplate("", 1, false))
                            .Build())
                        .WithUpstreamPathTemplate("someUpstreamPath")
                        .WithUpstreamHttpMethod(new List<string> { "Get" })
                        .WithUpstreamTemplatePattern(new UpstreamPathTemplate("", 1, false))
                        .Build(),
                    new ReRouteBuilder()
                        .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                            .WithDownstreamPathTemplate("someDownstreamPathForAPost")
                            .WithUpstreamPathTemplate("someUpstreamPath")
                            .WithUpstreamHttpMethod(new List<string> { "Post" })
                            .WithUpstreamTemplatePattern(new UpstreamPathTemplate("", 1, false))
                            .Build())
                        .WithUpstreamPathTemplate("someUpstreamPath")
                        .WithUpstreamHttpMethod(new List<string> { "Post" })
                        .WithUpstreamTemplatePattern(new UpstreamPathTemplate("", 1, false))
                        .Build()
                }, string.Empty, serviceProviderConfig
                    ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Post"))
                .When(x => x.WhenICallTheFinder())
                .Then(
                    x => x.ThenTheFollowingIsReturned(new DownstreamRoute(new List<PlaceholderNameAndValue>(),
                        new ReRouteBuilder()
                            .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPathForAPost")
                                .WithUpstreamHttpMethod(new List<string> { "Post" })
                                .WithUpstreamTemplatePattern(new UpstreamPathTemplate("", 1, false))
                                .Build())
                            .WithUpstreamHttpMethod(new List<string> { "Post" })
                            .WithUpstreamTemplatePattern(new UpstreamPathTemplate("", 1, false))
                            .Build()
                        )))
                .BDDfy();
        }

        [Fact]
        public void should_not_return_route()
        {
            var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();

            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("dontMatchPath/"))
                 .And(x => x.GivenTheConfigurationIs(new List<ReRoute>
                     {
                        new ReRouteBuilder()
                        .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                                .WithDownstreamPathTemplate("somPath")
                                .WithUpstreamPathTemplate("somePath")
                                .WithUpstreamHttpMethod(new List<string> { "Get" })
                                .WithUpstreamTemplatePattern(new UpstreamPathTemplate("somePath", 1, false))
                                .Build())
                        .WithUpstreamPathTemplate("somePath")
                        .WithUpstreamHttpMethod(new List<string> { "Get" })
                        .WithUpstreamTemplatePattern(new UpstreamPathTemplate("somePath", 1, false))
                        .Build(),
                     }, string.Empty, serviceProviderConfig
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
            var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();

            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("someUpstreamPath"))
                .And(
                    x =>
                        x.GivenTheTemplateVariableAndNameFinderReturns(
                            new OkResponse<List<PlaceholderNameAndValue>>(new List<PlaceholderNameAndValue>())))
                .And(x => x.GivenTheConfigurationIs(new List<ReRoute>
                {
                    new ReRouteBuilder()
                        .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                            .WithDownstreamPathTemplate("someDownstreamPath")
                            .WithUpstreamPathTemplate("someUpstreamPath")
                            .WithUpstreamHttpMethod(new List<string> { "Get", "Post" })
                            .WithUpstreamTemplatePattern(new UpstreamPathTemplate("", 1, false))
                            .Build())
                        .WithUpstreamPathTemplate("someUpstreamPath")
                        .WithUpstreamHttpMethod(new List<string> { "Get", "Post" })
                        .WithUpstreamTemplatePattern(new UpstreamPathTemplate("", 1, false))
                        .Build()
                }, string.Empty, serviceProviderConfig
                    ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Post"))
                .When(x => x.WhenICallTheFinder())
                .Then(
                    x => x.ThenTheFollowingIsReturned(new DownstreamRoute(new List<PlaceholderNameAndValue>(),
                        new ReRouteBuilder()
                            .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPath")
                                .WithUpstreamHttpMethod(new List<string> { "Post" })
                                .WithUpstreamTemplatePattern(new UpstreamPathTemplate("", 1, false))
                                .Build())
                            .WithUpstreamHttpMethod(new List<string> { "Post" })
                            .WithUpstreamTemplatePattern(new UpstreamPathTemplate("", 1, false))
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
                .And(x => x.GivenTheConfigurationIs(new List<ReRoute>
                {
                    new ReRouteBuilder()
                        .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                            .WithDownstreamPathTemplate("someDownstreamPath")
                            .WithUpstreamPathTemplate("someUpstreamPath")
                            .WithUpstreamHttpMethod(new List<string>())
                            .WithUpstreamTemplatePattern(new UpstreamPathTemplate("", 1, false))
                            .Build())
                        .WithUpstreamPathTemplate("someUpstreamPath")
                        .WithUpstreamHttpMethod(new List<string>())
                        .WithUpstreamTemplatePattern(new UpstreamPathTemplate("", 1, false))
                        .Build()
                }, string.Empty, serviceProviderConfig
                    ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Post"))
                .When(x => x.WhenICallTheFinder())
                .Then(
                    x => x.ThenTheFollowingIsReturned(new DownstreamRoute(new List<PlaceholderNameAndValue>(),
                        new ReRouteBuilder()
                            .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPath")
                                .WithUpstreamHttpMethod(new List<string> { "Post" })
                                .WithUpstreamTemplatePattern(new UpstreamPathTemplate("", 1, false))
                                .Build())
                            .WithUpstreamHttpMethod(new List<string> { "Post" })
                            .WithUpstreamTemplatePattern(new UpstreamPathTemplate("", 1, false))
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
                .And(x => x.GivenTheConfigurationIs(new List<ReRoute>
                {
                    new ReRouteBuilder()
                        .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                            .WithDownstreamPathTemplate("someDownstreamPath")
                            .WithUpstreamPathTemplate("someUpstreamPath")
                            .WithUpstreamHttpMethod(new List<string> { "Get", "Patch", "Delete" })
                            .WithUpstreamTemplatePattern(new UpstreamPathTemplate("", 1, false))
                            .Build())
                        .WithUpstreamPathTemplate("someUpstreamPath")
                        .WithUpstreamHttpMethod(new List<string> { "Get", "Patch", "Delete" })
                        .WithUpstreamTemplatePattern(new UpstreamPathTemplate("", 1, false))
                        .Build()
                }, string.Empty, serviceProviderConfig
                    ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
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
                .And(x => x.GivenTheConfigurationIs(new List<ReRoute>
                    {
                        new ReRouteBuilder()
                            .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPath")
                                .WithUpstreamPathTemplate("someUpstreamPath")
                                .WithUpstreamHttpMethod(new List<string> { "Get" })
                                .WithUpstreamTemplatePattern(new UpstreamPathTemplate("someUpstreamPath", 1, false))
                                .WithUpstreamHost("MATCH")
                                .Build())
                            .WithUpstreamPathTemplate("someUpstreamPath")
                            .WithUpstreamHttpMethod(new List<string> { "Get" })
                            .WithUpstreamTemplatePattern(new UpstreamPathTemplate("someUpstreamPath", 1, false))
                            .WithUpstreamHost("MATCH")
                            .Build()
                    }, string.Empty, serviceProviderConfig
                ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Get"))
                .When(x => x.WhenICallTheFinder())
                .Then(
                    x => x.ThenTheFollowingIsReturned(new DownstreamRoute(
                        new List<PlaceholderNameAndValue>(),
                        new ReRouteBuilder()
                            .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPath")
                                .WithUpstreamHttpMethod(new List<string> { "Get" })
                                .WithUpstreamTemplatePattern(new UpstreamPathTemplate("someUpstreamPath", 1, false))
                                .Build())
                            .WithUpstreamHttpMethod(new List<string> { "Get" })
                            .WithUpstreamTemplatePattern(new UpstreamPathTemplate("someUpstreamPath", 1, false))
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
                .And(x => x.GivenTheConfigurationIs(new List<ReRoute>
                    {
                        new ReRouteBuilder()
                            .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPath")
                                .WithUpstreamPathTemplate("someUpstreamPath")
                                .WithUpstreamHttpMethod(new List<string> { "Get" })
                                .WithUpstreamTemplatePattern(new UpstreamPathTemplate("someUpstreamPath", 1, false))
                                .Build())
                            .WithUpstreamPathTemplate("someUpstreamPath")
                            .WithUpstreamHttpMethod(new List<string> { "Get" })
                            .WithUpstreamTemplatePattern(new UpstreamPathTemplate("someUpstreamPath", 1, false))
                            .Build()
                    }, string.Empty, serviceProviderConfig
                ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Get"))
                .When(x => x.WhenICallTheFinder())
                .Then(
                    x => x.ThenTheFollowingIsReturned(new DownstreamRoute(
                        new List<PlaceholderNameAndValue>(),
                        new ReRouteBuilder()
                            .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPath")
                                .WithUpstreamHttpMethod(new List<string> { "Get" })
                                .WithUpstreamTemplatePattern(new UpstreamPathTemplate("someUpstreamPath", 1, false))
                                .Build())
                            .WithUpstreamHttpMethod(new List<string> { "Get" })
                            .WithUpstreamTemplatePattern(new UpstreamPathTemplate("someUpstreamPath", 1, false))
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
                .And(x => x.GivenTheConfigurationIs(new List<ReRoute>
                    {
                        new ReRouteBuilder()
                            .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPath")
                                .WithUpstreamPathTemplate("someUpstreamPath")
                                .WithUpstreamHttpMethod(new List<string> { "Get" })
                                .WithUpstreamTemplatePattern(new UpstreamPathTemplate("someUpstreamPath", 1, false))
                                .WithUpstreamHost("MATCH")
                                .Build())
                            .WithUpstreamPathTemplate("someUpstreamPath")
                            .WithUpstreamHttpMethod(new List<string> { "Get" })
                            .WithUpstreamTemplatePattern(new UpstreamPathTemplate("someUpstreamPath", 1, false))
                            .WithUpstreamHost("MATCH")
                            .Build(),
                        new ReRouteBuilder()
                            .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPath")
                                .WithUpstreamPathTemplate("someUpstreamPath")
                                .WithUpstreamHttpMethod(new List<string> { }) // empty list of methods
                                .WithUpstreamTemplatePattern(new UpstreamPathTemplate("someUpstreamPath", 1, false))
                                .WithUpstreamHost("MATCH")
                                .Build())
                            .WithUpstreamPathTemplate("someUpstreamPath")
                            .WithUpstreamHttpMethod(new List<string> { }) // empty list of methods
                            .WithUpstreamTemplatePattern(new UpstreamPathTemplate("someUpstreamPath", 1, false))
                            .WithUpstreamHost("MATCH")
                            .Build()
                    }, string.Empty, serviceProviderConfig
                ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
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
                .And(x => x.GivenTheConfigurationIs(new List<ReRoute>
                    {
                        new ReRouteBuilder()
                            .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPath")
                                .WithUpstreamPathTemplate("someUpstreamPath")
                                .WithUpstreamHttpMethod(new List<string>())
                                .WithUpstreamTemplatePattern(new UpstreamPathTemplate("someUpstreamPath", 1, false))
                                .WithUpstreamHost("MATCH")
                                .Build())
                            .WithUpstreamPathTemplate("someUpstreamPath")
                            .WithUpstreamHttpMethod(new List<string>())
                            .WithUpstreamTemplatePattern(new UpstreamPathTemplate("someUpstreamPath", 1, false))
                            .WithUpstreamHost("MATCH")
                            .Build()
                    }, string.Empty, serviceProviderConfig
                ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
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
                .And(x => x.GivenTheConfigurationIs(new List<ReRoute>
                    {
                        new ReRouteBuilder()
                            .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPath")
                                .WithUpstreamPathTemplate("someUpstreamPath")
                                .WithUpstreamHttpMethod(new List<string>())
                                .WithUpstreamTemplatePattern(new UpstreamPathTemplate("someUpstreamPath", 1, false))
                                .WithUpstreamHost("MATCH")
                                .Build())
                            .WithUpstreamPathTemplate("someUpstreamPath")
                            .WithUpstreamHttpMethod(new List<string>())
                            .WithUpstreamTemplatePattern(new UpstreamPathTemplate("someUpstreamPath", 1, false))
                            .WithUpstreamHost("MATCH")
                            .Build()
                    }, string.Empty, serviceProviderConfig
                ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Get"))
                .When(x => x.WhenICallTheFinder())
                .And(x => x.ThenTheUrlMatcherIsCalledCorrectly(1))
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
                .And(x => x.GivenTheConfigurationIs(new List<ReRoute>
                    {
                        new ReRouteBuilder()
                            .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                                .WithDownstreamPathTemplate("THENULLPATH")
                                .WithUpstreamPathTemplate("someUpstreamPath")
                                .WithUpstreamHttpMethod(new List<string> { "Get" })
                                .WithUpstreamTemplatePattern(new UpstreamPathTemplate("someUpstreamPath", 1, false))
                                .Build())
                            .WithUpstreamPathTemplate("someUpstreamPath")
                            .WithUpstreamHttpMethod(new List<string> { "Get" })
                            .WithUpstreamTemplatePattern(new UpstreamPathTemplate("someUpstreamPath", 1, false))
                            .Build(),
                        new ReRouteBuilder()
                            .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPath")
                                .WithUpstreamPathTemplate("someUpstreamPath")
                                .WithUpstreamHttpMethod(new List<string> { "Get" })
                                .WithUpstreamTemplatePattern(new UpstreamPathTemplate("someUpstreamPath", 1, false))
                                .WithUpstreamHost("MATCH")
                                .Build())
                            .WithUpstreamPathTemplate("someUpstreamPath")
                            .WithUpstreamHttpMethod(new List<string> { "Get" })
                            .WithUpstreamTemplatePattern(new UpstreamPathTemplate("someUpstreamPath", 1, false))
                            .WithUpstreamHost("MATCH")
                            .Build()
                    }, string.Empty, serviceProviderConfig
                ))
                .And(x => x.GivenTheUrlMatcherReturns(new OkResponse<UrlMatch>(new UrlMatch(true))))
                .And(x => x.GivenTheUpstreamHttpMethodIs("Get"))
                .When(x => x.WhenICallTheFinder())
                .Then(
                    x => x.ThenTheFollowingIsReturned(new DownstreamRoute(
                        new List<PlaceholderNameAndValue>(),
                        new ReRouteBuilder()
                            .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                                .WithDownstreamPathTemplate("someDownstreamPath")
                                .WithUpstreamHttpMethod(new List<string> { "Get" })
                                .WithUpstreamTemplatePattern(new UpstreamPathTemplate("someUpstreamPath", 1, false))
                                .Build())
                            .WithUpstreamHttpMethod(new List<string> { "Get" })
                            .WithUpstreamTemplatePattern(new UpstreamPathTemplate("someUpstreamPath", 1, false))
                            .Build()
                    )))
                .And(x => x.ThenTheUrlMatcherIsCalledCorrectly(2))
                .BDDfy();
        }

        private void GivenTheUpstreamHostIs(string upstreamHost)
        {
            _upstreamHost = upstreamHost;
        }

        private void GivenTheTemplateVariableAndNameFinderReturns(Response<List<PlaceholderNameAndValue>> response)
        {
            _finder
                .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
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
                .Verify(x => x.Match(_upstreamUrlPath, _upstreamQuery, _reRoutesConfig[0].UpstreamPathTemplate.Value, _reRoutesConfig[0].UpstreamTemplatePattern.ContainsQueryString), Times.Once);
        }

        private void ThenTheUrlMatcherIsCalledCorrectly(int times)
        {
            _mockMatcher
                .Verify(x => x.Match(_upstreamUrlPath, _upstreamQuery, _reRoutesConfig[0].UpstreamPathTemplate.Value, _reRoutesConfig[0].UpstreamTemplatePattern.ContainsQueryString), Times.Exactly(times));
        }

        private void ThenTheUrlMatcherIsCalledCorrectly(string expectedUpstreamUrlPath)
        {
            _mockMatcher
                .Verify(x => x.Match(expectedUpstreamUrlPath, _upstreamQuery, _reRoutesConfig[0].UpstreamPathTemplate.Value, _reRoutesConfig[0].UpstreamTemplatePattern.ContainsQueryString), Times.Once);
        }

        private void ThenTheUrlMatcherIsNotCalled()
        {
            _mockMatcher
                .Verify(x => x.Match(_upstreamUrlPath, _upstreamQuery, _reRoutesConfig[0].UpstreamPathTemplate.Value, _reRoutesConfig[0].UpstreamTemplatePattern.ContainsQueryString), Times.Never);
        }

        private void GivenTheUrlMatcherReturns(Response<UrlMatch> match)
        {
            _match = match;
            _mockMatcher
                .Setup(x => x.Match(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(_match);
        }

        private void GivenTheConfigurationIs(List<ReRoute> reRoutesConfig, string adminPath, ServiceProviderConfiguration serviceProviderConfig)
        {
            _reRoutesConfig = reRoutesConfig;
            _config = new InternalConfiguration(_reRoutesConfig, adminPath, serviceProviderConfig, "", null, new LoadBalancerOptionsBuilder().Build(), "", new QoSOptionsBuilder().Build(), new RateLimitGlobalOptionsBuilder().Build(), new HttpHandlerOptionsBuilder().Build());
        }

        private void GivenThereIsAnUpstreamUrlPath(string upstreamUrlPath)
        {
            _upstreamUrlPath = upstreamUrlPath;
        }

        private void WhenICallTheFinder()
        {
            _result = _downstreamRouteFinder.GetAsync(_upstreamUrlPath, _upstreamQuery, _upstreamHttpMethod, _config, _upstreamHost).GetAwaiter().GetResult();
        }

        private void ThenTheFollowingIsReturned(DownstreamRoute expected)
        {
            _result.Data.ReRoute.DownstreamReRoute[0].DownstreamPathTemplate.Value.ShouldBe(expected.ReRoute.DownstreamReRoute[0].DownstreamPathTemplate.Value);
            _result.Data.ReRoute.UpstreamTemplatePattern.Priority.ShouldBe(expected.ReRoute.UpstreamTemplatePattern.Priority);

            for (int i = 0; i < _result.Data.TemplatePlaceholderNameAndValues.Count; i++)
            {
                _result.Data.TemplatePlaceholderNameAndValues[i].Name.ShouldBe(expected.TemplatePlaceholderNameAndValues[i].Name);
                _result.Data.TemplatePlaceholderNameAndValues[i].Value.ShouldBe(expected.TemplatePlaceholderNameAndValues[i].Value);
            }

            _result.IsError.ShouldBeFalse();
        }
    }
}
