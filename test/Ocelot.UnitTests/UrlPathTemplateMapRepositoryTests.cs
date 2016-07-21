using System.Collections.Generic;
using Ocelot.Library.Infrastructure.Responses;
using Ocelot.Library.Infrastructure.UrlPathTemplateRepository;
using Shouldly;
using Xunit;

namespace Ocelot.UnitTests
{
    public class UrlPathTemplateMapRepositoryTests
    {
        private string _upstreamUrlPath; 
        private string _downstreamUrlPath;
        private IUrlPathTemplateMapRepository _repository;
        private Response _response;
        private Response<UrlPathTemplateMap> _getResponse;
        private Response<List<UrlPathTemplateMap>> _listResponse;

        public UrlPathTemplateMapRepositoryTests() 
        {
            _repository = new InMemoryUrlPathTemplateMapRepository();
        }

        [Fact]
        public void can_add_url_path()
        {
            GivenIHaveAnUpstreamUrlPath("/api/products/products/{productId}");
            GivenIWantToRouteRequestsToMyUpstreamUrlPath("/api/products/{productId}");
            WhenIAddTheConfiguration();
            ThenTheResponseIsSuccesful();
        }

        [Fact]
        public void can_get_url_path()
        {
            GivenIHaveSetUpADownstreamUrlPathAndAnUpstreamUrlPath("/api2", "http://www.someapi.com/api2");
            WhenIRetrieveTheUrlPathByDownstreamUrl();
            ThenTheUrlPathIsReturned();
        }

        [Fact]
        public void can_get_all_urls()
        {
            GivenIHaveSetUpADownstreamUrlPathAndAnUpstreamUrlPath("/api2", "http://www.someapi.com/api2");
            WhenIRetrieveTheUrls();
            ThenTheUrlsAreReturned();
        }
 
        [Fact]
        public void should_return_error_response_when_url_path_already_used()
        {
            GivenIHaveSetUpADownstreamUrlPathAndAnUpstreamUrlPath("/api2", "http://www.someapi.com/api2");
            WhenITryToUseTheSameDownstreamUrl();
            ThenTheDownstreamUrlAlreadyBeenUsed();
        }

        [Fact]
        public void should_return_error_response_if_key_doesnt_exist()
        {
            GivenIWantToRouteRequestsToMyUpstreamUrlPath("/api");
            WhenIRetrieveTheUrlPathByDownstreamUrl();
            ThenTheKeyDoesNotExist();
        }

        private void WhenITryToUseTheSameDownstreamUrl()
        {
            WhenIAddTheConfiguration();
        }

        private void ThenTheDownstreamUrlAlreadyBeenUsed()
        {
            _response.ShouldNotBeNull();
            _response.ShouldBeOfType<ErrorResponse>();
            _response.Errors[0].Message.ShouldBe("This key has already been used");
        }

        private void ThenTheKeyDoesNotExist()
        {
            _getResponse.ShouldNotBeNull();
            _getResponse.ShouldBeOfType<ErrorResponse<UrlPathTemplateMap>>();
            _getResponse.Errors[0].Message.ShouldBe("This key does not exist");
        }

        private void WhenIRetrieveTheUrlPathByDownstreamUrl()
        {
            _getResponse = _repository.GetUrlPathTemplateMap(_downstreamUrlPath);
        }

           private void WhenIRetrieveTheUrls()
        {
            _listResponse = _repository.All;
        }

        private void ThenTheUrlPathIsReturned()
        {
            _getResponse.Data.DownstreamUrlPathTemplate.ShouldBe(_downstreamUrlPath);
            _getResponse.Data.UpstreamUrlPathTemplate.ShouldBe(_upstreamUrlPath);
        }

        private void ThenTheUrlsAreReturned()
        {
            _listResponse.Data.Count.ShouldBeGreaterThan(0);
        }

        private void GivenIHaveSetUpADownstreamUrlPathAndAnUpstreamUrlPath(string downstream, string upstreamApiUrl)
        {
            GivenIHaveAnUpstreamUrlPath(upstreamApiUrl);
            GivenIWantToRouteRequestsToMyUpstreamUrlPath(downstream);
            WhenIAddTheConfiguration();
        }

        private void GivenIHaveAnUpstreamUrlPath(string upstreamApiUrl)
        {
            _upstreamUrlPath = upstreamApiUrl;
        }

        private void GivenIWantToRouteRequestsToMyUpstreamUrlPath(string apiKey)
        {
            _downstreamUrlPath = apiKey;
        }

        private void WhenIAddTheConfiguration()
        {
            _response = _repository.AddUrlPathTemplateMap(new UrlPathTemplateMap(_downstreamUrlPath, _upstreamUrlPath));
        }

        private void ThenTheResponseIsSuccesful()
        {
            _response.ShouldBeOfType<OkResponse>();
        }
    }
}