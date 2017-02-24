using Ocelot.Configuration.Authentication;
using Xunit;
using Shouldly;
using TestStack.BDDfy;
using Moq;
using IdentityServer4.Validation;
using Ocelot.Configuration.Provider;
using System.Collections.Generic;

namespace Ocelot.UnitTests.Configuration
{
    public class OcelotResourceOwnerPasswordValidatorTests
    {
        private OcelotResourceOwnerPasswordValidator _validator;
        private Mock<IHashMatcher> _matcher;
        private string _userName;
        private string _password;
        private ResourceOwnerPasswordValidationContext _context;
        private Mock<IIdentityServerConfiguration> _config;
        private User _user;

        public OcelotResourceOwnerPasswordValidatorTests()
        {
            _matcher = new Mock<IHashMatcher>();
            _config = new Mock<IIdentityServerConfiguration>();
            _validator = new OcelotResourceOwnerPasswordValidator(_matcher.Object, _config.Object);
        }

        [Fact]
        public void should_return_success()
        {
            this.Given(x => GivenTheUserName("tom"))
                .And(x => GivenThePassword("password"))
                .And(x => GivenTheUserIs(new User("sub", "tom", "xxx", "xxx")))
                .And(x => GivenTheMatcherReturns(true))
                .When(x => WhenIValidate())
                .Then(x => ThenTheUserIsValidated())
                .And(x => ThenTheMatcherIsCalledCorrectly())
                .BDDfy();
        }

        [Fact]
        public void should_return_fail_when_no_user()
        {
            this.Given(x => GivenTheUserName("bob"))
                .And(x => GivenTheUserIs(new User("sub", "tom", "xxx", "xxx")))
                .And(x => GivenTheMatcherReturns(true))
                .When(x => WhenIValidate())
                .Then(x => ThenTheUserIsNotValidated())
                .BDDfy();
        }

        [Fact]
        public void should_return_fail_when_password_doesnt_match()
        {
            this.Given(x => GivenTheUserName("tom"))
                .And(x => GivenThePassword("password"))
                .And(x => GivenTheUserIs(new User("sub", "tom", "xxx", "xxx")))
                .And(x => GivenTheMatcherReturns(false))
                .When(x => WhenIValidate())
                .Then(x => ThenTheUserIsNotValidated())
                .And(x => ThenTheMatcherIsCalledCorrectly())
                .BDDfy();
        }

        private void ThenTheMatcherIsCalledCorrectly()
        {
            _matcher
                .Verify(x => x.Match(_password, _user.Salt, _user.Hash), Times.Once);
        }

        private void GivenThePassword(string password)
        {
            _password = password;
        }

        private void GivenTheUserIs(User user)
        {
            _user = user;
            _config
                .Setup(x => x.Users)
                .Returns(new List<User>{_user});
        }

        private void GivenTheMatcherReturns(bool expected)
        {
            _matcher
                .Setup(x => x.Match(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(expected);
        }

        private void GivenTheUserName(string userName)
        {
            _userName = userName;
        }

        private void WhenIValidate()
        {
            _context = new ResourceOwnerPasswordValidationContext
            {
                UserName = _userName,
                Password = _password
            };
            _validator.ValidateAsync(_context).Wait();
        }

        private void ThenTheUserIsValidated()
        {
            _context.Result.IsError.ShouldBe(false);
        }

         private void ThenTheUserIsNotValidated()
        {
            _context.Result.IsError.ShouldBe(true);
        }
    }
}