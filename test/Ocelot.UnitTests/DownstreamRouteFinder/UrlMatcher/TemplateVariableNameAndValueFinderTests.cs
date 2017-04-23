using System.Collections.Generic;
using System.Linq;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.DownstreamRouteFinder.UrlMatcher
{
    public class UrlPathToUrlTemplateMatcherTests 
    {
        private readonly IUrlPathPlaceholderNameAndValueFinder _finder;
        private string _downstreamUrlPath;
        private string _downstreamPathTemplate;
        private Response<List<UrlPathPlaceholderNameAndValue>> _result;

        public UrlPathToUrlTemplateMatcherTests()
        {
            _finder = new UrlPathPlaceholderNameAndValueFinder();
        }

        [Fact]
        public void can_match_down_stream_url()
        {
            this.Given(x => x.GivenIHaveAUpstreamPath(""))
                .And(x => x.GivenIHaveAnUpstreamUrlTemplate(""))
                .When(x => x.WhenIFindTheUrlVariableNamesAndValues())
                .And(x => x.ThenTheTemplatesVariablesAre(new List<UrlPathPlaceholderNameAndValue>()))
                .BDDfy();
        }

        [Fact]
        public void can_match_down_stream_url_with_no_slash()
        {
            this.Given(x => x.GivenIHaveAUpstreamPath("api"))
                 .Given(x => x.GivenIHaveAnUpstreamUrlTemplate("api"))
                 .When(x => x.WhenIFindTheUrlVariableNamesAndValues())
                 .And(x => x.ThenTheTemplatesVariablesAre(new List<UrlPathPlaceholderNameAndValue>()))
                 .BDDfy();
        }

        [Fact]
        public void can_match_down_stream_url_with_one_slash()
        {
            this.Given(x => x.GivenIHaveAUpstreamPath("api/"))
                 .Given(x => x.GivenIHaveAnUpstreamUrlTemplate("api/"))
                 .When(x => x.WhenIFindTheUrlVariableNamesAndValues())
                 .And(x => x.ThenTheTemplatesVariablesAre(new List<UrlPathPlaceholderNameAndValue>()))
                 .BDDfy();
        }

        [Fact]
        public void can_match_down_stream_url_with_downstream_template()
        {
            this.Given(x => x.GivenIHaveAUpstreamPath("api/product/products/"))
              .Given(x => x.GivenIHaveAnUpstreamUrlTemplate("api/product/products/"))
              .When(x => x.WhenIFindTheUrlVariableNamesAndValues())
              .And(x => x.ThenTheTemplatesVariablesAre(new List<UrlPathPlaceholderNameAndValue>()))
              .BDDfy();
        }

        [Fact]
        public void can_match_down_stream_url_with_downstream_template_with_one_place_holder()
        {
            var expectedTemplates = new List<UrlPathPlaceholderNameAndValue> 
            {
                new UrlPathPlaceholderNameAndValue("{productId}", "1")
            };

            this.Given(x => x.GivenIHaveAUpstreamPath("api/product/products/1"))
               .Given(x => x.GivenIHaveAnUpstreamUrlTemplate("api/product/products/{productId}"))
               .When(x => x.WhenIFindTheUrlVariableNamesAndValues())
               .And(x => x.ThenTheTemplatesVariablesAre(expectedTemplates))
               .BDDfy();
        }

        [Fact]
        public void can_match_down_stream_url_with_downstream_template_with_two_place_holders()
        {
            var expectedTemplates = new List<UrlPathPlaceholderNameAndValue> 
            {
                new UrlPathPlaceholderNameAndValue("{productId}", "1"),
                new UrlPathPlaceholderNameAndValue("{categoryId}", "2")
            };

            this.Given(x => x.GivenIHaveAUpstreamPath("api/product/products/1/2"))
                 .Given(x => x.GivenIHaveAnUpstreamUrlTemplate("api/product/products/{productId}/{categoryId}"))
                 .When(x => x.WhenIFindTheUrlVariableNamesAndValues())
                 .And(x => x.ThenTheTemplatesVariablesAre(expectedTemplates))
                 .BDDfy();
        }

        [Fact]
        public void can_match_down_stream_url_with_downstream_template_with_two_place_holders_seperated_by_something()
        {
            var expectedTemplates = new List<UrlPathPlaceholderNameAndValue> 
            {
                new UrlPathPlaceholderNameAndValue("{productId}", "1"),
                new UrlPathPlaceholderNameAndValue("{categoryId}", "2")
            };

            this.Given(x => x.GivenIHaveAUpstreamPath("api/product/products/1/categories/2"))
                .And(x => x.GivenIHaveAnUpstreamUrlTemplate("api/product/products/{productId}/categories/{categoryId}"))
                .When(x => x.WhenIFindTheUrlVariableNamesAndValues())
                .And(x => x.ThenTheTemplatesVariablesAre(expectedTemplates))
                .BDDfy();
        }

        [Fact]
        public void can_match_down_stream_url_with_downstream_template_with_three_place_holders_seperated_by_something()
        {
            var expectedTemplates = new List<UrlPathPlaceholderNameAndValue> 
            {
                new UrlPathPlaceholderNameAndValue("{productId}", "1"),
                new UrlPathPlaceholderNameAndValue("{categoryId}", "2"),
                new UrlPathPlaceholderNameAndValue("{variantId}", "123")
            };

            this.Given(x => x.GivenIHaveAUpstreamPath("api/product/products/1/categories/2/variant/123"))
                .And(x => x.GivenIHaveAnUpstreamUrlTemplate("api/product/products/{productId}/categories/{categoryId}/variant/{variantId}"))
                .When(x => x.WhenIFindTheUrlVariableNamesAndValues())
                .And(x => x.ThenTheTemplatesVariablesAre(expectedTemplates))
                .BDDfy();
        }

        [Fact]
        public void can_match_down_stream_url_with_downstream_template_with_three_place_holders()
        {
            var expectedTemplates = new List<UrlPathPlaceholderNameAndValue> 
            {
                new UrlPathPlaceholderNameAndValue("{productId}", "1"),
                new UrlPathPlaceholderNameAndValue("{categoryId}", "2")
            };

            this.Given(x => x.GivenIHaveAUpstreamPath("api/product/products/1/categories/2/variant/"))
                 .And(x => x.GivenIHaveAnUpstreamUrlTemplate("api/product/products/{productId}/categories/{categoryId}/variant/"))
                 .When(x => x.WhenIFindTheUrlVariableNamesAndValues())
                 .And(x => x.ThenTheTemplatesVariablesAre(expectedTemplates))
                 .BDDfy();
        }

        [Fact]
        public void can_match_down_stream_url_with_downstream_template_with_place_holder_to_final_url_path()
        {
            var expectedTemplates = new List<UrlPathPlaceholderNameAndValue>
            {
                new UrlPathPlaceholderNameAndValue("{finalUrlPath}", "product/products/categories/"),
            };

            this.Given(x => x.GivenIHaveAUpstreamPath("api/product/products/categories/"))
                 .And(x => x.GivenIHaveAnUpstreamUrlTemplate("api/{finalUrlPath}/"))
                 .When(x => x.WhenIFindTheUrlVariableNamesAndValues())
                 .And(x => x.ThenTheTemplatesVariablesAre(expectedTemplates))
                 .BDDfy();
        }

        private void ThenTheTemplatesVariablesAre(List<UrlPathPlaceholderNameAndValue> expectedResults)
        {
            foreach (var expectedResult in expectedResults)
            {
                var result = _result.Data
                    .First(t => t.TemplateVariableName == expectedResult.TemplateVariableName);
                result.TemplateVariableValue.ShouldBe(expectedResult.TemplateVariableValue);
            }
        }

        private void GivenIHaveAUpstreamPath(string downstreamPath)
        {
            _downstreamUrlPath = downstreamPath;
        }

        private void GivenIHaveAnUpstreamUrlTemplate(string downstreamUrlTemplate)
        {
            _downstreamPathTemplate = downstreamUrlTemplate;
        }

        private void WhenIFindTheUrlVariableNamesAndValues()
        {
            _result = _finder.Find(_downstreamUrlPath, _downstreamPathTemplate);
        }
    }
} 