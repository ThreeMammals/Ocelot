using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Configuration
{
    public class QoSOptionsCreatorTests
    {
        private QoSOptionsCreator _creator;
        private FileReRoute _fileReRoute;
        private QoSOptions _result;

        public QoSOptionsCreatorTests()
        {
            _creator = new QoSOptionsCreator();
        }

        [Fact]
        public void should_create_qos_options()
        {
            var reRoute = new FileReRoute
            {
                QoSOptions = new FileQoSOptions
                {
                    ExceptionsAllowedBeforeBreaking = 1,
                    DurationOfBreak = 1,
                    TimeoutValue = 1
                }
            };
            var expected = new QoSOptionsBuilder()
                .WithDurationOfBreak(1)
                .WithExceptionsAllowedBeforeBreaking(1)
                .WithTimeoutValue(1)
                .Build();

            this.Given(x => x.GivenTheFollowingReRoute(reRoute))
                .When(x => x.WhenICreate())
                .Then(x => x.ThenTheFollowingIsReturned(expected))
                .BDDfy();
        }

        private void GivenTheFollowingReRoute(FileReRoute fileReRoute)
        {
            _fileReRoute = fileReRoute;
        }

        private void WhenICreate()
        {
            _result = _creator.Create(_fileReRoute.QoSOptions);
        }

        private void ThenTheFollowingIsReturned(QoSOptions expected)
        {
            _result.DurationOfBreak.ShouldBe(expected.DurationOfBreak);
            _result.ExceptionsAllowedBeforeBreaking.ShouldBe(expected.ExceptionsAllowedBeforeBreaking);
            _result.TimeoutValue.ShouldBe(expected.TimeoutValue);
        }
    }
}
