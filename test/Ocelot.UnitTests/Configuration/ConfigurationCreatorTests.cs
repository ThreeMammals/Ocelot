using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;

namespace Ocelot.UnitTests.Configuration;

public class ConfigurationCreatorTests : UnitTest
{
    private ConfigurationCreator _creator;
    private InternalConfiguration _result;
    private readonly Mock<IServiceProviderConfigurationCreator> _spcCreator;
    private readonly Mock<IQoSOptionsCreator> _qosCreator;
    private readonly Mock<IHttpHandlerOptionsCreator> _hhoCreator;
    private readonly Mock<ILoadBalancerOptionsCreator> _lboCreator;
    private readonly Mock<IVersionCreator> _vCreator;
    private readonly Mock<IVersionPolicyCreator> _vpCreator;
    private readonly Mock<IMetadataCreator> _mdCreator;
    private readonly Mock<IRateLimitOptionsCreator> _rlCreator;
    private readonly Mock<ICacheOptionsCreator> _coCreator;
    private FileConfiguration _fileConfig;
    private Route[] _routes;
    private ServiceProviderConfiguration _spc;
    private LoadBalancerOptions _lbo;
    private QoSOptions _qoso;
    private HttpHandlerOptions _hho;
    private AdministrationPath _adminPath;
    private readonly ServiceCollection _serviceCollection;

    public ConfigurationCreatorTests()
    {
        _vCreator = new Mock<IVersionCreator>();
        _vpCreator = new Mock<IVersionPolicyCreator>();
        _lboCreator = new Mock<ILoadBalancerOptionsCreator>();
        _hhoCreator = new Mock<IHttpHandlerOptionsCreator>();
        _qosCreator = new Mock<IQoSOptionsCreator>();
        _spcCreator = new Mock<IServiceProviderConfigurationCreator>();
        _mdCreator = new Mock<IMetadataCreator>();
        _rlCreator = new Mock<IRateLimitOptionsCreator>();
        _coCreator = new Mock<ICacheOptionsCreator>();
        _serviceCollection = new ServiceCollection();
    }

    [Fact]
    public void Should_build_configuration_with_no_admin_path()
    {
        // Arrange
        GivenTheDependenciesAreSetUp();

        // Act
        WhenICreate();

        // Assert
        ThenTheDepdenciesAreCalledCorrectly();
        ThenThePropertiesAreSetCorrectly();
        _result.AdministrationPath.ShouldBeNull();
    }

    [Fact]
    public void Should_build_configuration_with_admin_path()
    {
        // Arrange
        GivenTheDependenciesAreSetUp();
        GivenTheAdminPath();

        // Act
        WhenICreate();

        // Assert
        ThenTheDepdenciesAreCalledCorrectly();
        ThenThePropertiesAreSetCorrectly();
        ThenTheAdminPathIsSet();
    }

    [Fact]
    public void Configuration_GlobalConfiguration_SoftNullGuard()
    {
        // Arrange
        GivenTheDependenciesAreSetUp();
        _fileConfig.GlobalConfiguration = null;

        // Act
        WhenICreate();

        // Assert
        ThenTheDepdenciesAreCalledCorrectly();
        ThenThePropertiesAreSetCorrectly();
    }

    private void ThenThePropertiesAreSetCorrectly()
    {
        _fileConfig.GlobalConfiguration ??= new();
        _result.ShouldNotBeNull();
        _result.ServiceProviderConfiguration.ShouldBe(_spc);
        _result.LoadBalancerOptions.ShouldBe(_lbo);
        _result.QoSOptions.ShouldBe(_qoso);
        _result.HttpHandlerOptions.ShouldBe(_hho);
        _result.Routes.ShouldBe(_routes);
        _result.RequestId.ShouldBe(_fileConfig.GlobalConfiguration.RequestIdKey);
        _result.DownstreamScheme.ShouldBe(_fileConfig.GlobalConfiguration.DownstreamScheme);
    }

    private void ThenTheAdminPathIsSet()
    {
        _result.AdministrationPath.ShouldBe("wooty");
    }

    private void ThenTheDepdenciesAreCalledCorrectly()
    {
        _spcCreator.Verify(x => x.Create(It.IsAny<FileGlobalConfiguration>()), Times.Once);
        _lboCreator.Verify(x => x.Create(It.IsAny<FileLoadBalancerOptions>()), Times.Once);
        _qosCreator.Verify(x => x.Create(It.IsAny<FileQoSOptions>()), Times.Once);
        _hhoCreator.Verify(x => x.Create(It.IsAny<FileHttpHandlerOptions>()), Times.Once);
        _vCreator.Verify(x => x.Create(It.IsAny<string>()), Times.Once);
        _vpCreator.Verify(x => x.Create(It.IsAny<string>()), Times.Once);
        _mdCreator.Verify(x => x.Create(It.IsAny<IDictionary<string, string>>(), It.IsAny<FileGlobalConfiguration>()), Times.Once);
        _vCreator.Verify(x => x.Create(It.IsAny<string>()), Times.Once);
        _rlCreator.Verify(x => x.Create(It.IsAny<FileGlobalConfiguration>()), Times.Once);
    }

    private void GivenTheAdminPath()
    {
        _adminPath = new AdministrationPath("wooty");
        _serviceCollection.AddSingleton<IAdministrationPath>(_adminPath);
    }

    private void GivenTheDependenciesAreSetUp()
    {
        _fileConfig = new FileConfiguration
        {
            GlobalConfiguration = new FileGlobalConfiguration(),
        };
        _routes = Array.Empty<Route>();
        _spc = new ServiceProviderConfiguration(string.Empty, string.Empty, string.Empty, 1, string.Empty, string.Empty, 1);
        _lbo = new();
        _qoso = new QoSOptionsBuilder().Build();
        _hho = new HttpHandlerOptions();

        _spcCreator.Setup(x => x.Create(It.IsAny<FileGlobalConfiguration>())).Returns(_spc);
        _lboCreator.Setup(x => x.Create(It.IsAny<FileLoadBalancerOptions>())).Returns(_lbo);
        _qosCreator.Setup(x => x.Create(It.IsAny<FileQoSOptions>())).Returns(_qoso);
        _hhoCreator.Setup(x => x.Create(It.IsAny<FileHttpHandlerOptions>())).Returns(_hho);
    }

    private void WhenICreate()
    {
        var serviceProvider = _serviceCollection.BuildServiceProvider(true);
        _creator = new ConfigurationCreator(
            _spcCreator.Object,
            _qosCreator.Object,
            _hhoCreator.Object,
            serviceProvider,
            _lboCreator.Object,
            _vCreator.Object,
            _vpCreator.Object,
            _mdCreator.Object,
            _rlCreator.Object,
            _coCreator.Object);
        _result = _creator.Create(_fileConfig, _routes);
    }
}
