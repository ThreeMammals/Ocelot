using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.ChangeTracking;
using Ocelot.Configuration.Repository;
using Ocelot.Responses;

namespace Ocelot.UnitTests.Configuration;

public class InMemoryConfigurationRepositoryTests : UnitTest
{
    private readonly InMemoryInternalConfigurationRepository _repo;
    private IInternalConfiguration _config;
    private Response _result;
    private Response<IInternalConfiguration> _getResult;
    private readonly Mock<IOcelotConfigurationChangeTokenSource> _changeTokenSource;

    public InMemoryConfigurationRepositoryTests()
    {
        _changeTokenSource = new Mock<IOcelotConfigurationChangeTokenSource>(MockBehavior.Strict);
        _changeTokenSource.Setup(m => m.Activate());
        _repo = new InMemoryInternalConfigurationRepository(_changeTokenSource.Object);
    }

    [Fact]
    public void Can_add_config()
    {
        // Arrange
        _config = new FakeConfig("initial", "adminath");

        // Act
        _result = _repo.AddOrReplace(_config);

        // Assert
        _result.IsError.ShouldBeFalse();
        _changeTokenSource.Verify(m => m.Activate(), Times.Once);
    }

    [Fact]
    public void Can_get_config()
    {
        // Arrange
        _config = new FakeConfig("initial", "adminath");
        _result = _repo.AddOrReplace(_config);

        // Act
        _getResult = _repo.Get();

        // Assert
        _getResult.Data.Routes[0].DownstreamRoute[0].DownstreamPathTemplate.Value.ShouldBe("initial");
    }

    private class FakeConfig : IInternalConfiguration
    {
        private readonly string _downstreamTemplatePath;

        public FakeConfig(string downstreamTemplatePath, string administrationPath)
        {
            _downstreamTemplatePath = downstreamTemplatePath;
            AdministrationPath = administrationPath;
        }

        public List<Route> Routes
        {
            get
            {
                var downstreamRoute = new DownstreamRouteBuilder()
                    .WithDownstreamPathTemplate(_downstreamTemplatePath)
                    .Build();

                return new List<Route>
                {
                    new RouteBuilder()
                        .WithDownstreamRoute(downstreamRoute)
                        .WithUpstreamHttpMethod(new List<string> {"Get"})
                        .Build(),
                };
            }
        }

        public string AdministrationPath { get; }
        public ServiceProviderConfiguration ServiceProviderConfiguration => throw new NotImplementedException();
        public string RequestId { get; }
        public LoadBalancerOptions LoadBalancerOptions { get; }
        public string DownstreamScheme { get; }
        public QoSOptions QoSOptions { get; }
        public HttpHandlerOptions HttpHandlerOptions { get; }
        public Version DownstreamHttpVersion { get; }
        public HttpVersionPolicy? DownstreamHttpVersionPolicy { get; }
    }
}
