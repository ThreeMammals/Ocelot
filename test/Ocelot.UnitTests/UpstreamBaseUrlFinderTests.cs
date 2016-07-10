using Ocelot.Library.Infrastructure.BaseUrlRepository;
using Ocelot.Library.Infrastructure.UrlFinder;
using Ocelot.Library.Infrastructure.Responses;
using Xunit;
using Shouldly;
using System;

namespace Ocelot.UnitTests
{
    public class UpstreamBaseUrlFinderTests
    {
        private IUpstreamBaseUrlFinder _upstreamBaseUrlFinder;
        private IBaseUrlMapRepository _baseUrlMapRepository;
        private string _downstreamBaseUrl;
        private Response<string> _result;
        public UpstreamBaseUrlFinderTests()
        {            
            _baseUrlMapRepository = new InMemoryBaseUrlMapRepository();
            _upstreamBaseUrlFinder = new UpstreamBaseUrlFinder(_baseUrlMapRepository);
        }

        [Fact]
        public void can_find_base_url()
        {
            GivenTheBaseUrlMapExists(new BaseUrlMap("api.tom.com", "api.laura.com"));
            GivenTheDownstreamBaseUrlIs("api.tom.com");
            WhenIFindTheMatchingUpstreamBaseUrl();
            ThenTheFollowingIsReturned("api.laura.com");
        }

        private void GivenTheBaseUrlMapExists(BaseUrlMap baseUrlMap)
        {
            _baseUrlMapRepository.AddBaseUrlMap(baseUrlMap);

        }

        private void GivenTheDownstreamBaseUrlIs(string downstreamBaseUrl)
        {
            _downstreamBaseUrl = downstreamBaseUrl;
        }

        private void WhenIFindTheMatchingUpstreamBaseUrl()
        {
            _result = _upstreamBaseUrlFinder.FindUpstreamBaseUrl(_downstreamBaseUrl);

        }

        private void ThenTheFollowingIsReturned(string expectedBaseUrl)
        {
            _result.Data.ShouldBe(expectedBaseUrl);
        }
    }
}