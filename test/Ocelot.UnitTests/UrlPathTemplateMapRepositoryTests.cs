using System.Collections.Generic;
using Ocelot.Library.Infrastructure.Responses;
using Ocelot.Library.Infrastructure.UrlTemplateRepository;
using Shouldly;
using Xunit;

namespace Ocelot.UnitTests
{
    using TestStack.BDDfy;

    public class UrlPathTemplateMapRepositoryTests
    {
        private string _upstreamUrlPathTemplate; 
        private string _downstreamUrlTemplate;
        private IUrlTemplateMapRepository _repository;
        private Response _response;
        private Response<List<UrlTemplateMap>> _listResponse;

        public UrlPathTemplateMapRepositoryTests() 
        {
            _repository = new InMemoryUrlTemplateMapRepository();
        }

        [Fact]
        public void can_add_url_path()
        {
            this.Given(x => x.GivenIHaveAnUpstreamUrlPathTemplate("/api/products/products/{productId}"))
                .And(x => x.GivenADownstreamUrlTemplate("/api/products/{productId}"))
                .When(x => x.WhenIAddTheConfiguration())
                .Then(x => x.ThenTheResponseIsSuccesful())
                .BDDfy();
        }

        [Fact]
        public void can_get_all_urls()
        {
            this.Given(x => x.GivenIHaveSetUpADownstreamUrlTemplateAndAnUpstreamUrlPathTemplate("/api2", "http://www.someapi.com/api2"))
                 .When(x => x.WhenIRetrieveTheUrls())
                 .Then(x => x.ThenTheUrlsAreReturned())
                 .BDDfy();
        }
 
        [Fact]
        public void should_return_error_response_when_url_path_already_used()
        {
            this.Given(x => x.GivenIHaveSetUpADownstreamUrlTemplateAndAnUpstreamUrlPathTemplate("/api2", "http://www.someapi.com/api2"))
                 .When(x => x.WhenITryToUseTheSameDownstreamUrl())
                 .Then(x => x.ThenTheDownstreamUrlAlreadyBeenUsed())
                 .BDDfy();
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

        private void WhenIRetrieveTheUrls()
        {
            _listResponse = _repository.All;
        }

        private void ThenTheUrlsAreReturned()
        {
            _listResponse.Data.Count.ShouldBeGreaterThan(0);
        }

        private void GivenIHaveSetUpADownstreamUrlTemplateAndAnUpstreamUrlPathTemplate(string downstreamUrlTemplate, string upstreamUrlPathTemplate)
        {
            GivenIHaveAnUpstreamUrlPathTemplate(upstreamUrlPathTemplate);
            GivenADownstreamUrlTemplate(downstreamUrlTemplate);
            WhenIAddTheConfiguration();
        }

        private void GivenIHaveAnUpstreamUrlPathTemplate(string upstreamUrlPathTemplate)
        {
            _upstreamUrlPathTemplate = upstreamUrlPathTemplate;
        }

        private void GivenADownstreamUrlTemplate(string downstreamUrlTemplate)
        {
            _downstreamUrlTemplate = downstreamUrlTemplate;
        }

        private void WhenIAddTheConfiguration()
        {
            _response = _repository.AddUrlTemplateMap(new UrlTemplateMap(_downstreamUrlTemplate, _upstreamUrlPathTemplate));
        }

        private void ThenTheResponseIsSuccesful()
        {
            _response.ShouldBeOfType<OkResponse>();
        }
    }
}