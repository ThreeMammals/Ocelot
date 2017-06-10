using System.Collections.Generic;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.DownstreamUrlCreator.UrlTemplateReplacer;
using Ocelot.Responses;
using Ocelot.Values;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.DownstreamUrlCreator.UrlTemplateReplacer
{
    public class UpstreamUrlPathTemplateVariableReplacerTests
    {
        private DownstreamRoute _downstreamRoute;
        private Response<DownstreamPath> _result;
        private readonly IDownstreamPathPlaceholderReplacer _downstreamPathReplacer;

        public UpstreamUrlPathTemplateVariableReplacerTests()
        {
            _downstreamPathReplacer = new DownstreamTemplatePathPlaceholderReplacer();
        }

        [Fact]
        public void can_replace_no_template_variables()
        {
            this.Given(x => x.GivenThereIsAUrlMatch(
                new DownstreamRoute(
                    new List<UrlPathPlaceholderNameAndValue>(), 
                    new ReRouteBuilder()
                        .WithUpstreamHttpMethod(new List<string> { "Get" })
                        .Build())))
                .When(x => x.WhenIReplaceTheTemplateVariables())
                .Then(x => x.ThenTheDownstreamUrlPathIsReturned(""))
                .BDDfy();
        }

        [Fact]
        public void can_replace_no_template_variables_with_slash()
        {
            this.Given(x => x.GivenThereIsAUrlMatch(
                new DownstreamRoute(
                new List<UrlPathPlaceholderNameAndValue>(), 
                new ReRouteBuilder()
                    .WithDownstreamPathTemplate("/")
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .Build())))
                .When(x => x.WhenIReplaceTheTemplateVariables())
                .Then(x => x.ThenTheDownstreamUrlPathIsReturned("/"))
                .BDDfy();
        }

        [Fact]
        public void can_replace_url_no_slash()
        {
            this.Given(x => x.GivenThereIsAUrlMatch(new DownstreamRoute(new List<UrlPathPlaceholderNameAndValue>(), 
                new ReRouteBuilder()
                    .WithDownstreamPathTemplate("api")
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .Build())))
                .When(x => x.WhenIReplaceTheTemplateVariables())
                .Then(x => x.ThenTheDownstreamUrlPathIsReturned("api"))
                .BDDfy();
        }

        [Fact]
        public void can_replace_url_one_slash()
        {
            this.Given(x => x.GivenThereIsAUrlMatch(new DownstreamRoute(new List<UrlPathPlaceholderNameAndValue>(), 
                new ReRouteBuilder()
                    .WithDownstreamPathTemplate("api/")
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .Build())))
                .When(x => x.WhenIReplaceTheTemplateVariables())
                .Then(x => x.ThenTheDownstreamUrlPathIsReturned("api/"))
                .BDDfy();
        }

        [Fact]
        public void can_replace_url_multiple_slash()
        {
            this.Given(x => x.GivenThereIsAUrlMatch(new DownstreamRoute(new List<UrlPathPlaceholderNameAndValue>(), 
                new ReRouteBuilder()
                    .WithDownstreamPathTemplate("api/product/products/")
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .Build())))
                .When(x => x.WhenIReplaceTheTemplateVariables())
                .Then(x => x.ThenTheDownstreamUrlPathIsReturned("api/product/products/"))
                .BDDfy();
        }

        [Fact]
        public void can_replace_url_one_template_variable()
        {
            var templateVariables = new List<UrlPathPlaceholderNameAndValue>()
            {
                new UrlPathPlaceholderNameAndValue("{productId}", "1")
            };

            this.Given(x => x.GivenThereIsAUrlMatch(new DownstreamRoute(templateVariables, 
                new ReRouteBuilder()
                    .WithDownstreamPathTemplate("productservice/products/{productId}/")
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .Build())))
             .When(x => x.WhenIReplaceTheTemplateVariables())
             .Then(x => x.ThenTheDownstreamUrlPathIsReturned("productservice/products/1/"))
             .BDDfy();
        }

        [Fact]
        public void can_replace_url_one_template_variable_with_path_after()
        {
            var templateVariables = new List<UrlPathPlaceholderNameAndValue>()
            {
                new UrlPathPlaceholderNameAndValue("{productId}", "1")
            };

            this.Given(x => x.GivenThereIsAUrlMatch(new DownstreamRoute(templateVariables, 
                new ReRouteBuilder()
                    .WithDownstreamPathTemplate("productservice/products/{productId}/variants")
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .Build())))
             .When(x => x.WhenIReplaceTheTemplateVariables())
             .Then(x => x.ThenTheDownstreamUrlPathIsReturned("productservice/products/1/variants"))
             .BDDfy();
        }

        [Fact]
        public void can_replace_url_two_template_variable()
        {
            var templateVariables = new List<UrlPathPlaceholderNameAndValue>()
            {
                new UrlPathPlaceholderNameAndValue("{productId}", "1"),
                new UrlPathPlaceholderNameAndValue("{variantId}", "12")
            };

            this.Given(x => x.GivenThereIsAUrlMatch(new DownstreamRoute(templateVariables, 
                new ReRouteBuilder()
                    .WithDownstreamPathTemplate("productservice/products/{productId}/variants/{variantId}")
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .Build())))
             .When(x => x.WhenIReplaceTheTemplateVariables())
             .Then(x => x.ThenTheDownstreamUrlPathIsReturned("productservice/products/1/variants/12"))
             .BDDfy();
        }

           [Fact]
        public void can_replace_url_three_template_variable()
        {
            var templateVariables = new List<UrlPathPlaceholderNameAndValue>()
            {
                new UrlPathPlaceholderNameAndValue("{productId}", "1"),
                new UrlPathPlaceholderNameAndValue("{variantId}", "12"),
                new UrlPathPlaceholderNameAndValue("{categoryId}", "34")
            };

            this.Given(x => x.GivenThereIsAUrlMatch(new DownstreamRoute(templateVariables, 
                new ReRouteBuilder()
                .WithDownstreamPathTemplate("productservice/category/{categoryId}/products/{productId}/variants/{variantId}")
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build())))
             .When(x => x.WhenIReplaceTheTemplateVariables())
             .Then(x => x.ThenTheDownstreamUrlPathIsReturned("productservice/category/34/products/1/variants/12"))
             .BDDfy();
        }

        private void GivenThereIsAUrlMatch(DownstreamRoute downstreamRoute)
        {
            _downstreamRoute = downstreamRoute;
        }

        private void WhenIReplaceTheTemplateVariables()
        {
            _result = _downstreamPathReplacer.Replace(_downstreamRoute.ReRoute.DownstreamPathTemplate, _downstreamRoute.TemplatePlaceholderNameAndValues);
        }

        private void ThenTheDownstreamUrlPathIsReturned(string expected)
        {
            _result.Data.Value.ShouldBe(expected);
        }
    }
}
