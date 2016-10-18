using System.Collections.Generic;
using Ocelot.Library.Configuration.Builder;
using Ocelot.Library.DownstreamRouteFinder;
using Ocelot.Library.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Library.DownstreamUrlCreator.UrlTemplateReplacer;
using Ocelot.Library.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.DownstreamUrlCreator.UrlTemplateReplacer
{
    public class UpstreamUrlPathTemplateVariableReplacerTests
    {
        private DownstreamRoute _downstreamRoute;
        private Response<string> _result;
        private readonly IDownstreamUrlTemplateVariableReplacer _downstreamUrlPathReplacer;

        public UpstreamUrlPathTemplateVariableReplacerTests()
        {
            _downstreamUrlPathReplacer = new DownstreamUrlTemplateVariableReplacer();
        }

        [Fact]
        public void can_replace_no_template_variables()
        {
            this.Given(x => x.GivenThereIsAUrlMatch(new DownstreamRoute(new List<TemplateVariableNameAndValue>(), new ReRouteBuilder().Build())))
                .When(x => x.WhenIReplaceTheTemplateVariables())
                .Then(x => x.ThenTheDownstreamUrlPathIsReturned(""))
                .BDDfy();
        }

        [Fact]
        public void can_replace_no_template_variables_with_slash()
        {
            this.Given(x => x.GivenThereIsAUrlMatch(new DownstreamRoute(new List<TemplateVariableNameAndValue>(), new ReRouteBuilder().WithDownstreamTemplate("/").Build())))
                .When(x => x.WhenIReplaceTheTemplateVariables())
                .Then(x => x.ThenTheDownstreamUrlPathIsReturned("/"))
                .BDDfy();
        }

        [Fact]
        public void can_replace_url_no_slash()
        {
            this.Given(x => x.GivenThereIsAUrlMatch(new DownstreamRoute(new List<TemplateVariableNameAndValue>(), new ReRouteBuilder().WithDownstreamTemplate("api").Build())))
                .When(x => x.WhenIReplaceTheTemplateVariables())
                .Then(x => x.ThenTheDownstreamUrlPathIsReturned("api"))
                .BDDfy();
        }

        [Fact]
        public void can_replace_url_one_slash()
        {
            this.Given(x => x.GivenThereIsAUrlMatch(new DownstreamRoute(new List<TemplateVariableNameAndValue>(), new ReRouteBuilder().WithDownstreamTemplate("api/").Build())))
                .When(x => x.WhenIReplaceTheTemplateVariables())
                .Then(x => x.ThenTheDownstreamUrlPathIsReturned("api/"))
                .BDDfy();
        }

        [Fact]
        public void can_replace_url_multiple_slash()
        {
            this.Given(x => x.GivenThereIsAUrlMatch(new DownstreamRoute(new List<TemplateVariableNameAndValue>(), new ReRouteBuilder().WithDownstreamTemplate("api/product/products/").Build())))
                .When(x => x.WhenIReplaceTheTemplateVariables())
                .Then(x => x.ThenTheDownstreamUrlPathIsReturned("api/product/products/"))
                .BDDfy();
        }

        [Fact]
        public void can_replace_url_one_template_variable()
        {
            var templateVariables = new List<TemplateVariableNameAndValue>()
            {
                new TemplateVariableNameAndValue("{productId}", "1")
            };

            this.Given(x => x.GivenThereIsAUrlMatch(new DownstreamRoute(templateVariables, new ReRouteBuilder().WithDownstreamTemplate("productservice/products/{productId}/").Build())))
             .When(x => x.WhenIReplaceTheTemplateVariables())
             .Then(x => x.ThenTheDownstreamUrlPathIsReturned("productservice/products/1/"))
             .BDDfy();
        }

        [Fact]
        public void can_replace_url_one_template_variable_with_path_after()
        {
            var templateVariables = new List<TemplateVariableNameAndValue>()
            {
                new TemplateVariableNameAndValue("{productId}", "1")
            };

            this.Given(x => x.GivenThereIsAUrlMatch(new DownstreamRoute(templateVariables, new ReRouteBuilder().WithDownstreamTemplate("productservice/products/{productId}/variants").Build())))
             .When(x => x.WhenIReplaceTheTemplateVariables())
             .Then(x => x.ThenTheDownstreamUrlPathIsReturned("productservice/products/1/variants"))
             .BDDfy();
        }

        [Fact]
        public void can_replace_url_two_template_variable()
        {
            var templateVariables = new List<TemplateVariableNameAndValue>()
            {
                new TemplateVariableNameAndValue("{productId}", "1"),
                new TemplateVariableNameAndValue("{variantId}", "12")
            };

            this.Given(x => x.GivenThereIsAUrlMatch(new DownstreamRoute(templateVariables, new ReRouteBuilder().WithDownstreamTemplate("productservice/products/{productId}/variants/{variantId}").Build())))
             .When(x => x.WhenIReplaceTheTemplateVariables())
             .Then(x => x.ThenTheDownstreamUrlPathIsReturned("productservice/products/1/variants/12"))
             .BDDfy();
        }

           [Fact]
        public void can_replace_url_three_template_variable()
        {
            var templateVariables = new List<TemplateVariableNameAndValue>()
            {
                new TemplateVariableNameAndValue("{productId}", "1"),
                new TemplateVariableNameAndValue("{variantId}", "12"),
                new TemplateVariableNameAndValue("{categoryId}", "34")
            };

            this.Given(x => x.GivenThereIsAUrlMatch(new DownstreamRoute(templateVariables, new ReRouteBuilder().WithDownstreamTemplate("productservice/category/{categoryId}/products/{productId}/variants/{variantId}").Build())))
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
            _result = _downstreamUrlPathReplacer.ReplaceTemplateVariables(_downstreamRoute);
        }

        private void ThenTheDownstreamUrlPathIsReturned(string expected)
        {
            _result.Data.ShouldBe(expected);
        }
    }
}
