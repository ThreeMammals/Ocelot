using System;
using Ocelot.Library.Infrastructure.Responses;
using Ocelot.Library.Infrastructure.Router.UpstreamRouter;
using Ocelot.Library.Infrastructure.Router.UrlPathMatcher;
using Shouldly;
using Xunit;

namespace Ocelot.UnitTests
{
    public class UrlPathToUrlPathTemplateMatcherTests 
    {
        private IUrlPathToUrlPathTemplateMatcher _urlMapper;

        public UrlPathToUrlPathTemplateMatcherTests()
        {
            _urlMapper = new UrlPathToUrlPathTemplateMatcher();
        }

        [Fact]
        public void can_match_down_stream_url_with_downstream_template_with_one_query_string_parameter()
        {
            var downstreamUrl = "api/product/products/?soldout=false";
            var downstreamTemplate = "api/product/products/";
            var result = _urlMapper.Match(downstreamUrl, downstreamTemplate);
            result.ShouldBeTrue();
        }

          [Fact]
        public void can_match_down_stream_url_with_downstream_template_with_one_query_string_parameter_and_one_template()
        {
            var downstreamUrl = "api/product/products/1/variants/?soldout=false";
            var downstreamTemplate = "api/product/products/{productId}/variants/";
            var result = _urlMapper.Match(downstreamUrl, downstreamTemplate);
            result.ShouldBeTrue();
        }

        [Fact]
        public void can_match_down_stream_url_with_downstream_template_with_one_place_holder()
        {
            var downstreamUrl = "api/product/products/1";
            var downstreamTemplate = "api/product/products/{productId}";
            var result = _urlMapper.Match(downstreamUrl, downstreamTemplate);
            result.ShouldBeTrue();
        }

        [Fact]
        public void can_match_down_stream_url_with_downstream_template_with_two_place_holders()
        {
            var downstreamUrl = "api/product/products/1/2";
            var downstreamTemplate = "api/product/products/{productId}/{categoryId}";
            var result = _urlMapper.Match(downstreamUrl, downstreamTemplate);
            result.ShouldBeTrue();
        }

        [Fact]
        public void can_match_down_stream_url_with_downstream_template_with_two_place_holders_seperated_by_something()
        {
            var downstreamUrl = "api/product/products/1/categories/2";
            var downstreamTemplate = "api/product/products/{productId}/categories/{categoryId}";
            var result = _urlMapper.Match(downstreamUrl, downstreamTemplate);
            result.ShouldBeTrue();
        }

        [Fact]
        public void can_match_down_stream_url_with_downstream_template_with_three_place_holders_seperated_by_something()
        {
            var downstreamUrl = "api/product/products/1/categories/2/variant/123";
            var downstreamTemplate = "api/product/products/{productId}/categories/{categoryId}/variant/{variantId}";
            var result = _urlMapper.Match(downstreamUrl, downstreamTemplate);
            result.ShouldBeTrue();
        }

        [Fact]
        public void can_match_down_stream_url_with_downstream_template_with_three_place_holders()
        {
            var downstreamUrl = "api/product/products/1/categories/2/variant/";
            var downstreamTemplate = "api/product/products/{productId}/categories/{categoryId}/variant/";
            var result = _urlMapper.Match(downstreamUrl, downstreamTemplate);
            result.ShouldBeTrue();
        }
    }
} 