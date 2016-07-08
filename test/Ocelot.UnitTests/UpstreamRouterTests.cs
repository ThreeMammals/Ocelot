using Ocelot.Library.Infrastructure.Responses;
using Ocelot.Library.Infrastructure.Router.UpstreamRouter;
using Shouldly;
using Xunit;

namespace Ocelot.UnitTests
{
    public class RouterTests
    {
        private string _upstreamApiUrl;
        private string _apiKey;
        private IUpstreamRouter _router;
        private Response _response;
        private Response<Route> _getRouteResponse;

        public RouterTests() 
        {
            _router = new InMemoryUpstreamRouter();
        }

        [Fact]
        public void can_add_route()
        {
            GivenIHaveAnUpstreamApi("http://www.someapi.com/api1");
            GivenIWantToRouteRequestsToMyUpstreamApi("api");
            WhenIAddTheConfiguration();
            ThenTheResponseIsSuccesful();
        }

        [Fact]
        public void can_get_route_by_key()
        {
            GivenIHaveSetUpAnApiKeyAndUpstreamUrl("api2", "http://www.someapi.com/api2");
            WhenIRetrieveTheRouteByKey();
            ThenTheRouteIsReturned();
        }
 
        [Fact]
        public void should_return_error_response_when_key_already_used()
        {
            GivenIHaveSetUpAnApiKeyAndUpstreamUrl("api2", "http://www.someapi.com/api2");
            WhenITryToUseTheSameKey();
            ThenTheKeyHasAlreadyBeenUsed();
        }

        [Fact]
        public void should_return_error_response_if_key_doesnt_exist()
        {
            GivenIWantToRouteRequestsToMyUpstreamApi("api");
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
            _getRouteResponse.ShouldBeOfType<ErrorResponse<Route>>();
            _getRouteResponse.Errors[0].Message.ShouldBe("This key does not exist");
        }

        private void WhenIRetrieveTheRouteByKey()
        {
            _getRouteResponse = _router.GetRoute(_apiKey);
        }

        private void ThenTheRouteIsReturned()
        {
            _getRouteResponse.Data.DownstreamUrl.ShouldBe(_apiKey);
            _getRouteResponse.Data.UpstreamUrl.ShouldBe(_upstreamApiUrl);
        }

        private void GivenIHaveSetUpAnApiKeyAndUpstreamUrl(string apiKey, string upstreamUrl)
        {
            GivenIHaveAnUpstreamApi(upstreamUrl);
            GivenIWantToRouteRequestsToMyUpstreamApi(apiKey);
            WhenIAddTheConfiguration();
        }

        private void GivenIHaveAnUpstreamApi(string upstreamApiUrl)
        {
            _upstreamApiUrl = upstreamApiUrl;
        }

        private void GivenIWantToRouteRequestsToMyUpstreamApi(string apiKey)
        {
            _apiKey = apiKey;
        }

        private void WhenIAddTheConfiguration()
        {
            _response = _router.AddRoute(_apiKey, _upstreamApiUrl);
        }

        private void ThenTheResponseIsSuccesful()
        {
            _response.ShouldBeOfType<OkResponse>();
        }
    }
}