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
    [Fact]
    public void HasChanged_When_Not_Registered_Returns_False()
    {
        var token = new OcelotConfigurationChangeToken();
        Assert.False(token.HasChanged);
    }

    [Fact]
    public void HasChanged_After_ActiveChangeTokenSource_Is_Triggered_Returns_True()
    {
        var token = new OcelotConfigurationChangeToken
        {
            //ActiveChangeCallbacks = true // simulate registration
        };

        token.Activate(); // trigger change

        Assert.True(token.HasChanged);
    }

    [Fact]
    public void ActiveChangeCallbacks_When_No_Callbacks_Registered_Returns_False()
    {
        var token = new OcelotConfigurationChangeToken();
        Assert.False(token.ActiveChangeCallbacks);
    }

    [Fact]
    public void ActiveChangeCallbacks_After_RegisterChangeCallback_Returns_True()
    {
        var token = new OcelotConfigurationChangeToken();

        token.RegisterChangeCallback(_ => { }, null);

        Assert.True(token.ActiveChangeCallbacks);
    }

    [Fact]
    public void RegisterChangeCallback_When_Callback_Is_Null_Throws_ArgumentNullException()
    {
        Action<object> callback = null;
        var token = new OcelotConfigurationChangeToken();

        var actual = Assert.Throws<ArgumentNullException>(() =>
            token.RegisterChangeCallback(callback, null));

        Assert.Equal(nameof(callback), actual.ParamName);
    }

    [Fact]
    public void RegisterChangeCallback_When_Valid_Callback_Is_Provided_Invokes_Callback_On_Reload()
    {
        var token = new OcelotConfigurationChangeToken();
        bool callbackInvoked = false;
        object callbackState = null;

        token.RegisterChangeCallback(state =>
        {
            callbackInvoked = true;
            callbackState = state;
        }, "test-state");

        token.Activate();

        Assert.True(callbackInvoked);
        Assert.Equal("test-state", callbackState);
    }

    [Fact]
    public void RegisterChangeCallback_Multiple_Callbacks_All_Invoked_On_Reload()
    {
        var token = new OcelotConfigurationChangeToken();
        int invocationCount = 0;

        token.RegisterChangeCallback(_ => Interlocked.Increment(ref invocationCount), null);
        token.RegisterChangeCallback(_ => Interlocked.Increment(ref invocationCount), null);
        token.RegisterChangeCallback(_ => Interlocked.Increment(ref invocationCount), null);

        token.Activate();

        Assert.Equal(3, invocationCount);
    }

    [Fact]
    public void OnReload_When_No_Callbacks_Does_Not_Throw()
    {
        var token = new OcelotConfigurationChangeToken();

        // Should not throw
        token.Activate();
        Assert.True(token.HasChanged);
    }

    [Fact]
    public void HasChanged_Remains_True_After_Multiple_OnReload_Calls()
    {
        var token = new OcelotConfigurationChangeToken();
        token.RegisterChangeCallback(_ => { }, null);

        token.Activate();
        token.Activate();
        token.Activate();

        Assert.True(token.HasChanged);
    }

    [Fact]
    public void RegisterChangeCallback_Returns_Disposable_That_Can_Be_Disposed_Without_Error()
    {
        var token = new OcelotConfigurationChangeToken();
        var disposable = token.RegisterChangeCallback(_ => { }, null);

        // Should not throw
        disposable.Dispose();
    }

    [Fact]
    public void RegisterChangeCallback_After_Dispose_Of_Previous_Callback_Still_Works_For_New_Ones()
    {
        var token = new OcelotConfigurationChangeToken();
        bool firstCallback = false;
        bool secondCallback = false;

        var disposable = token.RegisterChangeCallback(_ => firstCallback = true, null);
        disposable.Dispose();

        token.RegisterChangeCallback(_ => secondCallback = true, null);

        token.Activate();

        Assert.False(firstCallback);   // disposed callback should not fire
        Assert.True(secondCallback);
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
