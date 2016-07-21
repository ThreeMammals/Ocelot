using System;
using System.Collections.Generic;
using System.Linq;
using Ocelot.Library.Infrastructure.UrlPathMatcher;
using Shouldly;
using Xunit;

namespace Ocelot.UnitTests
{
    public class UrlPathToUrlPathTemplateMatcherTests 
    {
        private IUrlPathToUrlPathTemplateMatcher _urlMapper;
        private string _downstreamPath;
        private string _downstreamPathTemplate;
        private UrlPathMatch _result;
        public UrlPathToUrlPathTemplateMatcherTests()
        {
            _urlMapper = new UrlPathToUrlPathTemplateMatcher();
        }

        [Fact]
        public void can_match_down_stream_url_with_downstream_template_with_one_query_string_parameter()
        {
            GivenIHaveADownstreamPath("api/product/products/?soldout=false");
            GivenIHaveAnDownstreamPathTemplate("api/product/products/");
            WhenIMatchThePaths();
            ThenTheResultIsTrue();
            ThenTheTemplatesDictionaryIs(new List<TemplateVariableNameAndValue>());
            ThenTheUrlPathTemplateIs("api/product/products/");
        }

        [Fact]
        public void can_match_down_stream_url_with_downstream_template_with_one_query_string_parameter_and_one_template()
        {
            var expectedTemplates = new List<TemplateVariableNameAndValue> 
            {
                new TemplateVariableNameAndValue("{productid}", "1")
            };
           
            GivenIHaveADownstreamPath("api/product/products/1/variants/?soldout=false");
            GivenIHaveAnDownstreamPathTemplate("api/product/products/{productId}/variants/");
            WhenIMatchThePaths();
            ThenTheResultIsTrue();
            ThenTheTemplatesDictionaryIs(expectedTemplates);
            ThenTheUrlPathTemplateIs("api/product/products/{productId}/variants/");
        }

        [Fact]
        public void can_match_down_stream_url_with_downstream_template_with_one_place_holder()
        {
            var expectedTemplates = new List<TemplateVariableNameAndValue> 
            {
                new TemplateVariableNameAndValue("{productid}", "1")
            };

            GivenIHaveADownstreamPath("api/product/products/1");
            GivenIHaveAnDownstreamPathTemplate("api/product/products/{productId}");
            WhenIMatchThePaths();
            ThenTheResultIsTrue();
            ThenTheTemplatesDictionaryIs(expectedTemplates);
            ThenTheUrlPathTemplateIs("api/product/products/{productId}");

        }

        [Fact]
        public void can_match_down_stream_url_with_downstream_template_with_two_place_holders()
        {
            var expectedTemplates = new List<TemplateVariableNameAndValue> 
            {
                new TemplateVariableNameAndValue("{productid}", "1"),
                new TemplateVariableNameAndValue("{categoryid}", "2")
            };
            
            GivenIHaveADownstreamPath("api/product/products/1/2");
            GivenIHaveAnDownstreamPathTemplate("api/product/products/{productId}/{categoryId}");
            WhenIMatchThePaths();
            ThenTheResultIsTrue();
            ThenTheTemplatesDictionaryIs(expectedTemplates);
            ThenTheUrlPathTemplateIs("api/product/products/{productId}/{categoryId}");

        }

        [Fact]
        public void can_match_down_stream_url_with_downstream_template_with_two_place_holders_seperated_by_something()
        {
            var expectedTemplates = new List<TemplateVariableNameAndValue> 
            {
                new TemplateVariableNameAndValue("{productid}", "1"),
                new TemplateVariableNameAndValue("{categoryid}", "2")
            };
            
            GivenIHaveADownstreamPath("api/product/products/1/categories/2");
            GivenIHaveAnDownstreamPathTemplate("api/product/products/{productId}/categories/{categoryId}");
            WhenIMatchThePaths();
            ThenTheResultIsTrue();
            ThenTheTemplatesDictionaryIs(expectedTemplates);
            ThenTheUrlPathTemplateIs("api/product/products/{productId}/categories/{categoryId}");

        }

        [Fact]
        public void can_match_down_stream_url_with_downstream_template_with_three_place_holders_seperated_by_something()
        {
            var expectedTemplates = new List<TemplateVariableNameAndValue> 
            {
                new TemplateVariableNameAndValue("{productid}", "1"),
                new TemplateVariableNameAndValue("{categoryid}", "2"),
                new TemplateVariableNameAndValue("{variantid}", "123")
            };
            
            GivenIHaveADownstreamPath("api/product/products/1/categories/2/variant/123");
            GivenIHaveAnDownstreamPathTemplate("api/product/products/{productId}/categories/{categoryId}/variant/{variantId}");
            WhenIMatchThePaths();
            ThenTheResultIsTrue();
            ThenTheTemplatesDictionaryIs(expectedTemplates);
            ThenTheUrlPathTemplateIs("api/product/products/{productId}/categories/{categoryId}/variant/{variantId}");

        }

        [Fact]
        public void can_match_down_stream_url_with_downstream_template_with_three_place_holders()
        {
            var expectedTemplates = new List<TemplateVariableNameAndValue> 
            {
                new TemplateVariableNameAndValue("{productid}", "1"),
                new TemplateVariableNameAndValue("{categoryid}", "2")
            };
            
            GivenIHaveADownstreamPath("api/product/products/1/categories/2/variant/");
            GivenIHaveAnDownstreamPathTemplate("api/product/products/{productId}/categories/{categoryId}/variant/");
            WhenIMatchThePaths();
            ThenTheResultIsTrue();
            ThenTheTemplatesDictionaryIs(expectedTemplates);
            ThenTheUrlPathTemplateIs("api/product/products/{productId}/categories/{categoryId}/variant/");

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
            _result.UrlPathTemplate.ShouldBe(expectedUrlPathTemplate);
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