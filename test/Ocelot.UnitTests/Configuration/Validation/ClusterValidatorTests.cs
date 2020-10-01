using FluentValidation.Results;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Validator;
using Shouldly;
using System.Collections.Generic;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Configuration.Validation
{
    public class ClusterValidatorTests
    {
        private ClusterValidator _validator;
        private ValidationResult _result;
        private FileCluster _cluster;

        public ClusterValidatorTests()
        {
            _validator = new ClusterValidator();
        }

        [Fact]
        public void should_be_valid_because_destinations_empty()
        {
            var cluster = new FileCluster
            {
            };

            this.Given(_ => GivenThe(cluster))
                .When(_ => WhenIValidate())
                .Then(_ => ThenTheErorrIs())
                .BDDfy();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void should_be_invalid_because_destinations_empty(string host)
        {
            var cluster = new FileCluster
            {
                Destinations = new Dictionary<string, FileDestination>
                {
                    {
                        $"cluster1/destination1", new FileDestination
                            {
                                Address = host,
                            }
                    },
                },
            };

            this.Given(_ => GivenThe(cluster))
               .When(_ => WhenIValidate())
               .Then(_ => ThenTheResultIsInValid())
               .And(_ => ThenTheErorrIs())
               .BDDfy();
        }

        [Fact]
        public void should_be_valid_because_destinations_set()
        {
            var cluster = new FileCluster
            {
                Destinations = new Dictionary<string, FileDestination>
                {
                    {
                        $"cluster1/destination1", new FileDestination
                            {
                                Address = $"http://localhost:80",
                            }
                    },      
                },
            };

            this.Given(_ => GivenThe(cluster))
                .When(_ => WhenIValidate())
                .Then(_ => ThenTheResultIsValid())
                .BDDfy();
        }

        private void GivenThe(FileCluster cluster)
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
            //TODO: Improve the assertions in this validator to give the user an idea of what is wrong
            _result.Errors[0].ErrorMessage.ShouldBe("When not using service discovery Cluster.Destinations must be set or Ocelot cannot find your service!");
        }

        private void ThenTheResultIsInValid()
        {
            _result.IsValid.ShouldBeFalse();
        }
    }
}
