using Ocelot.Library.Infrastructure.HostUrlRepository;
using Ocelot.Library.Infrastructure.UrlFinder;
using Ocelot.Library.Infrastructure.Responses;
using Xunit;
using Shouldly;
using System;

namespace Ocelot.UnitTests
{
    public class UpstreamBaseUrlFinderTests
    {
        private IUpstreamHostUrlFinder _upstreamBaseUrlFinder;
        private IHostUrlMapRepository _hostUrlMapRepository;
        private string _downstreamBaseUrl;
        private Response<string> _result;
        public UpstreamBaseUrlFinderTests()
        {            
            _hostUrlMapRepository = new InMemoryHostUrlMapRepository();
            _upstreamBaseUrlFinder = new UpstreamHostUrlFinder(_hostUrlMapRepository);
        }

        [Fact]
        public void can_find_base_url()
        {
            GivenTheBaseUrlMapExists(new HostUrlMap("api.tom.com", "api.laura.com"));
            GivenTheDownstreamBaseUrlIs("api.tom.com");
            WhenIFindTheMatchingUpstreamBaseUrl();
            ThenTheFollowingIsReturned("api.laura.com");
        }

        [Fact]
        public void cant_find_base_url()
        {
            GivenTheDownstreamBaseUrlIs("api.tom.com");
            WhenIFindTheMatchingUpstreamBaseUrl();
            ThenAnErrorIsReturned();
        }

        private void ThenAnErrorIsReturned()
        {
            _result.Errors.Count.ShouldBe(1);
        }

        private void GivenTheBaseUrlMapExists(HostUrlMap baseUrlMap)
        {
            _hostUrlMapRepository.AddBaseUrlMap(baseUrlMap);

        }

        private void GivenTheDownstreamBaseUrlIs(string downstreamBaseUrl)
        {
            _downstreamBaseUrl = downstreamBaseUrl;
        }

        private void WhenIFindTheMatchingUpstreamBaseUrl()
        {
            _result = _upstreamBaseUrlFinder.FindUpstreamHostUrl(_downstreamBaseUrl);

        }

        private void ThenTheFollowingIsReturned(string expectedBaseUrl)
        {
            _result.Data.ShouldBe(expectedBaseUrl);
        }
    }
}