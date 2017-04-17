using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moq;
using Ocelot.Configuration;
using Ocelot.Errors;
using Ocelot.Headers;
using Ocelot.Infrastructure.Claims.Parser;
using Ocelot.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Headers
{
    public class AddHeadersToRequestTests
    {
        private readonly AddHeadersToRequest _addHeadersToRequest;
        private readonly Mock<IClaimsParser> _parser;
        private List<ClaimToThing> _configuration;
        private HttpContext _context;
        private Response _result;
        private Response<string> _claimValue;

        public AddHeadersToRequestTests()
        {
            _parser = new Mock<IClaimsParser>();
            _addHeadersToRequest = new AddHeadersToRequest(_parser.Object);
        }

        [Fact]
        public void should_add_headers_to_context()
        {
            var context = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                {
                    new Claim("test", "data")
                }))
            };

            this.Given(
                x => x.GivenConfigurationHeaderExtractorProperties(new List<ClaimToThing>
                {
                    new ClaimToThing("header-key", "", "", 0)
                }))
                .Given(x => x.GivenHttpContext(context))
                .And(x => x.GivenTheClaimParserReturns(new OkResponse<string>("value")))
                .When(x => x.WhenIAddHeadersToTheRequest())
                .Then(x => x.ThenTheResultIsSuccess())
                .And(x => x.ThenTheHeaderIsAdded())
                .BDDfy();
        }

        [Fact]
        public void if_header_exists_should_replace_it()
        {
            var context = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                {
                    new Claim("test", "data")
                })),
            };

            context.Request.Headers.Add("header-key", new StringValues("initial"));

            this.Given(
                x => x.GivenConfigurationHeaderExtractorProperties(new List<ClaimToThing>
                {
                    new ClaimToThing("header-key", "", "", 0)
                }))
                .Given(x => x.GivenHttpContext(context))
                .And(x => x.GivenTheClaimParserReturns(new OkResponse<string>("value")))
                .When(x => x.WhenIAddHeadersToTheRequest())
                .Then(x => x.ThenTheResultIsSuccess())
                .And(x => x.ThenTheHeaderIsAdded())
                .BDDfy();
        }

        [Fact]
        public void should_return_error()
        {
            this.Given(
               x => x.GivenConfigurationHeaderExtractorProperties(new List<ClaimToThing>
               {
                    new ClaimToThing("", "", "", 0)
               }))
               .Given(x => x.GivenHttpContext(new DefaultHttpContext()))
               .And(x => x.GivenTheClaimParserReturns(new ErrorResponse<string>(new List<Error>
               {
                   new AnyError()
               })))
               .When(x => x.WhenIAddHeadersToTheRequest())
               .Then(x => x.ThenTheResultIsError())
               .BDDfy();
        }

        private void ThenTheHeaderIsAdded()
        {
            var header = _context.Request.Headers.First(x => x.Key == "header-key");
            header.Value.First().ShouldBe(_claimValue.Data);
        }

        private void GivenConfigurationHeaderExtractorProperties(List<ClaimToThing> configuration)
        {
            _configuration = configuration;
        }

        private void GivenHttpContext(HttpContext context)
        {
            _context = context;
        }

        private void GivenTheClaimParserReturns(Response<string> claimValue)
        {
            _claimValue = claimValue;
            _parser
                .Setup(
                    x =>
                        x.GetValue(It.IsAny<IEnumerable<Claim>>(), 
                        It.IsAny<string>(), 
                        It.IsAny<string>(),
                        It.IsAny<int>()))
                .Returns(_claimValue);
        }

        private void WhenIAddHeadersToTheRequest()
        {
            //_result = _addHeadersToRequest.SetHeadersOnContext(_configuration, _context);
            //TODO: pass in DownstreamRequest
            _result = _addHeadersToRequest.SetHeadersOnDownstreamRequest(_configuration, _context.User.Claims, null);
        }

        private void ThenTheResultIsSuccess()
        {
            _result.IsError.ShouldBe(false);
        }

        private void ThenTheResultIsError()
        {

            _result.IsError.ShouldBe(true);
        }

        class AnyError : Error
        {
            public AnyError() 
                : base("blahh", OcelotErrorCode.UnknownError)
            {
            }
        }
    }
}
