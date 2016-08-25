using Ocelot.Library.Infrastructure.HostUrlRepository;
using Ocelot.Library.Infrastructure.UrlFinder;
using Ocelot.Library.Infrastructure.Responses;
using Xunit;
using Shouldly;
using TestStack.BDDfy;

namespace Ocelot.UnitTests
{
    public class UpstreamBaseUrlFinderTests
    {
        private readonly IUpstreamHostUrlFinder _upstreamBaseUrlFinder;
        private readonly IHostUrlMapRepository _hostUrlMapRepository;
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
            this.Given(x => x.GivenTheBaseUrlMapExists(new HostUrlMap("api.tom.com", "api.laura.com")))
                .And(x => x.GivenTheDownstreamBaseUrlIs("api.tom.com"))
                .When(x => x.WhenIFindTheMatchingUpstreamBaseUrl())
                .Then(x => x.ThenTheFollowingIsReturned("api.laura.com"))
                .BDDfy();
        }

        [Fact]
        public void cant_find_base_url()
        {
            this.Given(x => x.GivenTheDownstreamBaseUrlIs("api.tom.com"))
                .When(x => x.WhenIFindTheMatchingUpstreamBaseUrl())
                .Then(x => x.ThenAnErrorIsReturned())
                .BDDfy();
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