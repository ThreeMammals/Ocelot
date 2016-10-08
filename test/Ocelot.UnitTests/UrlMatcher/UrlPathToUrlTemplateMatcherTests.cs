using System.Collections.Generic;
using System.Linq;
using Ocelot.Library.Infrastructure.UrlMatcher;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.UrlMatcher
{
    public class UrlPathToUrlTemplateMatcherTests 
    {
        private readonly IUrlPathToUrlTemplateMatcher _urlMatcher;
        private string _downstreamUrlPath;
        private string _downstreamPathTemplate;
        private UrlMatch _result;
        public UrlPathToUrlTemplateMatcherTests()
        {
            _urlMatcher = new UrlPathToUrlTemplateMatcher();
        }

        [Fact]
        public void should_find_match_when_template_smaller_than_valid_path()
        {
            this.Given(x => x.GivenIHaveAUpstreamPath("/api/products/2354325435624623464235"))
                .And(x => x.GivenIHaveAnUpstreamUrlTemplate("/api/products/{productId}"))
                .When(x => x.WhenIMatchThePaths())
                .And(x => x.ThenTheResultIsTrue())
                .BDDfy();
        }

        [Fact]
        public void should_not_find_match()
        {
            this.Given(x => x.GivenIHaveAUpstreamPath("/api/values"))
                .And(x => x.GivenIHaveAnUpstreamUrlTemplate("/"))
                .When(x => x.WhenIMatchThePaths())
                .And(x => x.ThenTheResultIsFalse())
                .BDDfy();
        }

        [Fact]
        public void can_match_down_stream_url()
        {
            this.Given(x => x.GivenIHaveAUpstreamPath(""))
                .And(x => x.GivenIHaveAnUpstreamUrlTemplate(""))
                .When(x => x.WhenIMatchThePaths())
                .And(x => x.ThenTheResultIsTrue())
                .And(x => x.ThenTheTemplatesVariablesAre(new List<TemplateVariableNameAndValue>()))
                .And(x => x.ThenTheDownstreamUrlTemplateIs(""))
                .BDDfy();
        }

        [Fact]
        public void can_match_down_stream_url_with_no_slash()
        {
            this.Given(x => x.GivenIHaveAUpstreamPath("api"))
                 .Given(x => x.GivenIHaveAnUpstreamUrlTemplate("api"))
                 .When(x => x.WhenIMatchThePaths())
                 .Then(x => x.ThenTheResultIsTrue())
                 .And(x => x.ThenTheTemplatesVariablesAre(new List<TemplateVariableNameAndValue>()))
                 .And(x => x.ThenTheDownstreamUrlTemplateIs("api"))
                 .BDDfy();
        }

        [Fact]
        public void can_match_down_stream_url_with_one_slash()
        {
            this.Given(x => x.GivenIHaveAUpstreamPath("api/"))
                 .Given(x => x.GivenIHaveAnUpstreamUrlTemplate("api/"))
                 .When(x => x.WhenIMatchThePaths())
                 .Then(x => x.ThenTheResultIsTrue())
                 .And(x => x.ThenTheTemplatesVariablesAre(new List<TemplateVariableNameAndValue>()))
                 .And(x => x.ThenTheDownstreamUrlTemplateIs("api/"))
                 .BDDfy();
        }

        [Fact]
        public void can_match_down_stream_url_with_downstream_template()
        {
            this.Given(x => x.GivenIHaveAUpstreamPath("api/product/products/"))
              .Given(x => x.GivenIHaveAnUpstreamUrlTemplate("api/product/products/"))
              .When(x => x.WhenIMatchThePaths())
              .Then(x => x.ThenTheResultIsTrue())
              .And(x => x.ThenTheTemplatesVariablesAre(new List<TemplateVariableNameAndValue>()))
              .And(x => x.ThenTheDownstreamUrlTemplateIs("api/product/products/"))
              .BDDfy();
        }

        [Fact]
        public void can_match_down_stream_url_with_downstream_template_with_one_place_holder()
        {
            var expectedTemplates = new List<TemplateVariableNameAndValue> 
            {
                new TemplateVariableNameAndValue("{productId}", "1")
            };

            this.Given(x => x.GivenIHaveAUpstreamPath("api/product/products/1"))
               .Given(x => x.GivenIHaveAnUpstreamUrlTemplate("api/product/products/{productId}"))
               .When(x => x.WhenIMatchThePaths())
               .Then(x => x.ThenTheResultIsTrue())
               .And(x => x.ThenTheTemplatesVariablesAre(expectedTemplates))
               .And(x => x.ThenTheDownstreamUrlTemplateIs("api/product/products/{productId}"))
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

            this.Given(x => x.GivenIHaveAUpstreamPath("api/product/products/1/2"))
                 .Given(x => x.GivenIHaveAnUpstreamUrlTemplate("api/product/products/{productId}/{categoryId}"))
                 .When(x => x.WhenIMatchThePaths())
                 .Then(x => x.ThenTheResultIsTrue())
                 .And(x => x.ThenTheTemplatesVariablesAre(expectedTemplates))
                 .And(x => x.ThenTheDownstreamUrlTemplateIs("api/product/products/{productId}/{categoryId}"))
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

            this.Given(x => x.GivenIHaveAUpstreamPath("api/product/products/1/categories/2"))
                .And(x => x.GivenIHaveAnUpstreamUrlTemplate("api/product/products/{productId}/categories/{categoryId}"))
                .When(x => x.WhenIMatchThePaths())
                .Then(x => x.ThenTheResultIsTrue())
                .And(x => x.ThenTheTemplatesVariablesAre(expectedTemplates))
                .And(x => x.ThenTheDownstreamUrlTemplateIs("api/product/products/{productId}/categories/{categoryId}"))
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

            this.Given(x => x.GivenIHaveAUpstreamPath("api/product/products/1/categories/2/variant/123"))
                .And(x => x.GivenIHaveAnUpstreamUrlTemplate("api/product/products/{productId}/categories/{categoryId}/variant/{variantId}"))
                .When(x => x.WhenIMatchThePaths())
                .Then(x => x.ThenTheResultIsTrue())
                .And(x => x.ThenTheTemplatesVariablesAre(expectedTemplates))
                .And(x => x.ThenTheDownstreamUrlTemplateIs("api/product/products/{productId}/categories/{categoryId}/variant/{variantId}"))
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

            this.Given(x => x.GivenIHaveAUpstreamPath("api/product/products/1/categories/2/variant/"))
                 .And(x => x.GivenIHaveAnUpstreamUrlTemplate("api/product/products/{productId}/categories/{categoryId}/variant/"))
                 .When(x => x.WhenIMatchThePaths())
                 .Then(x => x.ThenTheResultIsTrue())
                 .And(x => x.ThenTheTemplatesVariablesAre(expectedTemplates))
                 .And(x => x.ThenTheDownstreamUrlTemplateIs("api/product/products/{productId}/categories/{categoryId}/variant/"))
                 .BDDfy();
        }

        private void ThenTheTemplatesVariablesAre(List<TemplateVariableNameAndValue> expectedResults)
        {
            foreach (var expectedResult in expectedResults)
            {
                var result = _result.TemplateVariableNameAndValues
                    .First(t => t.TemplateVariableName == expectedResult.TemplateVariableName);
                result.TemplateVariableValue.ShouldBe(expectedResult.TemplateVariableValue);
            }
        }

        private void ThenTheDownstreamUrlTemplateIs(string expectedDownstreamUrlTemplate)
        {
            _result.DownstreamUrlTemplate.ShouldBe(expectedDownstreamUrlTemplate);
        }
        private void GivenIHaveAUpstreamPath(string downstreamPath)
        {
            _downstreamUrlPath = downstreamPath;
        }

        private void GivenIHaveAnUpstreamUrlTemplate(string downstreamUrlTemplate)
        {
            _downstreamPathTemplate = downstreamUrlTemplate;
        }

        private void WhenIMatchThePaths()
        {
            _result = _urlMatcher.Match(_downstreamUrlPath, _downstreamPathTemplate);
        }

        private void ThenTheResultIsTrue()
        {
            _result.Match.ShouldBeTrue();
        }

        private void ThenTheResultIsFalse()
        {
            _result.Match.ShouldBeFalse();
        }
    }
} 