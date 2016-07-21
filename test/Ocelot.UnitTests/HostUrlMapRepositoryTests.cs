using Ocelot.Library.Infrastructure.Responses;
using Ocelot.Library.Infrastructure.HostUrlRepository;
using Shouldly;
using Xunit;

namespace Ocelot.UnitTests
{
    public class HostUrlMapRepositoryTests
    {
        private string _upstreamBaseUrl;
        private string _downstreamBaseUrl;
        private IHostUrlMapRepository _repository;
        private Response _response;
        private Response<HostUrlMap> _getRouteResponse;
 
        public HostUrlMapRepositoryTests() 
        {
            _repository = new InMemoryHostUrlMapRepository();
        }

        [Fact]
        public void can_add_route()
        {
            GivenIHaveAnUpstreamBaseUrl("www.someapi.com");
            GivenIWantToRouteRequestsFromMyDownstreamBaseUrl("api");
            WhenIAddTheConfiguration();
            ThenTheResponseIsSuccesful();
        }

        [Fact]
        public void can_get_route_by_key()
        {
            GivenIHaveSetUpAnApiKeyAndUpstreamUrl("api2", "www.someapi.com");
            WhenIRetrieveTheRouteByKey();
            ThenTheRouteIsReturned();
        }
 
        [Fact]
        public void should_return_error_response_when_key_already_used()
        {
            GivenIHaveSetUpAnApiKeyAndUpstreamUrl("api2", "www.someapi.com");
            WhenITryToUseTheSameKey();
            ThenTheKeyHasAlreadyBeenUsed();
        }

        [Fact]
        public void should_return_error_response_if_key_doesnt_exist()
        {
            GivenIWantToRouteRequestsFromMyDownstreamBaseUrl("api");
            WhenIRetrieveTheRouteByKey();
            ThenTheKeyDoesNotExist();
        }

        private void WhenITryToUseTheSameKey()
        {
            WhenIAddTheConfiguration();
        }

        private void ThenTheKeyHasAlreadyBeenUsed()
        {
            _response.ShouldNotBeNull();
            _response.ShouldBeOfType<ErrorResponse>();
            _response.Errors[0].Message.ShouldBe("This key has already been used");
        }

        private void ThenTheKeyDoesNotExist()
        {
            _getRouteResponse.ShouldNotBeNull();
            _getRouteResponse.ShouldBeOfType<ErrorResponse<HostUrlMap>>();
            _getRouteResponse.Errors[0].Message.ShouldBe("This key does not exist");
        }

        private void WhenIRetrieveTheRouteByKey()
        {
            _getRouteResponse = _repository.GetBaseUrlMap(_downstreamBaseUrl);
        }

        private void ThenTheRouteIsReturned()
        {
            _getRouteResponse.Data.UrlPathTemplate.ShouldBe(_downstreamBaseUrl);
            _getRouteResponse.Data.UpstreamHostUrl.ShouldBe(_upstreamBaseUrl);
        }

        private void GivenIHaveSetUpAnApiKeyAndUpstreamUrl(string apiKey, string upstreamUrl)
        {
            GivenIHaveAnUpstreamBaseUrl(upstreamUrl);
            GivenIWantToRouteRequestsFromMyDownstreamBaseUrl(apiKey);
            WhenIAddTheConfiguration();
        }

        private void GivenIHaveAnUpstreamBaseUrl(string upstreamApiUrl)
        {
            _upstreamBaseUrl = upstreamApiUrl;
        }

        private void GivenIWantToRouteRequestsFromMyDownstreamBaseUrl(string downstreamBaseUrl)
        {
            _downstreamBaseUrl = downstreamBaseUrl;
        }

        private void WhenIAddTheConfiguration()
        {
            _response = _repository.AddBaseUrlMap(new HostUrlMap(_downstreamBaseUrl, _upstreamBaseUrl));
        }

        private void ThenTheResponseIsSuccesful()
        {
            _response.ShouldBeOfType<OkResponse>();
        }
    }
}