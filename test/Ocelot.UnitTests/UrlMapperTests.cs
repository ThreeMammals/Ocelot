using System;
using Ocelot.Library.Infrastructure.Responses;
using Ocelot.Library.Infrastructure.Router.UpstreamRouter;
using Shouldly;
using Xunit;

namespace Ocelot.UnitTests
{
    public class UrlMapperTests
    {
        private UrlToUrlTemplateMatcher _urlMapper;

        public UrlMapperTests()
        {
            _urlMapper = new UrlToUrlTemplateMatcher();
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

    public class UrlToUrlTemplateMatcher
    {
        public bool Match(string url, string urlTemplate)
        {
            url = url.ToLower();

            urlTemplate = urlTemplate.ToLower();

            int counterForUrl = 0;

            for (int counterForTemplate = 0; counterForTemplate < urlTemplate.Length; counterForTemplate++)
            {
                if (CharactersDontMatch(urlTemplate[counterForTemplate], url[counterForUrl]) && ContinueScanningUrl(counterForUrl,url.Length))
                {
                    if (IsPlaceholder(urlTemplate[counterForTemplate]))
                    {
                        counterForTemplate = GetNextCounterPosition(urlTemplate, counterForTemplate, '}');
                        counterForUrl = GetNextCounterPosition(url, counterForUrl, '/');
                        continue;
                    }
                    else
                    {
                        return false;
                    } 
                }
                counterForUrl++;
            }
            return true;
        }

        private int GetNextCounterPosition(string urlTemplate, int counterForTemplate, char delimiter)
        {                        
            var closingPlaceHolderPositionOnTemplate = urlTemplate.IndexOf(delimiter, counterForTemplate);
            return closingPlaceHolderPositionOnTemplate + 1; 
        }

        private bool CharactersDontMatch(char characterOne, char characterTwo)
        {
            return characterOne != characterTwo;
        }

        private bool ContinueScanningUrl(int counterForUrl, int urlLength)
        {
            return counterForUrl < urlLength;
        }

        private bool IsPlaceholder(char character)
        {
            return character == '{';
        }
    }
} 