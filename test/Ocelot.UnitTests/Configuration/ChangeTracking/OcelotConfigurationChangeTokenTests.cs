using Ocelot.Configuration.ChangeTracking;

namespace Ocelot.UnitTests.Configuration.ChangeTracking;

public class OcelotConfigurationChangeTokenTests : UnitTest
{
    [Fact]
    public void Should_call_callback_with_state()
    {
        // Arrange
        GivenIHaveAChangeToken();
        AndIRegisterACallback();
        ThenIShouldGetADisposableWrapper();

        // Act
        GivenIActivateTheToken();

        // Assert
        ThenTheCallbackShouldBeCalled();
    }

    [Fact]
    public void Should_not_call_callback_if_it_is_disposed()
    {
        // Arrange
        GivenIHaveAChangeToken();
        AndIRegisterACallback();
        ThenIShouldGetADisposableWrapper();

        // Act, Assert
        GivenIActivateTheToken();
        AndIDisposeTheCallbackWrapper();

        // Act, Assert
        GivenIActivateTheToken();
        ThenTheCallbackShouldNotBeCalled();
    }

    private OcelotConfigurationChangeToken _changeToken;
    private IDisposable _callbackWrapper;
    private int _callbackCounter;
    private readonly object _callbackInitialState = new();
    private object _callbackState;

    private void Callback(object state)
    {
        _callbackCounter++;
        _callbackState = state;
        _changeToken.HasChanged.ShouldBeTrue();
    }

    private void GivenIHaveAChangeToken()
    {
        _changeToken = new OcelotConfigurationChangeToken();
    }

    private void AndIRegisterACallback()
    {
        _callbackWrapper = _changeToken.RegisterChangeCallback(Callback, _callbackInitialState);
    }

    private void ThenIShouldGetADisposableWrapper()
    {
        _callbackWrapper.ShouldNotBeNull();
    }

    private void GivenIActivateTheToken()
    {
        _callbackCounter = 0;
        _callbackState = null;
        _changeToken.Activate();
    }

    private void ThenTheCallbackShouldBeCalled()
    {
        _callbackCounter.ShouldBe(1);
        _callbackState.ShouldNotBeNull();
        _callbackState.ShouldBeSameAs(_callbackInitialState);
    }

    private void ThenTheCallbackShouldNotBeCalled()
    {
        _callbackCounter.ShouldBe(0);
        _callbackState.ShouldBeNull();
    }

    private void AndIDisposeTheCallbackWrapper()
    {
        _callbackState = null;
        _callbackCounter = 0;
        _callbackWrapper.Dispose();
    }
}
