using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
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
    private readonly object _result;
    private readonly Mock<IFileConfigurationRepository> _repo;

    public FileConfigurationSetterTests()
    {
        _repo = new Mock<IFileConfigurationRepository>();
        _configRepo = new Mock<IInternalConfigurationRepository>();
        _configCreator = new Mock<IInternalConfigurationCreator>();
        _configSetter = new FileAndInternalConfigurationSetter(_configRepo.Object, _configCreator.Object, _repo.Object);
    }

    protected static CancellationToken CancelMe => TestContext.Current.CancellationToken;

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
        GivenTheCreatorReturns(new OkResponse<IInternalConfiguration>(config));

        // Act
        await _configSetter.SetAsync(_fileConfiguration, CancelMe);

        // Assert
        ThenTheConfigurationRepositoryIsCalledCorrectly();
    }

    [Fact]
    public async Task Should_throw_exception_if_unable_to_set_file_configuration()
    {
        // Arrange
        _fileConfiguration = new FileConfiguration();
        GivenTheCreatorReturns(new ErrorResponse<IInternalConfiguration>(new FakeError("testMe")));

        // Act
        var e = await Assert.ThrowsAsync<ConfigurationRepositoryException>(
            () => _configSetter.SetAsync(_fileConfiguration, CancelMe));

        // Assert
        Assert.Equal("CannotAddDataError: testMe", e.Message);
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

public class FakeError : Error
{
    public FakeError(string message) : base(message, OcelotErrorCode.CannotAddDataError, 404)
    {
    }
}
