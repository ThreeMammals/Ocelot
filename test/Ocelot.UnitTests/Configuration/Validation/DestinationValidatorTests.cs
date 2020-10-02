using FluentValidation.Results;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Validator;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Configuration.Validation
{
    public class DestinationValidatorTests
    {
        private DestinationValidator _validator;
        private ValidationResult _result;
        private FileDestination _cluster;

        public DestinationValidatorTests()
        {
            _validator = new DestinationValidator();
        }   

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void should_be_invalid_because_destinations_empty(string host)
        {
            var destination = new FileDestination
            {
                Address = host,
            };

            this.Given(_ => GivenThe(destination))
               .When(_ => WhenIValidate())
               .Then(_ => ThenTheResultIsInValid())
               .And(_ => ThenTheErorrIs())
               .BDDfy();
        }

        [Fact]
        public void should_be_valid_because_destinations_set()
        {
            var destination = new FileDestination
            {
                Address = $"http://localhost:80",
            };

            this.Given(_ => GivenThe(destination))
                .When(_ => WhenIValidate())
                .Then(_ => ThenTheResultIsValid())
                .BDDfy();
        }

        private void GivenThe(FileDestination cluster)
        {
            _cluster = cluster;
        }

        private void WhenIValidate()
        {
            _result = _validator.Validate(_cluster);
        }

        private void ThenTheResultIsValid()
        {
            _result.IsValid.ShouldBeTrue();
        }

        private void ThenTheErorrIs()
        {
            _result.Errors[0].ErrorMessage.ShouldBe("Address cannot be empty");
        }

        private void ThenTheResultIsInValid()
        {
            _result.IsValid.ShouldBeFalse();
        }
    }
}
