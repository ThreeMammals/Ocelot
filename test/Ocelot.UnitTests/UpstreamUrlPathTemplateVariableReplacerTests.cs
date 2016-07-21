using System.Collections.Generic;
using Ocelot.Library.Infrastructure.UrlPathMatcher;
using Ocelot.Library.Infrastructure.UrlPathReplacer;
using Shouldly;
using Xunit;

namespace Ocelot.UnitTests
{
    public class UpstreamUrlPathTemplateVariableReplacerTests
    {

        private string _upstreamUrlPath;
        private UrlPathMatch _urlPathMatch;
        private string _result;
        private IUpstreamUrlPathTemplateVariableReplacer _upstreamUrlPathReplacer;

        public UpstreamUrlPathTemplateVariableReplacerTests()
        {
            _upstreamUrlPathReplacer = new UpstreamUrlPathTemplateVariableReplacer();
        }
        [Fact]
        public void can_replace_no_template_variables()
        {
            GivenThereIsAnUpstreamUrlPath("");
            GivenThereIsAUrlPathMatch(new UrlPathMatch(true, new List<TemplateVariableNameAndValue>(), ""));
            WhenIReplaceTheTemplateVariables();
            ThenTheUpstreamUrlPathIsReturned("");
        }

        [Fact]
        public void can_replace_url_no_slash()
        {
            GivenThereIsAnUpstreamUrlPath("api");
            GivenThereIsAUrlPathMatch(new UrlPathMatch(true, new List<TemplateVariableNameAndValue>(), "api"));
            WhenIReplaceTheTemplateVariables();
            ThenTheUpstreamUrlPathIsReturned("api");
        }

        [Fact]
        public void can_replace_url_one_slash()
        {
            GivenThereIsAnUpstreamUrlPath("api/");
            GivenThereIsAUrlPathMatch(new UrlPathMatch(true, new List<TemplateVariableNameAndValue>(), "api/"));
            WhenIReplaceTheTemplateVariables();
            ThenTheUpstreamUrlPathIsReturned("api/");
        }

        [Fact]
        public void can_replace_url_multiple_slash()
        {
            GivenThereIsAnUpstreamUrlPath("api/product/products/");
            GivenThereIsAUrlPathMatch(new UrlPathMatch(true, new List<TemplateVariableNameAndValue>(), "api/product/products/"));
            WhenIReplaceTheTemplateVariables();
            ThenTheUpstreamUrlPathIsReturned("api/product/products/");
        }

        [Fact]
        public void can_replace_url_one_template_variable()
        {
            var templateVariables = new List<TemplateVariableNameAndValue>()
            {
                new TemplateVariableNameAndValue("{productId}", "1")
            };

            GivenThereIsAnUpstreamUrlPath("productservice/products/{productId}/");
            GivenThereIsAUrlPathMatch(new UrlPathMatch(true, templateVariables, "api/products/{productId}/"));
            WhenIReplaceTheTemplateVariables();
            ThenTheUpstreamUrlPathIsReturned("productservice/products/1/");
        }

        [Fact]
        public void can_replace_url_one_template_variable_with_path_after()
        {
            var templateVariables = new List<TemplateVariableNameAndValue>()
            {
                new TemplateVariableNameAndValue("{productId}", "1")
            };

            GivenThereIsAnUpstreamUrlPath("productservice/products/{productId}/variants");
            GivenThereIsAUrlPathMatch(new UrlPathMatch(true, templateVariables, "api/products/{productId}/"));
            WhenIReplaceTheTemplateVariables();
            ThenTheUpstreamUrlPathIsReturned("productservice/products/1/variants");
        }

        [Fact]
        public void can_replace_url_two_template_variable()
        {
            var templateVariables = new List<TemplateVariableNameAndValue>()
            {
                new TemplateVariableNameAndValue("{productId}", "1"),
                new TemplateVariableNameAndValue("{variantId}", "12")
            };

            GivenThereIsAnUpstreamUrlPath("productservice/products/{productId}/variants/{variantId}");
            GivenThereIsAUrlPathMatch(new UrlPathMatch(true, templateVariables, "api/products/{productId}/{variantId}"));
            WhenIReplaceTheTemplateVariables();
            ThenTheUpstreamUrlPathIsReturned("productservice/products/1/variants/12");
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

            GivenThereIsAnUpstreamUrlPath("productservice/category/{categoryId}/products/{productId}/variants/{variantId}");
            GivenThereIsAUrlPathMatch(new UrlPathMatch(true, templateVariables, "api/products/{categoryId}/{productId}/{variantId}"));
            WhenIReplaceTheTemplateVariables();
            ThenTheUpstreamUrlPathIsReturned("productservice/category/34/products/1/variants/12");
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
