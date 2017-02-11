using System;
using System.Collections.Generic;
using Ocelot.Errors;
using Ocelot.Middleware;
using Ocelot.Requester;
using Ocelot.Responder;
using Ocelot.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Responder
{
    public class ErrorsToHttpStatusCodeMapperTests
    {
        private readonly IErrorsToHttpStatusCodeMapper _codeMapper;
        private Response<int> _result;
        private List<Error> _errors;

        public ErrorsToHttpStatusCodeMapperTests()
        {
            _codeMapper = new ErrorsToHttpStatusCodeMapper();
        }

        [Fact]
        public void should_return_timeout()
        {
            this.Given(x => x.GivenThereAreErrors(new List<Error>
                {
                    new RequestTimedOutError(new Exception())
                }))
               .When(x => x.WhenIGetErrorStatusCode())
               .Then(x => x.ThenTheResponseIsStatusCodeIs(503))
               .BDDfy();
        }

        [Fact]
        public void should_create_unauthenticated_response_code()
        {
            this.Given(x => x.GivenThereAreErrors(new List<Error>
                {
                    new UnauthenticatedError("no matter")
                }))
                .When(x => x.WhenIGetErrorStatusCode())
                .Then(x => x.ThenTheResponseIsStatusCodeIs(401))
                .BDDfy();
        }

        [Fact]
        public void should_create_not_found_response_response_code()
        {
            this.Given(x => x.GivenThereAreErrors(new List<Error>
                {
                    new AnyError()
                }))
                .When(x => x.WhenIGetErrorStatusCode())
                .Then(x => x.ThenTheResponseIsStatusCodeIs(404))
                .BDDfy();
        }

        class AnyError : Error
        {
            public AnyError() : base("blahh", OcelotErrorCode.UnknownError)
            {
            }
        }

        private void GivenThereAreErrors(List<Error> errors)
        {
            _errors = errors;
        }

        private void WhenIGetErrorStatusCode()
        {
            _result = _codeMapper.Map(_errors);
        }

        private void ThenTheResponseIsStatusCodeIs(int expectedCode)
        {
            _result.Data.ShouldBe(expectedCode);
        }    
    }
}
