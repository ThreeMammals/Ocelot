using System;
using System.Net.Http;
using Moq;
using Ocelot.Infrastructure;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Middleware;
using Ocelot.Responses;
using Shouldly;
using Xunit;

namespace Ocelot.UnitTests.Infrastructure
{
    public class PlaceholdersTests
    {
        private IPlaceholders _placeholders;
        private Mock<IBaseUrlFinder> _finder;
        private Mock<IRequestScopedDataRepository> _repo;
        
        public PlaceholdersTests()
        {
            _repo = new Mock<IRequestScopedDataRepository>();
            _finder = new Mock<IBaseUrlFinder>();
            _placeholders = new Placeholders(_finder.Object, _repo.Object);
        }

        [Fact]
        public void should_return_base_url()
        {
            var baseUrl = "http://www.bbc.co.uk";
            _finder.Setup(x => x.Find()).Returns(baseUrl);
            var result = _placeholders.Get("{BaseUrl}");
            result.Data.ShouldBe(baseUrl);
        }

        [Fact]
        public void should_return_key_does_not_exist()
        {
            var result = _placeholders.Get("{Test}");
            result.IsError.ShouldBeTrue();
            result.Errors[0].Message.ShouldBe("Unable to find placeholder called {Test}");
        }

        [Fact]
        public void should_return_downstream_base_url_when_port_is_not_80_or_443()
        {
            var request = new HttpRequestMessage();
            request.RequestUri = new Uri("http://www.bbc.co.uk");
            var result = _placeholders.Get("{DownstreamBaseUrl}", request);
            result.Data.ShouldBe("http://www.bbc.co.uk/");
        }


        [Fact]
        public void should_return_downstream_base_url_when_port_is_80_or_443()
        {
              var request = new HttpRequestMessage();
            request.RequestUri = new Uri("http://www.bbc.co.uk:123");
            var result = _placeholders.Get("{DownstreamBaseUrl}", request);
            result.Data.ShouldBe("http://www.bbc.co.uk:123/");
        }

        [Fact]
        public void should_return_key_does_not_exist_for_http_request_message()
        {
            var result = _placeholders.Get("{Test}", new System.Net.Http.HttpRequestMessage());
            result.IsError.ShouldBeTrue();
            result.Errors[0].Message.ShouldBe("Unable to find placeholder called {Test}");
        }

        [Fact]
        public void should_return_trace_id()
        {
            var traceId = "123";
            _repo.Setup(x => x.Get<string>("TraceId")).Returns(new OkResponse<string>(traceId));
            var result = _placeholders.Get("{TraceId}");
            result.Data.ShouldBe(traceId);
        }
    }
}