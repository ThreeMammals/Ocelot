using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Responses;

namespace Ocelot.UnitTests.DownstreamRouteFinder.UrlMatcher
{
    public class UrlPathPlaceholderNameAndValueFinderTests : UnitTest
    {
        private readonly IPlaceholderNameAndValueFinder _finder;
        private string _downstreamUrlPath;
        private string _downstreamPathTemplate;
        private Response<List<PlaceholderNameAndValue>> _result;
        private string _query;

        public UrlPathPlaceholderNameAndValueFinderTests()
        {
            _finder = new UrlPathPlaceholderNameAndValueFinder();
        }

        [Fact]
        public void can_match_down_stream_url()
        {
            this.Given(x => x.GivenIHaveAUpstreamPath(string.Empty))
                .And(x => x.GivenIHaveAnUpstreamUrlTemplate(string.Empty))
                .When(x => x.WhenIFindTheUrlVariableNamesAndValues())
                .And(x => x.ThenTheTemplatesVariablesAre(new List<PlaceholderNameAndValue>()))
                .BDDfy();
        }

        [Fact]
        public void can_match_down_stream_url_with_nothing_then_placeholder_no_value_is_blank()
        {
            var expectedTemplates = new List<PlaceholderNameAndValue>
            {
                new("{url}", string.Empty),
            };

            this.Given(x => x.GivenIHaveAUpstreamPath(string.Empty))
                .And(x => x.GivenIHaveAnUpstreamUrlTemplate("/{url}"))
                .When(x => x.WhenIFindTheUrlVariableNamesAndValues())
                .And(x => x.ThenTheTemplatesVariablesAre(expectedTemplates))
                .BDDfy();
        }

        [Fact]
        public void can_match_down_stream_url_with_nothing_then_placeholder_value_is_test()
        {
            var expectedTemplates = new List<PlaceholderNameAndValue>
            {
                new("{url}", "test"),
            };

            this.Given(x => x.GivenIHaveAUpstreamPath("/test"))
                .And(x => x.GivenIHaveAnUpstreamUrlTemplate("/{url}"))
                .When(x => x.WhenIFindTheUrlVariableNamesAndValues())
                .And(x => x.ThenTheTemplatesVariablesAre(expectedTemplates))
                .BDDfy();
        }

        [Fact]
        public void should_match_everything_in_path_with_query()
        {
            var expectedTemplates = new List<PlaceholderNameAndValue>
            {
                new("{everything}", "test/toot"),
            };

            this.Given(x => x.GivenIHaveAUpstreamPath("/test/toot"))
                .And(x => GivenIHaveAQuery("?$filter=Name%20eq%20'Sam'"))
                .And(x => x.GivenIHaveAnUpstreamUrlTemplate("/{everything}"))
                .When(x => x.WhenIFindTheUrlVariableNamesAndValues())
                .And(x => x.ThenTheTemplatesVariablesAre(expectedTemplates))
                .BDDfy();
        }

        [Fact]
        public void should_match_everything_in_path()
        {
            var expectedTemplates = new List<PlaceholderNameAndValue>
            {
                new("{everything}", "test/toot"),
            };

            this.Given(x => x.GivenIHaveAUpstreamPath("/test/toot"))
                .And(x => x.GivenIHaveAnUpstreamUrlTemplate("/{everything}"))
                .When(x => x.WhenIFindTheUrlVariableNamesAndValues())
                .And(x => x.ThenTheTemplatesVariablesAre(expectedTemplates))
                .BDDfy();
        }

        [Fact]
        public void can_match_down_stream_url_with_forward_slash_then_placeholder_no_value_is_blank()
        {
            var expectedTemplates = new List<PlaceholderNameAndValue>
            {
                new("{url}", string.Empty),
            };

            this.Given(x => x.GivenIHaveAUpstreamPath("/"))
                .And(x => x.GivenIHaveAnUpstreamUrlTemplate("/{url}"))
                .When(x => x.WhenIFindTheUrlVariableNamesAndValues())
                .And(x => x.ThenTheTemplatesVariablesAre(expectedTemplates))
                .BDDfy();
        }

        [Fact]
        public void can_match_down_stream_url_with_forward_slash()
        {
            var expectedTemplates = new List<PlaceholderNameAndValue>();

            this.Given(x => x.GivenIHaveAUpstreamPath("/"))
                .And(x => x.GivenIHaveAnUpstreamUrlTemplate("/"))
                .When(x => x.WhenIFindTheUrlVariableNamesAndValues())
                .And(x => x.ThenTheTemplatesVariablesAre(expectedTemplates))
                .BDDfy();
        }

        [Fact]
        public void can_match_down_stream_url_with_forward_slash_then_placeholder_then_another_value()
        {
            var expectedTemplates = new List<PlaceholderNameAndValue>
            {
                new("{url}", "1"),
            };

            this.Given(x => x.GivenIHaveAUpstreamPath("/1/products"))
                .And(x => x.GivenIHaveAnUpstreamUrlTemplate("/{url}/products"))
                .When(x => x.WhenIFindTheUrlVariableNamesAndValues())
                .And(x => x.ThenTheTemplatesVariablesAre(expectedTemplates))
                .BDDfy();
        }

        [Fact]
        public void should_not_find_anything()
        {
            this.Given(x => x.GivenIHaveAUpstreamPath("/products"))
                .And(x => x.GivenIHaveAnUpstreamUrlTemplate("/products/"))
                .When(x => x.WhenIFindTheUrlVariableNamesAndValues())
                .And(x => x.ThenTheTemplatesVariablesAre(new List<PlaceholderNameAndValue>()))
                .BDDfy();
        }

        [Fact]
        public void should_find_query_string()
        {
            var expectedTemplates = new List<PlaceholderNameAndValue>
            {
                new("{productId}", "1"),
            };

            this.Given(x => x.GivenIHaveAUpstreamPath("/products"))
                .And(x => x.GivenIHaveAQuery("?productId=1"))
                .And(x => x.GivenIHaveAnUpstreamUrlTemplate("/products?productId={productId}"))
                .When(x => x.WhenIFindTheUrlVariableNamesAndValues())
                .And(x => x.ThenTheTemplatesVariablesAre(expectedTemplates))
                .BDDfy();
        }

        [Fact]
        public void should_find_query_string_dont_include_hardcoded()
        {
            var expectedTemplates = new List<PlaceholderNameAndValue>
            {
                new("{productId}", "1"),
            };

            this.Given(x => x.GivenIHaveAUpstreamPath("/products"))
                .And(x => x.GivenIHaveAQuery("?productId=1&categoryId=2"))
                .And(x => x.GivenIHaveAnUpstreamUrlTemplate("/products?productId={productId}"))
                .When(x => x.WhenIFindTheUrlVariableNamesAndValues())
                .And(x => x.ThenTheTemplatesVariablesAre(expectedTemplates))
                .BDDfy();
        }

        [Fact]
        public void should_find_multiple_query_string()
        {
            var expectedTemplates = new List<PlaceholderNameAndValue>
            {
                new("{productId}", "1"),
                new("{categoryId}", "2"),
            };

            this.Given(x => x.GivenIHaveAUpstreamPath("/products"))
                .And(x => x.GivenIHaveAQuery("?productId=1&categoryId=2"))
                .And(x => x.GivenIHaveAnUpstreamUrlTemplate("/products?productId={productId}&categoryId={categoryId}"))
                .When(x => x.WhenIFindTheUrlVariableNamesAndValues())
                .And(x => x.ThenTheTemplatesVariablesAre(expectedTemplates))
                .BDDfy();
        }

        [Fact]
        public void should_find_multiple_query_string_and_path()
        {
            var expectedTemplates = new List<PlaceholderNameAndValue>
            {
                new("{productId}", "1"),
                new("{categoryId}", "2"),
                new("{account}", "3"),
            };

            this.Given(x => x.GivenIHaveAUpstreamPath("/products/3"))
                .And(x => x.GivenIHaveAQuery("?productId=1&categoryId=2"))
                .And(x => x.GivenIHaveAnUpstreamUrlTemplate("/products/{account}?productId={productId}&categoryId={categoryId}"))
                .When(x => x.WhenIFindTheUrlVariableNamesAndValues())
                .And(x => x.ThenTheTemplatesVariablesAre(expectedTemplates))
                .BDDfy();
        }

        [Fact]
        public void should_find_multiple_query_string_and_path_that_ends_with_slash()
        {
            var expectedTemplates = new List<PlaceholderNameAndValue>
            {
                new("{productId}", "1"),
                new("{categoryId}", "2"),
                new("{account}", "3"),
            };

            this.Given(x => x.GivenIHaveAUpstreamPath("/products/3/"))
                .And(x => x.GivenIHaveAQuery("?productId=1&categoryId=2"))
                .And(x => x.GivenIHaveAnUpstreamUrlTemplate("/products/{account}/?productId={productId}&categoryId={categoryId}"))
                .When(x => x.WhenIFindTheUrlVariableNamesAndValues())
                .And(x => x.ThenTheTemplatesVariablesAre(expectedTemplates))
                .BDDfy();
        }

        [Fact]
        public void can_match_down_stream_url_with_no_slash()
        {
            this.Given(x => x.GivenIHaveAUpstreamPath("api"))
                 .Given(x => x.GivenIHaveAnUpstreamUrlTemplate("api"))
                 .When(x => x.WhenIFindTheUrlVariableNamesAndValues())
                 .And(x => x.ThenTheTemplatesVariablesAre(new List<PlaceholderNameAndValue>()))
                 .BDDfy();
        }

        [Fact]
        public void can_match_down_stream_url_with_one_slash()
        {
            this.Given(x => x.GivenIHaveAUpstreamPath("api/"))
                 .Given(x => x.GivenIHaveAnUpstreamUrlTemplate("api/"))
                 .When(x => x.WhenIFindTheUrlVariableNamesAndValues())
                 .And(x => x.ThenTheTemplatesVariablesAre(new List<PlaceholderNameAndValue>()))
                 .BDDfy();
        }

        [Fact]
        public void can_match_down_stream_url_with_downstream_template()
        {
            this.Given(x => x.GivenIHaveAUpstreamPath("api/product/products/"))
              .Given(x => x.GivenIHaveAnUpstreamUrlTemplate("api/product/products/"))
              .When(x => x.WhenIFindTheUrlVariableNamesAndValues())
              .And(x => x.ThenTheTemplatesVariablesAre(new List<PlaceholderNameAndValue>()))
              .BDDfy();
        }

        [Fact]
        public void can_match_down_stream_url_with_downstream_template_with_one_place_holder()
        {
            var expectedTemplates = new List<PlaceholderNameAndValue>
            {
                new("{productId}", "1"),
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
            var expectedTemplates = new List<PlaceholderNameAndValue>
            {
                new("{productId}", "1"),
                new("{categoryId}", "2"),
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
            var expectedTemplates = new List<PlaceholderNameAndValue>
            {
                new("{productId}", "1"),
                new("{categoryId}", "2"),
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
            var expectedTemplates = new List<PlaceholderNameAndValue>
            {
                new("{productId}", "1"),
                new("{categoryId}", "2"),
                new("{variantId}", "123"),
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
            var expectedTemplates = new List<PlaceholderNameAndValue>
            {
                new("{productId}", "1"),
                new("{categoryId}", "2"),
            };

            this.Given(x => x.GivenIHaveAUpstreamPath("api/product/products/1/categories/2/variant/"))
                 .And(x => x.GivenIHaveAnUpstreamUrlTemplate("api/product/products/{productId}/categories/{categoryId}/variant/"))
                 .When(x => x.WhenIFindTheUrlVariableNamesAndValues())
                 .And(x => x.ThenTheTemplatesVariablesAre(expectedTemplates))
                 .BDDfy();
        }

        [Theory]
        [Trait("Feat", "89")]
        [InlineData("/api/{finalUrlPath}", "/api/product/products/categories/", "{finalUrlPath}", "product/products/categories/")]
        [InlineData("/myApp1Name/api/{urlPath}", "/myApp1Name/api/products/1", "{urlPath}", "products/1")]
        public void Can_match_down_stream_url_with_downstream_template_with_place_holder_to_final_url_path(string template, string path, string placeholderName, string placeholderValue)
        {
            // Arrange
            var expectedTemplates = new List<PlaceholderNameAndValue>
            {
                new(placeholderName, placeholderValue),
            };
            GivenIHaveAUpstreamPath(path);
            GivenIHaveAnUpstreamUrlTemplate(template);

            // Act
            WhenIFindTheUrlVariableNamesAndValues();

            // Assert
            ThenTheTemplatesVariablesAre(expectedTemplates);
        }

        [Fact]
        [Trait("Bug", "748")]
        public void check_for_placeholder_at_end_of_template() 
        {
            var expectedTemplates = new List<PlaceholderNameAndValue>
            {
                new("{testId}", string.Empty),
            };
            this.Given(x => x.GivenIHaveAUpstreamPath("/upstream/test/"))
                .And(x => x.GivenIHaveAnUpstreamUrlTemplate("/upstream/test/{testId}"))
                .When(x => x.WhenIFindTheUrlVariableNamesAndValues())
                .And(x => x.ThenTheTemplatesVariablesAre(expectedTemplates))
                .BDDfy();
        }

        [Theory]
        [Trait("Bug", "748")]
        [InlineData("/api/invoices/{url}", "/api/invoices/123", "{url}", "123")]
        [InlineData("/api/invoices/{url}", "/api/invoices/", "{url}", "")]
        [InlineData("/api/invoices/{url}", "/api/invoices", "{url}", "")]
        [InlineData("/api/{version}/invoices/", "/api/v1/invoices/", "{version}", "v1")]
        public void should_fix_issue_748(string upstreamTemplate, string requestURL, string placeholderName, string placeholderValue)
        {
            var expectedTemplates = new List<PlaceholderNameAndValue>
            {
                new(placeholderName, placeholderValue),
            };
            this.Given(x => x.GivenIHaveAUpstreamPath(requestURL))
                .And(x => x.GivenIHaveAnUpstreamUrlTemplate(upstreamTemplate))
                .When(x => x.WhenIFindTheUrlVariableNamesAndValues())
                .And(x => x.ThenTheTemplatesVariablesAre(expectedTemplates))
                .BDDfy();
        }

        [Theory]
        [Trait("Bug", "748")]
        [InlineData("/api/{version}/invoices/{url}", "/api/v1/invoices/123", "{version}", "v1", "{url}", "123")]
        [InlineData("/api/{version}/invoices/{url}", "/api/v1/invoices/", "{version}", "v1", "{url}", "")]
        [InlineData("/api/invoices/{url}?{query}", "/api/invoices/test?query=1", "{url}", "test", "{query}", "query=1")]
        [InlineData("/api/invoices/{url}?{query}", "/api/invoices/?query=1", "{url}", "", "{query}", "query=1")]
        public void should_resolve_catchall_at_end_with_middle_placeholder(string upstreamTemplate, string requestURL, string placeholderName, string placeholderValue, string catchallName, string catchallValue)
        {
            var expectedTemplates = new List<PlaceholderNameAndValue>
            {
                new(placeholderName, placeholderValue),
                new(catchallName, catchallValue),
            };
            this.Given(x => x.GivenIHaveAUpstreamPath(requestURL))
                .And(x => x.GivenIHaveAnUpstreamUrlTemplate(upstreamTemplate))
                .When(x => x.WhenIFindTheUrlVariableNamesAndValues())
                .And(x => x.ThenTheTemplatesVariablesAre(expectedTemplates))
                .BDDfy();
        }

        [Theory]
        [Trait("Bug", "2199")]
        [InlineData("/api/invoices/{url}-abcd", "/api/invoices/123-abcd", "{url}", "123")]
        [InlineData("/api/invoices/{url1}-{url2}_abcd", "/api/invoices/123-456_abcd", "{url1}", "123")]
        public void Can_match_between_slashes(string upstreamTemplate, string requestURL, string placeholderName,
            string placeholderValue)
        {
            // Arrange
            var expectedTemplates = new List<PlaceholderNameAndValue> { new(placeholderName, placeholderValue), };
            GivenIHaveAUpstreamPath(requestURL);
            GivenIHaveAnUpstreamUrlTemplate(upstreamTemplate);

            // Act
            WhenIFindTheUrlVariableNamesAndValues();

            // Assert
            ThenTheTemplatesVariablesAre(expectedTemplates);
        }

        [Theory]
        [Trait("Bug", "2199")]
        [Trait("Feat", "2200")]
        [InlineData("/api/invoices_{url0}/{url1}-{url2}_abcd/{url3}?urlId={url4}", "/api/invoices_super/123-456_abcd/789?urlId=987",
            "{url0}", "super", "{url1}", "123", "{url2}", "456", "{url3}", "789", "{url4}", "987")]
        [InlineData("/api/users/{userId}/posts/{postId}_abcd/{timestamp}?filter={filter}", "/api/users/101/posts/2022_abcd/2024?filter=active",
            "{userId}", "101", "{postId}", "2022", "{timestamp}", "2024", "{filter}", "active")]
        [InlineData("/api/categories/{categoryId}/{subCategoryId}_abcd/{itemId}?sort={sortBy}", "/api/categories/1/2_abcd/5?sort=desc",
            "{categoryId}", "1", "{subCategoryId}", "2", "{itemId}", "5", "{sortBy}", "desc")]
        [InlineData("/api/products/{productId}/{category}_{itemId}_details/{status}", "/api/products/789/electronics_123_details/available",
            "{productId}", "789", "{category}", "electronics", "{itemId}", "123", "{status}", "available")]
        public void Can_match_all_placeholders_between_slashes(string upstreamTemplate, string requestURL,
            string placeholderName1, string placeholderValue1, string placeholderName2, string placeholderValue2,
            string placeholderName3, string placeholderValue3, string placeholderName4, string placeholderValue4,
            string placeholderName5 = null, string placeholderValue5 = null)
        {
            // Arrange
            var expectedTemplates = new List<PlaceholderNameAndValue>
            {
                new(placeholderName1, placeholderValue1),
                new(placeholderName2, placeholderValue2),
                new(placeholderName3, placeholderValue3),
                new(placeholderName4, placeholderValue4),
            };

            // Add optional placeholders if they exist
            if (!string.IsNullOrEmpty(placeholderName5))
            {
                expectedTemplates.Add(new(placeholderName5, placeholderValue5));
            }

            GivenIHaveAUpstreamPath(requestURL);
            GivenIHaveAnUpstreamUrlTemplate(upstreamTemplate);

            // Act
            WhenIFindTheUrlVariableNamesAndValues();

            // Assert
            ThenTheTemplatesVariablesAre(expectedTemplates);
        }

        private void ThenTheTemplatesVariablesAre(List<PlaceholderNameAndValue> expectedResults)
        {
            foreach (var expectedResult in expectedResults)
            {
                var result = _result.Data.First(t => t.Name == expectedResult.Name);
                result.Value.ShouldBe(expectedResult.Value);
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
            _result = _finder.Find(_downstreamUrlPath, _query, _downstreamPathTemplate);
        }

        private void GivenIHaveAQuery(string query)
        {
            _query = query;
        }
    }
}
