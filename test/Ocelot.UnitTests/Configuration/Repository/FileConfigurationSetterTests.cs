using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.Responses;
using Ocelot.UnitTests.Responder;

namespace Ocelot.UnitTests.Configuration.Repository;

public class FileConfigurationSetterTests : UnitTest
{
    private FileConfiguration _fileConfiguration;
    private readonly FileAndInternalConfigurationSetter _configSetter;
    private readonly Mock<IInternalConfigurationRepository> _configRepo;
    private readonly Mock<IInternalConfigurationCreator> _configCreator;
    private Response<IInternalConfiguration> _configuration;
    private readonly Mock<IFileConfigurationRepository> _repo;

    public FileConfigurationSetterTests()
    {
        _repo = new Mock<IFileConfigurationRepository>();
        _configRepo = new Mock<IInternalConfigurationRepository>();
        _configCreator = new Mock<IInternalConfigurationCreator>();
        _configSetter = new FileAndInternalConfigurationSetter(_configRepo.Object, _configCreator.Object, _repo.Object);
    }

    [Fact]
    public async Task Should_set_configuration()
    {
        // Arrange
        _fileConfiguration = new FileConfiguration();
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        var config = new InternalConfiguration()
        {
            AdministrationPath = string.Empty,
            ServiceProviderConfiguration = serviceProviderConfig,
            RequestId = "asdf",
            LoadBalancerOptions = new(),
            DownstreamScheme = string.Empty,
            QoSOptions = new(),
            HttpHandlerOptions = new(),
            DownstreamHttpVersion = new Version("1.1"),
            DownstreamHttpVersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
            MetadataOptions = new(),
            RateLimitOptions = new(),
            Timeout = 111,
        };
        GivenTheRepoSucceeds();
        GivenTheCreatorReturns(new OkResponse<IInternalConfiguration>(config));

        // Act
        await _configSetter.SetAsync(_fileConfiguration, TestContext.Current.CancellationToken);

        // Assert
        ThenTheConfigurationRepositoryIsCalledCorrectly();
    }

    [Fact]
    public async Task Should_throw_if_unable_to_set_ocelot_configuration()
    {
        // Arrange
        _fileConfiguration = new FileConfiguration();
        GivenTheRepoSucceeds();
        var error = new AnyError();
        GivenTheCreatorReturns(new ErrorResponse<IInternalConfiguration>(error));

        // Act & Assert
        await Assert.ThrowsAsync<ConfigurationRepositoryException>(
            () =>_configSetter.SetAsync(_fileConfiguration));
    }

    [Fact]
    public async Task Should_throw_if_repo_set_async_throws()
    {
        // Arrange
        _fileConfiguration = new FileConfiguration();
        _repo.Setup(x => x.SetAsync(It.IsAny<FileConfiguration>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Repo failure"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            () => _configSetter.SetAsync(_fileConfiguration));
    }

    private void GivenTheRepoSucceeds()
    {
        _repo
            .Setup(x => x.SetAsync(It.IsAny<FileConfiguration>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private void GivenTheCreatorReturns(Response<IInternalConfiguration> configuration)
    {
        _configuration = configuration;
        _configCreator
            .Setup(x => x.Create(_fileConfiguration))
            .ReturnsAsync(_configuration);
    }

    private void ThenTheConfigurationRepositoryIsCalledCorrectly()
    {
        _configRepo.Verify(x => x.AddOrReplace(_configuration.Data), Times.Once);
    }
}
