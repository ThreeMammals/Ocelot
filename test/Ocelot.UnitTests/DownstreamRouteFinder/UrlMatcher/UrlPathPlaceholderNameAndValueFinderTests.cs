using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Responses;

namespace Ocelot.UnitTests.DownstreamRouteFinder.UrlMatcher;

public class UrlPathPlaceholderNameAndValueFinderTests : UnitTest
{
    private readonly UrlPathPlaceholderNameAndValueFinder _finder;
    private Response<List<PlaceholderNameAndValue>> _result;
    private static readonly string Empty = string.Empty;

    public UrlPathPlaceholderNameAndValueFinderTests()
    {
        _finder = new UrlPathPlaceholderNameAndValueFinder();
    }

    [Fact]
    public void Can_match_down_stream_url()
    {
        // Arrange, Act
        _result = _finder.Find(Empty, Empty, Empty);

        // Assert
        ThenTheTemplatesVariablesAre();
    }

    [Fact]
    public void Can_match_down_stream_url_with_nothing_then_placeholder_no_value_is_blank()
    {
        // Arrange, Act
        _result = _finder.Find(Empty, Empty, "/{url}");

        // Assert
        ThenSinglePlaceholderIs("{url}", Empty);
    }

    [Fact]
    public void Can_match_down_stream_url_with_nothing_then_placeholder_value_is_test()
    {
        // Arrange, Act
        _result = _finder.Find("/test", Empty, "/{url}");

        // Assert
        ThenSinglePlaceholderIs("{url}", "test");
    }

    [Fact]
    public void Should_match_everything_in_path_with_query()
    {
        // Arrange, Act
        _result = _finder.Find("/test/toot", "?$filter=Name%20eq%20'Sam'", "/{everything}");

        // Assert
        ThenSinglePlaceholderIs("{everything}", "test/toot");
    }

    [Fact]
    public void Should_match_everything_in_path()
    {
        // Arrange, Act
        _result = _finder.Find("/test/toot", Empty, "/{everything}");

        // Assert
        ThenSinglePlaceholderIs("{everything}", "test/toot");
    }

    [Fact]
    public void Can_match_down_stream_url_with_forward_slash_then_placeholder_no_value_is_blank()
    {
        // Arrange, Act
        _result = _finder.Find("/", Empty, "/{url}");

        // Assert
        ThenSinglePlaceholderIs("{url}", Empty);
    }

    [Fact]
    public void Can_match_down_stream_url_with_forward_slash()
    {
        // Arrange, Act
        _result = _finder.Find("/", Empty, "/");

        // Assert
        ThenTheTemplatesVariablesAre();
    }

    [Fact]
    public void Can_match_down_stream_url_with_forward_slash_then_placeholder_then_another_value()
    {
        // Arrange, Act
        _result = _finder.Find("/1/products", Empty, "/{url}/products");

        // Assert
        ThenSinglePlaceholderIs("{url}", "1");
    }

    [Fact]
    public void Should_not_find_anything()
    {
        // Arrange, Act
        _result = _finder.Find("/products", Empty, "/products/");

        // Assert
        ThenTheTemplatesVariablesAre();
    }

    [Fact]
    public void Should_find_query_string()
    {
        // Arrange, Act
        _result = _finder.Find("/products", "?productId=1", "/products?productId={productId}");

        // Assert
        ThenSinglePlaceholderIs("{productId}", "1");
    }

    [Fact]
    public void Should_find_query_string_dont_include_hardcoded()
    {
        // Arrange, Act
        _result = _finder.Find("/products", "?productId=1&categoryId=2", "/products?productId={productId}");

        // Assert
        ThenSinglePlaceholderIs("{productId}", "1");
    }

    [Fact]
    public void Should_find_multiple_query_string()
    {
        // Arrange, Act
        _result = _finder.Find("/products", "?productId=1&categoryId=2", "/products?productId={productId}&categoryId={categoryId}");

        // Assert
        ThenTheTemplatesVariablesAre(
            new("{productId}", "1"),
            new("{categoryId}", "2"));
    }

    [Fact]
    public void Should_find_multiple_query_string_and_path()
    {
        // Arrange, Act
        _result = _finder.Find("/products/3", "?productId=1&categoryId=2", "/products/{account}?productId={productId}&categoryId={categoryId}");

        // Assert
        ThenTheTemplatesVariablesAre(
            new("{productId}", "1"),
            new("{categoryId}", "2"),
            new("{account}", "3"));
    }

    [Fact]
    public void Should_find_multiple_query_string_and_path_that_ends_with_slash()
    {
        // Arrange, Act
        _result = _finder.Find("/products/3/", "?productId=1&categoryId=2", "/products/{account}/?productId={productId}&categoryId={categoryId}");

        // Assert
        ThenTheTemplatesVariablesAre(
            new("{productId}", "1"),
            new("{categoryId}", "2"),
            new("{account}", "3"));
    }

    [Fact]
    public void Can_match_down_stream_url_with_no_slash()
    {
        // Arrange, Act
        _result = _finder.Find("api", Empty, "api");

        // Assert
        ThenTheTemplatesVariablesAre();
    }

    [Fact]
    public void Can_match_down_stream_url_with_one_slash()
    {
        // Arrange, Act
        _result = _finder.Find("api/", Empty, "api/");

        // Assert
        ThenTheTemplatesVariablesAre();
    }

    [Fact]
    public void Can_match_down_stream_url_with_downstream_template()
    {
        // Arrange, Act
        _result = _finder.Find("api/product/products/", Empty, "api/product/products/");

        // Assert
        ThenTheTemplatesVariablesAre();
    }

    [Fact]
    public void Can_match_down_stream_url_with_downstream_template_with_one_place_holder()
    {
        // Arrange, Act
        _result = _finder.Find("api/product/products/1", Empty, "api/product/products/{productId}");

        // Assert
        ThenSinglePlaceholderIs("{productId}", "1");
    }

    [Fact]
    public void Can_match_down_stream_url_with_downstream_template_with_two_place_holders()
    {
        // Arrange, Act
        _result = _finder.Find("api/product/products/1/2", Empty, "api/product/products/{productId}/{categoryId}");

        // Assert
        ThenTheTemplatesVariablesAre(
            new("{productId}", "1"),
            new("{categoryId}", "2"));
    }

    [Fact]
    public void Can_match_down_stream_url_with_downstream_template_with_two_place_holders_seperated_by_something()
    {
        // Arrange, Act
        _result = _finder.Find("api/product/products/1/categories/2", Empty, "api/product/products/{productId}/categories/{categoryId}");

        // Assert
        ThenTheTemplatesVariablesAre(
            new("{productId}", "1"),
            new("{categoryId}", "2"));
    }

    [Fact]
    public void Can_match_down_stream_url_with_downstream_template_with_three_place_holders_seperated_by_something()
    {
        // Arrange, Act
        _result = _finder.Find("api/product/products/1/categories/2/variant/123", Empty, "api/product/products/{productId}/categories/{categoryId}/variant/{variantId}");

        // Assert
        ThenTheTemplatesVariablesAre(
            new("{productId}", "1"),
            new("{categoryId}", "2"),
            new("{variantId}", "123"));
    }

    [Fact]
    public void Can_match_down_stream_url_with_downstream_template_with_three_place_holders()
    {
        // Arrange, Act
        _result = _finder.Find("api/product/products/1/categories/2/variant/", Empty, "api/product/products/{productId}/categories/{categoryId}/variant/");

        // Assert
        ThenTheTemplatesVariablesAre(
            new("{productId}", "1"),
            new("{categoryId}", "2"));
    }

    [Theory]
    [Trait("Feat", "89")]
    [InlineData("/api/{finalUrlPath}", "/api/product/products/categories/", "{finalUrlPath}", "product/products/categories/")]
    [InlineData("/myApp1Name/api/{urlPath}", "/myApp1Name/api/products/1", "{urlPath}", "products/1")]
    public void Can_match_down_stream_url_with_downstream_template_with_place_holder_to_final_url_path(string template, string path, string placeholderName, string placeholderValue)
    {
        // Arrange, Act
        _result = _finder.Find(path, Empty, template);

        // Assert
        ThenSinglePlaceholderIs(placeholderName, placeholderValue);
    }

    [Fact]
    [Trait("Bug", "748")]
    public void Check_for_placeholder_at_end_of_template() 
    {
        // Arrange, Act
        _result = _finder.Find("/upstream/test/", Empty, "/upstream/test/{testId}");

        // Assert
        ThenSinglePlaceholderIs("{testId}", Empty);
    }

    [Theory]
    [Trait("Bug", "748")]
    [InlineData("/api/invoices/{url}", "/api/invoices/123", "{url}", "123")]
    [InlineData("/api/invoices/{url}", "/api/invoices/", "{url}", "")]
    [InlineData("/api/invoices/{url}", "/api/invoices", "{url}", "")]
    [InlineData("/api/{version}/invoices/", "/api/v1/invoices/", "{version}", "v1")]
    public void Should_fix_issue_748(string template, string path, string placeholderName, string placeholderValue)
    {
        // Arrange, Act
        _result = _finder.Find(path, Empty, template);

        // Assert
        ThenSinglePlaceholderIs(placeholderName, placeholderValue);
    }

    [Theory]
    [Trait("Bug", "748")]
    [InlineData("/api/{version}/invoices/{url}", "/api/v1/invoices/123", "{version}", "v1", "{url}", "123")]
    [InlineData("/api/{version}/invoices/{url}", "/api/v1/invoices/", "{version}", "v1", "{url}", "")]
    [InlineData("/api/invoices/{url}?{query}", "/api/invoices/test?query=1", "{url}", "test", "{query}", "query=1")]
    [InlineData("/api/invoices/{url}?{query}", "/api/invoices/?query=1", "{url}", "", "{query}", "query=1")]
    public void Should_resolve_catchall_at_end_with_middle_placeholder(string template, string path, string placeholderName, string placeholderValue, string catchallName, string catchallValue)
    {
        // Arrange, Act
        _result = _finder.Find(path, Empty, template);

        // Assert
        ThenTheTemplatesVariablesAre(
            new(placeholderName, placeholderValue),
            new(catchallName, catchallValue));
    }

    [Theory]
    [Trait("Bug", "2199")]
    [InlineData("/api/invoices/{url}-abcd", "/api/invoices/123-abcd", "{url}", "123")]
    [InlineData("/api/invoices/{url1}-{url2}_abcd", "/api/invoices/123-456_abcd", "{url1}", "123")]
    public void Can_match_between_slashes(string template, string path, string placeholderName, string placeholderValue)
    {
        // Arrange, Act
        _result = _finder.Find(path, Empty, template);

        // Assert
        ThenSinglePlaceholderIs(placeholderName, placeholderValue);
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
    public void Can_match_all_placeholders_between_slashes(string template, string path,
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

        // Act
        _result = _finder.Find(path, Empty, template);

        // Assert
        ThenTheTemplatesVariablesAre(expectedTemplates.ToArray());
    }
    
    [Theory]
    [Trait("Bug", "2209")]
    [InlineData(
        "/entities/{id}/events/recordsdata/{subCategoryId}_abcd/{itemId}?sort={sortBy}",
        "/Entities/43/Events/RecordsData/2_abcd/5?sort=desc",
        "{id}", "43", "{subCategoryId}", "2", "{itemId}", "5", "{sortBy}", "desc")]
    [InlineData(
        "/api/PRODUCTS/{productId}/{category}_{itemId}_DeTails/{status}",
        "/API/Products/789/electronics_123_details/available",
        "{productId}", "789", "{category}", "electronics", "{itemId}", "123", "{status}", "available")]
    public void Find_CaseInsensitive_MatchedAllPlaceholdersBetweenSlashes(string template, string path,
        string placeholderName1, string placeholderValue1, string placeholderName2, string placeholderValue2,
        string placeholderName3, string placeholderValue3, string placeholderName4, string placeholderValue4)
    {
        // Arrange
        var expectedTemplates = new List<PlaceholderNameAndValue>
        {
            new(placeholderName1, placeholderValue1),
            new(placeholderName2, placeholderValue2),
            new(placeholderName3, placeholderValue3),
            new(placeholderName4, placeholderValue4),
        };

        // Act
        _result = _finder.Find(path, Empty, template);

        // Assert
        ThenTheTemplatesVariablesAre(expectedTemplates.ToArray());
    }

    [Theory]
    [Trait("Bug", "2209")]
    [InlineData(
        "/entities/{Id}/events/recordsdata/{subCategoryId}_abcd/{itemId}?sort={sortBy}",
        "/Entities/43/Events/RecordsData/2_abcd/5?sort=desc",
        "{id}", "43", "{subcategoryid}", "2", "{itemid}", "5", "{sortby}", "desc")]
    [InlineData(
        "/api/PRODUCTS/{productid}/{category}_{itemid}_DeTails/{status}",
        "/API/Products/789/electronics_123_details/available",
        "{productId}", "789", "{Category}", "electronics", "{itemId}", "123", "{Status}", "available")]
    public void Find_CaseInsensitive_CannotMatchPlaceholders(string template, string path,
        string placeholderName1, string placeholderValue1, string placeholderName2, string placeholderValue2,
        string placeholderName3, string placeholderValue3, string placeholderName4, string placeholderValue4)
    {
        var expectedTemplates = new List<PlaceholderNameAndValue>
        {
            new(placeholderName1, placeholderValue1),
            new(placeholderName2, placeholderValue2),
            new(placeholderName3, placeholderValue3),
            new(placeholderName4, placeholderValue4),
        };

        // Act
        _result = _finder.Find(path, Empty, template);

        // Assert;
        ThenTheExpectedVariablesCantBeFound(expectedTemplates.ToArray());
    }
    
    [Theory]
    [Trait("Bug", "2212")]
    [InlineData("/dati-registri/{version}/{everything}", "/dati-registri/v1.0/operatore/R80QQ5J9600/valida", "{version}", "v1.0", "{everything}", "operatore/R80QQ5J9600/valida")]
    [InlineData("/api/invoices/{invoiceId}/{url}", "/api/invoices/1", "{invoiceId}", "1", "{url}", "")]
    [InlineData("/api/{version}/{type}/{everything}", "/api/v1.0/items/details/12345", "{version}", "v1.0", "{type}", "items", "{everything}", "details/12345")]
    [InlineData("/resources/{area}/{id}/{details}", "/resources/europe/56789/info/about", "{area}", "europe", "{id}", "56789", "{details}", "info/about")]
    [InlineData("/data/{version}/{category}/{subcategory}/{rest}", "/data/2.1/sales/reports/weekly/summary", "{version}", "2.1", "{category}", "sales", "{subcategory}", "reports", "{rest}", "weekly/summary")]
    [InlineData("/users/{region}/{team}/{userId}/{details}", "/users/north/eu/12345/activities/list", "{region}", "north", "{team}", "eu", "{userId}", "12345", "{details}", "activities/list")]
    public void Find_HasCatchAll_OnlyTheLastPlaceholderCanContainSlashes(string template, string path,
        string placeholderName1, string placeholderValue1, string placeholderName2, string placeholderValue2,
        string placeholderName3 = null, string placeholderValue3 = null, string placeholderName4 = null, string placeholderValue4 = null)
    {
        var expectedTemplates = new List<PlaceholderNameAndValue>
        {
            new(placeholderName1, placeholderValue1),
            new(placeholderName2, placeholderValue2),
        };
        
        if (!string.IsNullOrEmpty(placeholderName3))
        {
            expectedTemplates.Add(new(placeholderName3, placeholderValue3));
        }
        
        if (!string.IsNullOrEmpty(placeholderName4))
        {
            expectedTemplates.Add(new(placeholderName4, placeholderValue4));
        }

        // Act
        _result = _finder.Find(path, Empty, template);

        // Assert
        ThenTheTemplatesVariablesAre(expectedTemplates.ToArray());
    }

    private void ThenSinglePlaceholderIs(string expectedName, string expectedValue)
    {
        var item = _result.Data.Single(t => t.Name == expectedName);
        item.Value.ShouldBe(expectedValue);
    }

    private void ThenTheTemplatesVariablesAre(params PlaceholderNameAndValue[] collection)
    {
        foreach (var expected in collection)
        {
            ThenSinglePlaceholderIs(expected.Name, expected.Value);
        }
    }

    private void ThenTheExpectedVariablesCantBeFound(params PlaceholderNameAndValue[] collection)
    {
        foreach (var expected in collection)
        {
            _result.Data.FirstOrDefault(t => t.Name == expected.Name).ShouldBeNull();
        }
    }
}
