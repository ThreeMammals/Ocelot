using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.Configuration.Setter;
using Ocelot.Errors;
using Ocelot.Responses;

namespace Ocelot.UnitTests.Configuration;

public class FileConfigurationSetterTests : UnitTest
{
    private FileConfiguration _fileConfiguration;
    private readonly FileAndInternalConfigurationSetter _configSetter;
    private readonly Mock<IInternalConfigurationRepository> _configRepo;
    private readonly Mock<IInternalConfigurationCreator> _configCreator;
    private Response<IInternalConfiguration> _configuration;
    private object _result;
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
            QoSOptions = new QoSOptionsBuilder().Build(),
            HttpHandlerOptions = new(),
            DownstreamHttpVersion = new Version("1.1"),
            DownstreamHttpVersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
            MetadataOptions = new(),
            RateLimitOptions = new(),
            Timeout = 111,
        };
        GivenTheRepoReturns(new OkResponse());
        GivenTheCreatorReturns(new OkResponse<IInternalConfiguration>(config));

        // Act
        _result = await _configSetter.Set(_fileConfiguration);

        // Assert
        ThenTheConfigurationRepositoryIsCalledCorrectly();
    }

    [Fact]
    public async Task Should_return_error_if_unable_to_set_file_configuration()
    {
        // Arrange
        _fileConfiguration = new FileConfiguration();
        GivenTheRepoReturns(new ErrorResponse(It.IsAny<Error>()));

        // Act
        _result = await _configSetter.Set(_fileConfiguration);

        // Assert
        _result.ShouldBeOfType<ErrorResponse>();
    }

    [Fact]
    public async Task Should_return_error_if_unable_to_set_ocelot_configuration()
    {
        // Arrange
        _fileConfiguration = new FileConfiguration();
        GivenTheRepoReturns(new OkResponse());
        GivenTheCreatorReturns(new ErrorResponse<IInternalConfiguration>(It.IsAny<Error>()));

        // Act
        _result = await _configSetter.Set(_fileConfiguration);

        // Assert
        _result.ShouldBeOfType<ErrorResponse>();
    }

    private void GivenTheRepoReturns(Response response)
    {
        _repo
            .Setup(x => x.Set(It.IsAny<FileConfiguration>()))
            .ReturnsAsync(response);
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
