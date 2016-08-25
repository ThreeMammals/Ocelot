using System.Collections.Generic;
using Ocelot.Library.Infrastructure.UrlPathMatcher;
using Ocelot.Library.Infrastructure.UrlPathReplacer;
using Shouldly;
using Xunit;

namespace Ocelot.UnitTests
{
    using TestStack.BDDfy;

    public class UpstreamUrlPathTemplateVariableReplacerTests
    {
        private string _upstreamUrlPath;
        private UrlPathMatch _urlPathMatch;
        private string _result;
        private readonly IUpstreamUrlPathTemplateVariableReplacer _upstreamUrlPathReplacer;

        public UpstreamUrlPathTemplateVariableReplacerTests()
        {
            _upstreamUrlPathReplacer = new UpstreamUrlPathTemplateVariableReplacer();
        }

        [Fact]
        public void can_replace_no_template_variables()
        {
            this.Given(x => x.GivenThereIsAnUpstreamUrlPath(""))
                .And(x => x.GivenThereIsAUrlPathMatch(new UrlPathMatch(true, new List<TemplateVariableNameAndValue>(), "")))
                .When(x => x.WhenIReplaceTheTemplateVariables())
                .Then(x => x.ThenTheUpstreamUrlPathIsReturned(""))
                .BDDfy();
        }

        [Fact]
        public void can_replace_url_no_slash()
        {
            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("api"))
                .And(x => x.GivenThereIsAUrlPathMatch(new UrlPathMatch(true, new List<TemplateVariableNameAndValue>(), "api")))
                .When(x => x.WhenIReplaceTheTemplateVariables())
                .Then(x => x.ThenTheUpstreamUrlPathIsReturned("api"))
                .BDDfy();
        }

        [Fact]
        public void can_replace_url_one_slash()
        {
            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("api/"))
                .And(x => x.GivenThereIsAUrlPathMatch(new UrlPathMatch(true, new List<TemplateVariableNameAndValue>(), "api/")))
                .When(x => x.WhenIReplaceTheTemplateVariables())
                .Then(x => x.ThenTheUpstreamUrlPathIsReturned("api/"))
                .BDDfy();
        }

        [Fact]
        public void can_replace_url_multiple_slash()
        {
            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("api/product/products/"))
                .And(x => x.GivenThereIsAUrlPathMatch(new UrlPathMatch(true, new List<TemplateVariableNameAndValue>(), "api/product/products/")))
                .When(x => x.WhenIReplaceTheTemplateVariables())
                .Then(x => x.ThenTheUpstreamUrlPathIsReturned("api/product/products/"))
                .BDDfy();
        }

        [Fact]
        public void can_replace_url_one_template_variable()
        {
            var templateVariables = new List<TemplateVariableNameAndValue>()
            {
                new TemplateVariableNameAndValue("{productId}", "1")
            };

            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("productservice/products/{productId}/"))
             .And(x => x.GivenThereIsAUrlPathMatch(new UrlPathMatch(true, templateVariables, "api/products/{productId}/")))
             .When(x => x.WhenIReplaceTheTemplateVariables())
             .Then(x => x.ThenTheUpstreamUrlPathIsReturned("productservice/products/1/"))
             .BDDfy();
        }

        [Fact]
        public void can_replace_url_one_template_variable_with_path_after()
        {
            var templateVariables = new List<TemplateVariableNameAndValue>()
            {
                new TemplateVariableNameAndValue("{productId}", "1")
            };

            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("productservice/products/{productId}/variants"))
             .And(x => x.GivenThereIsAUrlPathMatch(new UrlPathMatch(true, templateVariables, "api/products/{productId}/")))
             .When(x => x.WhenIReplaceTheTemplateVariables())
             .Then(x => x.ThenTheUpstreamUrlPathIsReturned("productservice/products/1/variants"))
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

            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("productservice/products/{productId}/variants/{variantId}"))
             .And(x => x.GivenThereIsAUrlPathMatch(new UrlPathMatch(true, templateVariables, "api/products/{productId}/{variantId}")))
             .When(x => x.WhenIReplaceTheTemplateVariables())
             .Then(x => x.ThenTheUpstreamUrlPathIsReturned("productservice/products/1/variants/12"))
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

            this.Given(x => x.GivenThereIsAnUpstreamUrlPath("productservice/category/{categoryId}/products/{productId}/variants/{variantId}"))
             .And(x => x.GivenThereIsAUrlPathMatch(new UrlPathMatch(true, templateVariables, "api/products/{categoryId}/{productId}/{variantId}")))
             .When(x => x.WhenIReplaceTheTemplateVariables())
             .Then(x => x.ThenTheUpstreamUrlPathIsReturned("productservice/category/34/products/1/variants/12"))
             .BDDfy();
        }

        private void GivenThereIsAnUpstreamUrlPath(string upstreamUrlPath)
        {
            _upstreamUrlPath = upstreamUrlPath;
        }

        private void GivenThereIsAUrlPathMatch(UrlPathMatch urlPathMatch)
        {
            _urlPathMatch = urlPathMatch;
        }

        private void WhenIReplaceTheTemplateVariables()
        {
            _result = _upstreamUrlPathReplacer.ReplaceTemplateVariable(_upstreamUrlPath, _urlPathMatch);
        }

        private void ThenTheUpstreamUrlPathIsReturned(string expected)
        {
            _result.ShouldBe(expected);
        }

    }
}
