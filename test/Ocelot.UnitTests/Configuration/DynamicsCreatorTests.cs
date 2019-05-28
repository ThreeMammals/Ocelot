namespace Ocelot.UnitTests.Configuration
{
    using Moq;
    using Ocelot.Configuration;
    using Ocelot.Configuration.Builder;
    using Ocelot.Configuration.Creator;
    using Ocelot.Configuration.File;
    using Shouldly;
    using System.Collections.Generic;
    using TestStack.BDDfy;
    using Xunit;

    public class DynamicsCreatorTests
    {
        private readonly DynamicsCreator _creator;
        private readonly Mock<IRateLimitOptionsCreator> _rloCreator;
        private List<ReRoute> _result;
        private FileConfiguration _fileConfig;
        private RateLimitOptions _rlo1;
        private RateLimitOptions _rlo2;

        public DynamicsCreatorTests()
        {
            _rloCreator = new Mock<IRateLimitOptionsCreator>();
            _creator = new DynamicsCreator(_rloCreator.Object);
        }

        [Fact]
        public void should_return_nothing()
        {
            var fileConfig = new FileConfiguration();

            this.Given(_ => GivenThe(fileConfig))
                .When(_ => WhenICreate())
                .Then(_ => ThenNothingIsReturned())
                .And(_ => ThenTheRloCreatorIsNotCalled())
                .BDDfy();
        }

        [Fact]
        public void should_return_re_routes()
        {
            var fileConfig = new FileConfiguration
            {
                DynamicReRoutes = new List<FileDynamicReRoute>
                {
                    new FileDynamicReRoute
                    {
                        ServiceName = "1",
                        RateLimitRule = new FileRateLimitRule
                        {
                            EnableRateLimiting = false
                        }
                    },
                    new FileDynamicReRoute
                    {
                        ServiceName = "2",
                        RateLimitRule = new FileRateLimitRule
                        {
                            EnableRateLimiting = true
                        }
                    }
                }
            };

            this.Given(_ => GivenThe(fileConfig))
                .And(_ => GivenTheRloCreatorReturns())
                .When(_ => WhenICreate())
                .Then(_ => ThenTheReRoutesAreReturned())
                .And(_ => ThenTheRloCreatorIsCalledCorrectly())
                .BDDfy();
        }

        private void ThenTheRloCreatorIsCalledCorrectly()
        {
            _rloCreator.Verify(x => x.Create(_fileConfig.DynamicReRoutes[0].RateLimitRule,
                _fileConfig.GlobalConfiguration), Times.Once);

            _rloCreator.Verify(x => x.Create(_fileConfig.DynamicReRoutes[1].RateLimitRule,
                _fileConfig.GlobalConfiguration), Times.Once);
        }

        private void ThenTheReRoutesAreReturned()
        {
            _result.Count.ShouldBe(2);
            _result[0].DownstreamReRoute[0].EnableEndpointEndpointRateLimiting.ShouldBeFalse();
            _result[0].DownstreamReRoute[0].RateLimitOptions.ShouldBe(_rlo1);
            _result[0].DownstreamReRoute[0].ServiceName.ShouldBe(_fileConfig.DynamicReRoutes[0].ServiceName);

            _result[1].DownstreamReRoute[0].EnableEndpointEndpointRateLimiting.ShouldBeTrue();
            _result[1].DownstreamReRoute[0].RateLimitOptions.ShouldBe(_rlo2);
            _result[1].DownstreamReRoute[0].ServiceName.ShouldBe(_fileConfig.DynamicReRoutes[1].ServiceName);
        }

        private void GivenTheRloCreatorReturns()
        {
            _rlo1 = new RateLimitOptionsBuilder().Build();
            _rlo2 = new RateLimitOptionsBuilder().WithEnableRateLimiting(true).Build();

            _rloCreator
                .SetupSequence(x => x.Create(It.IsAny<FileRateLimitRule>(), It.IsAny<FileGlobalConfiguration>()))
                .Returns(_rlo1)
                .Returns(_rlo2);
        }

        private void ThenTheRloCreatorIsNotCalled()
        {
            _rloCreator.Verify(x => x.Create(It.IsAny<FileRateLimitRule>(), It.IsAny<FileGlobalConfiguration>()), Times.Never);
        }

        private void ThenNothingIsReturned()
        {
            _result.Count.ShouldBe(0);
        }

        private void WhenICreate()
        {
            _result = _creator.Create(_fileConfig);
        }

        private void GivenThe(FileConfiguration fileConfig)
        {
            _fileConfig = fileConfig;
        }
    }
}
