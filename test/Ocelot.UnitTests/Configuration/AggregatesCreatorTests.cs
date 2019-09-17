namespace Ocelot.UnitTests.Configuration
{
    using Moq;
    using Ocelot.Configuration;
    using Ocelot.Configuration.Builder;
    using Ocelot.Configuration.Creator;
    using Ocelot.Configuration.File;
    using Shouldly;
    using System.Collections.Generic;
    using System.Net.Http;
    using TestStack.BDDfy;
    using Values;
    using Xunit;

    public class AggregatesCreatorTests
    {
        private readonly AggregatesCreator _creator;
        private readonly Mock<IUpstreamTemplatePatternCreator> _utpCreator;
        private FileConfiguration _fileConfiguration;
        private List<ReRoute> _reRoutes;
        private List<ReRoute> _result;
        private UpstreamPathTemplate _aggregate1Utp;
        private UpstreamPathTemplate _aggregate2Utp;

        public AggregatesCreatorTests()
        {
            _utpCreator = new Mock<IUpstreamTemplatePatternCreator>();
            _creator = new AggregatesCreator(_utpCreator.Object);
        }

        [Fact]
        public void should_return_no_aggregates()
        {
            var fileConfig = new FileConfiguration
            {
                Aggregates = new List<FileAggregateReRoute>
                {
                    new FileAggregateReRoute
                    {
                        ReRouteKeys = new List<string>{"key1"}
                    }
                }
            };
            var reRoutes = new List<ReRoute>();

            this.Given(_ => GivenThe(fileConfig))
                .And(_ => GivenThe(reRoutes))
                .When(_ => WhenICreate())
                .Then(_ => TheUtpCreatorIsNotCalled())
                .And(_ => ThenTheResultIsNotNull())
                .And(_ => ThenTheResultIsEmpty())
                .BDDfy();
        }

        [Fact]
        public void should_create_aggregates()
        {
            var fileConfig = new FileConfiguration
            {
                Aggregates = new List<FileAggregateReRoute>
                {
                    new FileAggregateReRoute
                    {
                        ReRouteKeys = new List<string>{"key1", "key2"},
                        UpstreamHost = "hosty",
                        UpstreamPathTemplate = "templatey",
                        Aggregator = "aggregatory",
                        ReRouteIsCaseSensitive = true
                    },
                    new FileAggregateReRoute
                    {
                        ReRouteKeys = new List<string>{"key3", "key4"},
                        UpstreamHost = "hosty",
                        UpstreamPathTemplate = "templatey",
                        Aggregator = "aggregatory",
                        ReRouteIsCaseSensitive = true
                    }
                }
            };

            var reRoutes = new List<ReRoute>
            {
                new ReRouteBuilder().WithDownstreamReRoute(new DownstreamReRouteBuilder().WithKey("key1").Build()).Build(),
                new ReRouteBuilder().WithDownstreamReRoute(new DownstreamReRouteBuilder().WithKey("key2").Build()).Build(),
                new ReRouteBuilder().WithDownstreamReRoute(new DownstreamReRouteBuilder().WithKey("key3").Build()).Build(),
                new ReRouteBuilder().WithDownstreamReRoute(new DownstreamReRouteBuilder().WithKey("key4").Build()).Build()
            };

            this.Given(_ => GivenThe(fileConfig))
                .And(_ => GivenThe(reRoutes))
                .And(_ => GivenTheUtpCreatorReturns())
                .When(_ => WhenICreate())
                .Then(_ => ThenTheUtpCreatorIsCalledCorrectly())
                .And(_ => ThenTheAggregatesAreCreated())
                .BDDfy();
        }

        private void ThenTheAggregatesAreCreated()
        {
            _result.ShouldNotBeNull();
            _result.Count.ShouldBe(2);

            _result[0].UpstreamHttpMethod.ShouldContain(x => x == HttpMethod.Get);
            _result[0].UpstreamHost.ShouldBe(_fileConfiguration.Aggregates[0].UpstreamHost);
            _result[0].UpstreamTemplatePattern.ShouldBe(_aggregate1Utp);
            _result[0].Aggregator.ShouldBe(_fileConfiguration.Aggregates[0].Aggregator);
            _result[0].DownstreamReRoute.ShouldContain(x => x == _reRoutes[0].DownstreamReRoute[0]);
            _result[0].DownstreamReRoute.ShouldContain(x => x == _reRoutes[1].DownstreamReRoute[0]);

            _result[1].UpstreamHttpMethod.ShouldContain(x => x == HttpMethod.Get);
            _result[1].UpstreamHost.ShouldBe(_fileConfiguration.Aggregates[1].UpstreamHost);
            _result[1].UpstreamTemplatePattern.ShouldBe(_aggregate2Utp);
            _result[1].Aggregator.ShouldBe(_fileConfiguration.Aggregates[1].Aggregator);
            _result[1].DownstreamReRoute.ShouldContain(x => x == _reRoutes[2].DownstreamReRoute[0]);
            _result[1].DownstreamReRoute.ShouldContain(x => x == _reRoutes[3].DownstreamReRoute[0]);
        }

        private void ThenTheUtpCreatorIsCalledCorrectly()
        {
            _utpCreator.Verify(x => x.Create(_fileConfiguration.Aggregates[0]), Times.Once);
            _utpCreator.Verify(x => x.Create(_fileConfiguration.Aggregates[1]), Times.Once);
        }

        private void GivenTheUtpCreatorReturns()
        {
            _aggregate1Utp = new UpstreamPathTemplateBuilder().Build();
            _aggregate2Utp = new UpstreamPathTemplateBuilder().Build();

            _utpCreator.SetupSequence(x => x.Create(It.IsAny<IReRoute>()))
                .Returns(_aggregate1Utp)
                .Returns(_aggregate2Utp);
        }

        private void ThenTheResultIsEmpty()
        {
            _result.Count.ShouldBe(0);
        }

        private void ThenTheResultIsNotNull()
        {
            _result.ShouldNotBeNull();
        }

        private void TheUtpCreatorIsNotCalled()
        {
            _utpCreator.Verify(x => x.Create(It.IsAny<FileAggregateReRoute>()), Times.Never);
        }

        private void GivenThe(FileConfiguration fileConfiguration)
        {
            _fileConfiguration = fileConfiguration;
        }

        private void GivenThe(List<ReRoute> reRoutes)
        {
            _reRoutes = reRoutes;
        }

        private void WhenICreate()
        {
            _result = _creator.Create(_fileConfiguration, _reRoutes);
        }
    }
}
