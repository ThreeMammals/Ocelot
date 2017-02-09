using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;
using Ocelot.Configuration;
using Ocelot.Errors;
using Ocelot.Infrastructure.Claims.Parser;
using Ocelot.QueryStrings;
using Ocelot.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.QueryStrings
{
    public class AddQueriesToRequestTests
    {
        private readonly AddQueriesToRequest _addQueriesToRequest;
        private readonly Mock<IClaimsParser> _parser;
        private List<ClaimToThing> _configuration;
        private HttpContext _context;
        private Response _result;
        private Response<string> _claimValue;

        public AddQueriesToRequestTests()
        {
            _parser = new Mock<IClaimsParser>();
            _addQueriesToRequest = new AddQueriesToRequest(_parser.Object);
        }

        [Fact]
        public void should_add_queries_to_context()
        {
            var context = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                {
                    new Claim("test", "data")
                }))
            };

            this.Given(
                x => x.GivenAClaimToThing(new List<ClaimToThing>
                {
                    new ClaimToThing("query-key", "", "", 0)
                }))
                .Given(x => x.GivenHttpContext(context))
                .And(x => x.GivenTheClaimParserReturns(new OkResponse<string>("value")))
                .When(x => x.WhenIAddQueriesToTheRequest())
                .Then(x => x.ThenTheResultIsSuccess())
                .And(x => x.ThenTheQueryIsAdded())
                .BDDfy();
        }

        [Fact]
        public void if_query_exists_should_replace_it()
        {
            var context = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                {
                    new Claim("test", "data")
                })),
            };

            context.Request.QueryString = context.Request.QueryString.Add("query-key", "initial");

            this.Given(
                x => x.GivenAClaimToThing(new List<ClaimToThing>
                {
                    new ClaimToThing("query-key", "", "", 0)
                }))
                .Given(x => x.GivenHttpContext(context))
                .And(x => x.GivenTheClaimParserReturns(new OkResponse<string>("value")))
                .When(x => x.WhenIAddQueriesToTheRequest())
                .Then(x => x.ThenTheResultIsSuccess())
                .And(x => x.ThenTheQueryIsAdded())
                .BDDfy();
        }

        [Fact]
        public void should_return_error()
        {
            this.Given(
               x => x.GivenAClaimToThing(new List<ClaimToThing>
               {
                    new ClaimToThing("", "", "", 0)
               }))
               .Given(x => x.GivenHttpContext(new DefaultHttpContext()))
               .And(x => x.GivenTheClaimParserReturns(new ErrorResponse<string>(new List<Error>
               {
                   new AnyError()
               })))
               .When(x => x.WhenIAddQueriesToTheRequest())
               .Then(x => x.ThenTheResultIsError())
               .BDDfy();
        }

        private void ThenTheQueryIsAdded()
        {
            var query = _context.Request.Query.First(x => x.Key == "query-key");
            query.Value.First().ShouldBe(_claimValue.Data);
        }

        private void GivenAClaimToThing(List<ClaimToThing> configuration)
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

        private void WhenIAddQueriesToTheRequest()
        {
            _result = _addQueriesToRequest.SetQueriesOnContext(_configuration, _context);
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
