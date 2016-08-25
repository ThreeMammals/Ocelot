using System.Collections.Generic;
using System.Linq;
using Ocelot.Library.Infrastructure.UrlPathMatcher;
using Shouldly;
using Xunit;

namespace Ocelot.UnitTests
{
    using TestStack.BDDfy;

    public class UrlPathToUrlPathTemplateMatcherTests 
    {
        private readonly IUrlPathToUrlPathTemplateMatcher _urlMapper;
        private string _downstreamPath;
        private string _downstreamPathTemplate;
        private UrlPathMatch _result;
        public UrlPathToUrlPathTemplateMatcherTests()
        {
            _urlMapper = new UrlPathToUrlPathTemplateMatcher();
        }

        [Fact]
        public void can_match_down_stream_url()
        {
            this.Given(x => x.GivenIHaveADownstreamPath(""))
                .And(x => x.GivenIHaveAnDownstreamPathTemplate(""))
                .When(x => x.WhenIMatchThePaths())
                .And(x => x.ThenTheResultIsTrue())
                .And(x => x.ThenTheTemplatesDictionaryIs(new List<TemplateVariableNameAndValue>()))
                .And(x => x.ThenTheUrlPathTemplateIs(""))
                .BDDfy();
        }

        [Fact]
        public void can_match_down_stream_url_with_no_slash()
        {
            this.Given(x => x.GivenIHaveADownstreamPath("api"))
                 .Given(x => x.GivenIHaveAnDownstreamPathTemplate("api"))
                 .When(x => x.WhenIMatchThePaths())
                 .Then(x => x.ThenTheResultIsTrue())
                 .And(x => x.ThenTheTemplatesDictionaryIs(new List<TemplateVariableNameAndValue>()))
                 .And(x => x.ThenTheUrlPathTemplateIs("api"))
                 .BDDfy();
        }

         [Fact]
        public void can_match_down_stream_url_with_one_slash()
        {
            this.Given(x => x.GivenIHaveADownstreamPath("api/"))
                 .Given(x => x.GivenIHaveAnDownstreamPathTemplate("api/"))
                 .When(x => x.WhenIMatchThePaths())
                 .Then(x => x.ThenTheResultIsTrue())
                 .And(x => x.ThenTheTemplatesDictionaryIs(new List<TemplateVariableNameAndValue>()))
                 .And(x => x.ThenTheUrlPathTemplateIs("api/"))
                 .BDDfy();
        }

        [Fact]
        public void can_match_down_stream_url_with_downstream_template()
        {
            this.Given(x => x.GivenIHaveADownstreamPath("api/product/products/"))
              .Given(x => x.GivenIHaveAnDownstreamPathTemplate("api/product/products/"))
              .When(x => x.WhenIMatchThePaths())
              .Then(x => x.ThenTheResultIsTrue())
              .And(x => x.ThenTheTemplatesDictionaryIs(new List<TemplateVariableNameAndValue>()))
              .And(x => x.ThenTheUrlPathTemplateIs("api/product/products/"))
              .BDDfy();
        }

        [Fact]
        public void can_match_down_stream_url_with_downstream_template_with_one_query_string_parameter()
        {
            this.Given(x => x.GivenIHaveADownstreamPath("api/product/products/?soldout=false"))
              .Given(x => x.GivenIHaveAnDownstreamPathTemplate("api/product/products/"))
              .When(x => x.WhenIMatchThePaths())
              .Then(x => x.ThenTheResultIsTrue())
              .And(x => x.ThenTheTemplatesDictionaryIs(new List<TemplateVariableNameAndValue>()))
              .And(x => x.ThenTheUrlPathTemplateIs("api/product/products/"))
              .BDDfy();
        }

        [Fact]
        public void can_match_down_stream_url_with_downstream_template_with_one_query_string_parameter_and_one_template()
        {
            var expectedTemplates = new List<TemplateVariableNameAndValue> 
            {
                new TemplateVariableNameAndValue("{productId}", "1")
            };

            this.Given(x => x.GivenIHaveADownstreamPath("api/product/products/1/variants/?soldout=false"))
              .Given(x => x.GivenIHaveAnDownstreamPathTemplate("api/product/products/{productId}/variants/"))
              .When(x => x.WhenIMatchThePaths())
              .Then(x => x.ThenTheResultIsTrue())
              .And(x => x.ThenTheTemplatesDictionaryIs(expectedTemplates))
              .And(x => x.ThenTheUrlPathTemplateIs("api/product/products/{productId}/variants/"))
              .BDDfy();
        }

        [Fact]
        public void can_match_down_stream_url_with_downstream_template_with_one_place_holder()
        {
            var expectedTemplates = new List<TemplateVariableNameAndValue> 
            {
                new TemplateVariableNameAndValue("{productId}", "1")
            };

            this.Given(x => x.GivenIHaveADownstreamPath("api/product/products/1"))
               .Given(x => x.GivenIHaveAnDownstreamPathTemplate("api/product/products/{productId}"))
               .When(x => x.WhenIMatchThePaths())
               .Then(x => x.ThenTheResultIsTrue())
               .And(x => x.ThenTheTemplatesDictionaryIs(expectedTemplates))
               .And(x => x.ThenTheUrlPathTemplateIs("api/product/products/{productId}"))
               .BDDfy();
        }

        [Fact]
        public void can_match_down_stream_url_with_downstream_template_with_two_place_holders()
        {
            var expectedTemplates = new List<TemplateVariableNameAndValue> 
            {
                new TemplateVariableNameAndValue("{productId}", "1"),
                new TemplateVariableNameAndValue("{categoryId}", "2")
            };

            this.Given(x => x.GivenIHaveADownstreamPath("api/product/products/1/2"))
                 .Given(x => x.GivenIHaveAnDownstreamPathTemplate("api/product/products/{productId}/{categoryId}"))
                 .When(x => x.WhenIMatchThePaths())
                 .Then(x => x.ThenTheResultIsTrue())
                 .And(x => x.ThenTheTemplatesDictionaryIs(expectedTemplates))
                 .And(x => x.ThenTheUrlPathTemplateIs("api/product/products/{productId}/{categoryId}"))
                 .BDDfy();
        }

        [Fact]
        public void can_match_down_stream_url_with_downstream_template_with_two_place_holders_seperated_by_something()
        {
            var expectedTemplates = new List<TemplateVariableNameAndValue> 
            {
                new TemplateVariableNameAndValue("{productId}", "1"),
                new TemplateVariableNameAndValue("{categoryId}", "2")
            };

            this.Given(x => x.GivenIHaveADownstreamPath("api/product/products/1/categories/2"))
                .And(x => x.GivenIHaveAnDownstreamPathTemplate("api/product/products/{productId}/categories/{categoryId}"))
                .When(x => x.WhenIMatchThePaths())
                .Then(x => x.ThenTheResultIsTrue())
                .And(x => x.ThenTheTemplatesDictionaryIs(expectedTemplates))
                .And(x => x.ThenTheUrlPathTemplateIs("api/product/products/{productId}/categories/{categoryId}"))
                .BDDfy();
        }

        [Fact]
        public void can_match_down_stream_url_with_downstream_template_with_three_place_holders_seperated_by_something()
        {
            var expectedTemplates = new List<TemplateVariableNameAndValue> 
            {
                new TemplateVariableNameAndValue("{productId}", "1"),
                new TemplateVariableNameAndValue("{categoryId}", "2"),
                new TemplateVariableNameAndValue("{variantId}", "123")
            };

            this.Given(x => x.GivenIHaveADownstreamPath("api/product/products/1/categories/2/variant/123"))
                .And(x => x.GivenIHaveAnDownstreamPathTemplate("api/product/products/{productId}/categories/{categoryId}/variant/{variantId}"))
                .When(x => x.WhenIMatchThePaths())
                .Then(x => x.ThenTheResultIsTrue())
                .And(x => x.ThenTheTemplatesDictionaryIs(expectedTemplates))
                .And(x => x.ThenTheUrlPathTemplateIs("api/product/products/{productId}/categories/{categoryId}/variant/{variantId}"))
                .BDDfy();
        }

        [Fact]
        public void can_match_down_stream_url_with_downstream_template_with_three_place_holders()
        {
            var expectedTemplates = new List<TemplateVariableNameAndValue> 
            {
                new TemplateVariableNameAndValue("{productId}", "1"),
                new TemplateVariableNameAndValue("{categoryId}", "2")
            };

            this.Given(x => x.GivenIHaveADownstreamPath("api/product/products/1/categories/2/variant/"))
                 .And(x => x.GivenIHaveAnDownstreamPathTemplate("api/product/products/{productId}/categories/{categoryId}/variant/"))
                 .When(x => x.WhenIMatchThePaths())
                 .Then(x => x.ThenTheResultIsTrue())
                 .And(x => x.ThenTheTemplatesDictionaryIs(expectedTemplates))
                 .And(x => x.ThenTheUrlPathTemplateIs("api/product/products/{productId}/categories/{categoryId}/variant/"))
                 .BDDfy();
        }

        private void ThenTheTemplatesDictionaryIs(List<TemplateVariableNameAndValue> expectedResults)
        {
            foreach (var expectedResult in expectedResults)
            {
                var result = _result.TemplateVariableNameAndValues
                    .First(t => t.TemplateVariableName == expectedResult.TemplateVariableName);
                result.TemplateVariableValue.ShouldBe(expectedResult.TemplateVariableValue);
            }
        }

        private void ThenTheUrlPathTemplateIs(string expectedUrlPathTemplate)
        {
            _result.DownstreamUrlPathTemplate.ShouldBe(expectedUrlPathTemplate);
        }
        private void GivenIHaveADownstreamPath(string downstreamPath)
        {
            _downstreamPath = downstreamPath;
        }

        private void GivenIHaveAnDownstreamPathTemplate(string downstreamTemplate)
        {
            _downstreamPathTemplate = downstreamTemplate;
        }

        private void WhenIMatchThePaths()
        {
            _result = _urlMapper.Match(_downstreamPath, _downstreamPathTemplate);
        }

        private void ThenTheResultIsTrue()
        {
            _result.Match.ShouldBeTrue();
        }
    }
} 