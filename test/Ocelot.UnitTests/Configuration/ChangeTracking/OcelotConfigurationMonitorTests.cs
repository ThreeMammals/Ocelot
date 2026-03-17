using Microsoft.Extensions.Primitives;
using Ocelot.Configuration;
using Ocelot.Configuration.ChangeTracking;
using Ocelot.Configuration.Repository;
using Ocelot.Responses;

namespace Ocelot.UnitTests.Configuration.ChangeTracking;

public class OcelotConfigurationMonitorTests : UnitTest
{
    private Mock<IInternalConfigurationRepository> _mConfigurationRepo;
    private Mock<IOcelotConfigurationChangeTokenSource> _mChangeTokenSource;
    private Mock<IChangeToken> _mChangeToken;
    private IInternalConfiguration _testConfiguration;
    private Response<IInternalConfiguration> _repoResponse;
    private OcelotConfigurationMonitor _monitor;

    public OcelotConfigurationMonitorTests()
    {
        GivenTheRepositoryMock();
        GivenTheChangeTokenSourceMock();
        GivenTheChangeTokenMock();
    }

    [Fact]
    public void Constructor_WithValidDependencies_ShouldCreateInstance()
    {
        // Arrange
        var repo = _mConfigurationRepo.Object;
        var changeTokenSource = _mChangeTokenSource.Object;

        // Act
        var monitor = new OcelotConfigurationMonitor(repo, changeTokenSource);

        // Assert
        Assert.NotNull(monitor);
        Assert.IsType<OcelotConfigurationMonitor>(monitor);
    }

    [Fact]
    public void Constructor_WithNullRepository_ShouldThrowArgumentNullException()
    {
        // Arrange
        IInternalConfigurationRepository repo = null;

        // Act & Assert
        var e = Assert.Throws<ArgumentNullException>(
            () => new OcelotConfigurationMonitor(repo, _mChangeTokenSource.Object));

        // Assert
        Assert.NotNull(e);
        Assert.Equal(nameof(repo), e.ParamName);
    }

    [Fact]
    public void Constructor_WithNullChangeTokenSource_ShouldThrowArgumentNullException()
    {
        // Arrange
        IOcelotConfigurationChangeTokenSource changeTokenSource = null;

        // Act & Assert
        var e = Assert.Throws<ArgumentNullException>(
            () => new OcelotConfigurationMonitor(_mConfigurationRepo.Object, changeTokenSource));

        // Assert
        Assert.NotNull(e);
        Assert.Equal(nameof(changeTokenSource), e.ParamName);
    }

    [Fact]
    public void CurrentValue_WhenCalled_ShouldReturnConfigurationFromRepository()
    {
        // Arrange
        GivenATestConfigurationIsSet();
        GivenTheMonitorIsCreated();

        // Act
        var result = _monitor.CurrentValue;

        // Assert
        Assert.NotNull(result);
        Assert.Same(_testConfiguration, result);
        _mConfigurationRepo.Verify(x => x.Get(), Times.Once);
    }

    [Fact]
    public void Get_WithValidName_ShouldReturnConfigurationFromRepository()
    {
        // Arrange
        GivenATestConfigurationIsSet();
        GivenTheMonitorIsCreated();
        const string configName = "test-config";

        // Act
        var result = _monitor.Get(configName);

        // Assert
        Assert.NotNull(result);
        Assert.Same(_testConfiguration, result);
        _mConfigurationRepo.Verify(x => x.Get(), Times.Once);
    }

    [Fact]
    public void Get_WithEmptyName_ShouldReturnConfigurationFromRepository()
    {
        // Arrange
        GivenATestConfigurationIsSet();
        GivenTheMonitorIsCreated();

        // Act
        var result = _monitor.Get(string.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Same(_testConfiguration, result);
        _mConfigurationRepo.Verify(x => x.Get(), Times.Once);
    }

    [Fact]
    public void Get_WithNullName_ShouldReturnConfigurationFromRepository()
    {
        // Arrange
        GivenATestConfigurationIsSet();
        GivenTheMonitorIsCreated();

        // Act
        var result = _monitor.Get(null);

        // Assert
        Assert.NotNull(result);
        Assert.Same(_testConfiguration, result);
        _mConfigurationRepo.Verify(x => x.Get(), Times.Once);
    }

    [Fact]
    public void OnChange_WithValidListener_ShouldRegisterCallbackOnChangeToken()
    {
        // Arrange
        GivenATestConfigurationIsSet();
        GivenTheMonitorIsCreated();
        var callbackInvoked = false;
        void listener(IInternalConfiguration config, string name)
        {
            callbackInvoked = true;
        }

        // Act
        var disposable = _monitor.OnChange(listener);

        // Assert
        Assert.NotNull(disposable);
        _mChangeToken.Verify(
            x => x.RegisterChangeCallback(It.IsAny<Action<object>>(), It.IsAny<object>()),
            Times.Once
        );
        Assert.False(callbackInvoked);
    }

    [Fact]
    public void OnChange_ShouldReturnDisposableWrapper()
    {
        // Arrange
        GivenATestConfigurationIsSet();
        GivenTheMonitorIsCreated();
        var mockDisposable = new Mock<IDisposable>();
        _mChangeToken
            .Setup(x => x.RegisterChangeCallback(It.IsAny<Action<object>>(), It.IsAny<object>()))
            .Returns(mockDisposable.Object);

        // Act
        var result = _monitor.OnChange((config, name) => { });

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockDisposable.Object, result);
    }

    [Fact]
    public void OnChange_WhenChangeTokenCallbackIsInvoked_ShouldCallListenerWithCurrentValueAndEmptyString()
    {
        // Arrange
        GivenATestConfigurationIsSet();
        GivenTheMonitorIsCreated();
        IInternalConfiguration capturedConfig = null;
        string capturedName = null;
        void listener(IInternalConfiguration config, string name)
        {
            capturedConfig = config;
            capturedName = name;
        }

        Action<object> capturedCallback = null;
        _mChangeToken
            .Setup(x => x.RegisterChangeCallback(It.IsAny<Action<object>>(), It.IsAny<object>()))
            .Callback<Action<object>, object>((callback, state) => capturedCallback = callback)
            .Returns(new Mock<IDisposable>().Object);

        // Act
        _monitor.OnChange(listener);
        capturedCallback?.Invoke(null);

        // Assert
        Assert.NotNull(capturedConfig);
        Assert.Same(_testConfiguration, capturedConfig);
        Assert.Equal(string.Empty, capturedName);
    }

    [Fact]
    public void OnChange_WithNullListener_ShouldThrowArgumentNullException()
    {
        // Arrange
        GivenATestConfigurationIsSet();
        GivenTheMonitorIsCreated();
        Action<IInternalConfiguration, string> listener = null;

        // Act & Assert
        var e = Assert.Throws<ArgumentNullException>(
            () => _monitor.OnChange(listener));

        // Assert
        Assert.NotNull(e);
        Assert.Equal(nameof(listener), e.ParamName);
    }

    [Fact]
    public void CurrentValue_MultipleInvocations_ShouldCallRepositoryEachTime()
    {
        // Arrange
        GivenATestConfigurationIsSet();
        GivenTheMonitorIsCreated();

        // Act
        _ = _monitor.CurrentValue;
        _ = _monitor.CurrentValue;
        _ = _monitor.CurrentValue;

        // Assert
        _mConfigurationRepo.Verify(x => x.Get(), Times.Exactly(3));
    }

    [Fact]
    public void Get_MultipleInvocationsWithDifferentNames_ShouldCallRepositoryEachTime()
    {
        // Arrange
        GivenATestConfigurationIsSet();
        GivenTheMonitorIsCreated();

        // Act
        _ = _monitor.Get("config1");
        _ = _monitor.Get("config2");
        _ = _monitor.Get("config3");

        // Assert
        _mConfigurationRepo.Verify(x => x.Get(), Times.Exactly(3));
    }

    [Fact]
    public void OnChange_MultipleListenersRegistered_ShouldRegisterAllCallbacks()
    {
        // Arrange
        GivenATestConfigurationIsSet();
        GivenTheMonitorIsCreated();
        var listener1Called = false;
        var listener2Called = false;

        // Act
        _monitor.OnChange((config, name) => listener1Called = true);
        _monitor.OnChange((config, name) => listener2Called = true);

        // Assert
        _mChangeToken.Verify(
            x => x.RegisterChangeCallback(It.IsAny<Action<object>>(), It.IsAny<object>()),
            Times.Exactly(2));
        Assert.False(listener1Called);
        Assert.False(listener2Called);
    }

    // Helper methods
    private void GivenTheRepositoryMock()
    {
        _mConfigurationRepo = new Mock<IInternalConfigurationRepository>();
    }

    private void GivenTheChangeTokenSourceMock()
    {
        _mChangeTokenSource = new Mock<IOcelotConfigurationChangeTokenSource>();
        _mChangeTokenSource.Setup(x => x.ChangeToken).Returns(_mChangeToken?.Object ?? new Mock<IChangeToken>().Object);
    }

    private void GivenTheChangeTokenMock()
    {
        _mChangeToken = new Mock<IChangeToken>();
        _mChangeToken
            .Setup(x => x.RegisterChangeCallback(It.IsAny<Action<object>>(), It.IsAny<object>()))
            .Returns(new Mock<IDisposable>().Object);
    }

    private void GivenATestConfigurationIsSet()
    {
        _testConfiguration = new Mock<IInternalConfiguration>().Object;
        _repoResponse = new OkResponse<IInternalConfiguration>(_testConfiguration);
        _mConfigurationRepo.Setup(x => x.Get()).Returns(_repoResponse);
    }

    private void GivenTheMonitorIsCreated()
    {
        _mChangeTokenSource.Setup(x => x.ChangeToken).Returns(_mChangeToken.Object);
        _monitor = new OcelotConfigurationMonitor(_mConfigurationRepo.Object, _mChangeTokenSource.Object);
    }
}
