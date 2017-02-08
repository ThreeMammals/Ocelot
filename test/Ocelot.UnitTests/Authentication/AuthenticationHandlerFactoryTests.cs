using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Moq;
using Ocelot.Authentication.Handler;
using Ocelot.Authentication.Handler.Creator;
using Ocelot.Authentication.Handler.Factory;
using Ocelot.Configuration.Builder;
using Ocelot.Errors;
using Ocelot.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;
using AuthenticationOptions = Ocelot.Configuration.AuthenticationOptions;

namespace Ocelot.UnitTests.Authentication
{
    public class AuthenticationHandlerFactoryTests
    {
        private readonly IAuthenticationHandlerFactory _authenticationHandlerFactory;
        private readonly Mock<IApplicationBuilder> _app;
        private readonly Mock<IAuthenticationHandlerCreator> _creator;
        private AuthenticationOptions _authenticationOptions;
        private Response<AuthenticationHandler> _result;

        public AuthenticationHandlerFactoryTests()
        {
            _app = new Mock<IApplicationBuilder>();
            _creator = new Mock<IAuthenticationHandlerCreator>();
            _authenticationHandlerFactory = new AuthenticationHandlerFactory(_creator.Object);
        }

        [Fact]
        public void should_return_identity_server_access_token_handler()
        {
            var authenticationOptions = new AuthenticationOptionsBuilder()
                .WithProvider("IdentityServer")
                .Build();

            this.Given(x => x.GivenTheAuthenticationOptionsAre(authenticationOptions))
                .And(x => x.GivenTheCreatorReturns())
                .When(x => x.WhenIGetFromTheFactory())
                .Then(x => x.ThenTheHandlerIsReturned("IdentityServer"))
                .BDDfy();
        }

        [Fact]
        public void should_return_error_if_cannot_create_handler()
        {
             var authenticationOptions = new AuthenticationOptionsBuilder()
                .Build();

            this.Given(x => x.GivenTheAuthenticationOptionsAre(authenticationOptions))
                .And(x => x.GivenTheCreatorReturnsAnError())
                .When(x => x.WhenIGetFromTheFactory())
                .Then(x => x.ThenAnErrorResponseIsReturned())
                .BDDfy();
        }

        private void GivenTheAuthenticationOptionsAre(AuthenticationOptions authenticationOptions)
        {
            _authenticationOptions = authenticationOptions;
        }

        private void GivenTheCreatorReturnsAnError()
        {
            _creator
                .Setup(x => x.Create(It.IsAny<IApplicationBuilder>(), It.IsAny<AuthenticationOptions>()))
                .Returns(new ErrorResponse<RequestDelegate>(new List<Error>
            {
                new UnableToCreateAuthenticationHandlerError($"Unable to create authentication handler for xxx")
            }));
        }

        private void GivenTheCreatorReturns()
        {
            _creator
                .Setup(x => x.Create(It.IsAny<IApplicationBuilder>(), It.IsAny<AuthenticationOptions>()))
                .Returns(new OkResponse<RequestDelegate>(x => Task.CompletedTask));
        }

        private void WhenIGetFromTheFactory()
        {
            _result = _authenticationHandlerFactory.Get(_app.Object, _authenticationOptions);
        }

        private void ThenTheHandlerIsReturned(string expected)
        {
            _result.Data.Provider.ShouldBe(expected);
        }

        private void ThenAnErrorResponseIsReturned()
        {
            _result.IsError.ShouldBeTrue();
        }
    }
}
