using Ocelot.Infrastructure.DesignPatterns;
using Ocelot.Logging;
using System.Diagnostics;
using System.Reflection;

namespace Ocelot.UnitTests.Infrastructure.DesignPatterns;

[Trait("PR", "2111")] // https://github.com/ThreeMammals/Ocelot/pull/2111
[Trait("Commit", "19a8e2f")] // https://github.com/ThreeMammals/Ocelot/commit/19a8e2f8b3e773fbe962a56fc2e7067b6b19132b
[Trait("Release", "23.3.4")] // https://github.com/ThreeMammals/Ocelot/releases/tag/23.3.4
public sealed class RetryTests : UnitTest
{
    private const int OprResult = 33;
    private static int Opr() => OprResult;

    private const int OprAsyncResult = 44;
    private static Task<int> OprAsync() => Task.FromResult(OprAsyncResult);

    private int _call = 0;
    private int Count() => ++_call;
    private Task<int> CountAsync() => Task.FromResult(++_call);

    private readonly Mock<IOcelotLogger> _logger = new();
    private readonly List<string> _logDebugMessages = new();
    private readonly List<string> _logWarningMessages = new();
    private readonly List<string> _logErrorMessages = new();
    private readonly List<Exception> _logExceptions = new();
    private void AddDebug(Func<string> message) => _logDebugMessages.Add(message());
    private void AddWarning(Func<string> message) => _logWarningMessages.Add(message());
    private void AddError(Func<string> message, Exception exception)
    {
        _logErrorMessages.Add(message());
        _logExceptions.Add(exception);
    }

    public RetryTests()
    {
        _logger.Setup(x => x.LogDebug(It.IsAny<Func<string>>()))
            .Callback<Func<string>>(AddDebug);
        _logger.Setup(x => x.LogWarning(It.IsAny<Func<string>>()))
            .Callback<Func<string>>(AddWarning);
        _logger.Setup(x => x.LogError(It.IsAny<Func<string>>(), It.IsAny<Exception>()))
            .Callback<Func<string>, Exception>(AddError);
    }


    [Fact]
    public void GetMessage()
    {
        // Arrange
        Func<int> operation = Count;
        int retryNo = 2;
        var message = "Hi";
        var mi = typeof(Retry).GetMethod(nameof(GetMessage), BindingFlags.NonPublic | BindingFlags.Static);
        var method = mi.MakeGenericMethod(typeof(Func<int>));

        // Act
        string actual = method.Invoke(null, [operation, retryNo, message]) as string;

        // Assert
        Assert.Equal("Ocelot Retry strategy for the operation of 'System.Func`1[System.Int32]' type -> Retry No 2: Hi", actual);
    }

    [Fact]
    public void Operation_WhenOperationIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        Func<int> operation = null;

        // Act, Assert
        var exception = Assert.Throws<ArgumentNullException>(() => Retry.Operation<int>(operation));
        Assert.Equal(nameof(operation), exception.ParamName);
    }

    [Fact]
    public async Task OperationAsync_WhenOperationIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        Func<Task<int>> operation = null;

        // Act, Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => Retry.OperationAsync<int>(operation));
        Assert.Equal(nameof(operation), exception.ParamName);
    }

    [Fact]
    public void Operation_WhenOperationSucceeds_HappyPath()
    {
        // Arrange, Act
        var actual = Retry.Operation(Opr);

        // Assert
        Assert.Equal(OprResult, actual);
    }

    [Fact]
    public async Task OperationAsync_WhenOperationSucceeds_HappyPath()
    {
        // Arrange, Act
        var actual = await Retry.OperationAsync(OprAsync);

        // Assert
        Assert.Equal(OprAsyncResult, actual);
    }

    [Fact]
    public void Operation_WaitTimeIsLessThanZero_WaitTimeArgHasBeenChecked()
    {
        // Arrange
        var watcher = Stopwatch.StartNew();

        // Act, Assert
        Assert.Throws<InvalidOperationException>(() => Retry.Operation(InvalidOperation, waitTime: -1));
        watcher.Stop();
        Assert.InRange(watcher.ElapsedMilliseconds, 0, Retry.DefaultWaitTimeMilliseconds);
        int closeToZero = IsCiCd() ? Retry.DefaultWaitTimeMilliseconds : 10;
        Assert.True(watcher.ElapsedMilliseconds < closeToZero);
    }

    [Fact]
    public async Task OperationAsync_WaitTimeIsLessThanZero_WaitTimeArgHasBeenChecked()
    {
        // Arrange
        var watcher = Stopwatch.StartNew();

        // Act, Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => Retry.OperationAsync(InvalidOperationAsync, waitTime: -1));
        watcher.Stop();
        Assert.InRange(watcher.ElapsedMilliseconds, 0, Retry.DefaultWaitTimeMilliseconds);
        Assert.True(watcher.ElapsedMilliseconds < 10); // near to zero
    }

    private static int InvalidOperation() => throw new InvalidOperationException("fail");
    private static Task<int> InvalidOperationAsync() => Task.FromException<int>(new InvalidOperationException("fail"));

    [Fact]
    public void Operation_WhenAlwaysThrows_ThenThrowsAfterRetries()
    {
        // Arrange, Act, Assert : Case 1. Without logger
        Assert.Throws<InvalidOperationException>(() => Retry.Operation(InvalidOperation));

        // Case 2. With logger
        Assert.Throws<InvalidOperationException>(() => Retry.Operation(InvalidOperation, logger: _logger.Object));
        _logger.Verify(
            x => x.LogError(It.IsAny<Func<string>>(), It.IsAny<Exception>()),
            Times.Exactly(2));
        Assert.Equal(2, _logErrorMessages.Count);
        Assert.Equal("Ocelot Retry strategy for the operation of 'System.Func`1[System.Int32]' type -> Retry No 1: Caught exception of the System.InvalidOperationException type -> Message: fail.",
            _logErrorMessages[0]);
        Assert.Equal(2, _logExceptions.Count);
        Assert.IsType<InvalidOperationException>(_logExceptions[0]);
        Assert.Equal("fail", _logExceptions[0].Message);
        _logger.Verify(
            x => x.LogDebug(It.IsAny<Func<string>>()),
            Times.Once);
        var dbgMessage = Assert.Single(_logDebugMessages);
        Assert.Equal("Ocelot Retry strategy for the operation of 'System.Func`1[System.Int32]' type -> Retry No 3: Retrying lastly...",
            dbgMessage);
    }

    [Fact]
    public async Task OperationAsync_WhenAlwaysThrows_ThenThrowsAfterRetries()
    {
        // Arrange, Act, Assert : Case 1. Without logger
        await Assert.ThrowsAsync<InvalidOperationException>(() => Retry.OperationAsync(InvalidOperationAsync));

        // Case 2. With logger
        await Assert.ThrowsAsync<InvalidOperationException>(() => Retry.OperationAsync(InvalidOperationAsync, logger: _logger.Object));
        _logger.Verify(
            x => x.LogError(It.IsAny<Func<string>>(), It.IsAny<Exception>()),
            Times.Exactly(2));
        Assert.Equal(2, _logErrorMessages.Count);
        Assert.Equal("Ocelot Retry strategy for the operation of 'System.Func`1[System.Threading.Tasks.Task`1[System.Int32]]' type -> Retry No 1: Caught exception of the System.InvalidOperationException type -> Message: fail.",
            _logErrorMessages[0]);
        Assert.Equal(2, _logExceptions.Count);
        Assert.IsType<InvalidOperationException>(_logExceptions[0]);
        Assert.Equal("fail", _logExceptions[0].Message);
        _logger.Verify(
            x => x.LogDebug(It.IsAny<Func<string>>()),
            Times.Once);
        var dbgMessage = Assert.Single(_logDebugMessages);
        Assert.Equal("Ocelot Retry strategy for the operation of 'System.Func`1[System.Threading.Tasks.Task`1[System.Int32]]' type -> Retry No 3: Retrying lastly...",
            dbgMessage);
    }

    [Fact]
    public void Operation_WhenPredicateTrue_ThenRetriesUntilPredicateFalse()
    {
        #region Case 1. Without logger
        // Arrange
        static bool predicate(int r) => r < 3;

        // Act
        var actual = Retry.Operation(Count, predicate, retryTimes: 4, waitTime: 0);

        // Assert
        Assert.Equal(3, actual);
        #endregion

        #region Case 2. With logger
        // Arrange
        _call = 0;

        // Act
        actual = Retry.Operation(Count, predicate, retryTimes: 4, waitTime: 0, logger: _logger.Object);

        // Assert
        Assert.Equal(3, actual);
        _logger.Verify(
            x => x.LogWarning(It.IsAny<Func<string>>()),
            Times.Exactly(2));
        Assert.Equal(2, _logWarningMessages.Count);
        Assert.Equal("Ocelot Retry strategy for the operation of 'System.Func`1[System.Int32]' type -> Retry No 1: The predicate has identified erroneous state in the returned result. For further details, implement logging of the result's value or properties within the predicate method.",
            _logWarningMessages[0]);
        _logger.Verify(
            x => x.LogDebug(It.IsAny<Func<string>>()),
            Times.Never);
        #endregion
    }

    [Fact]
    public async Task OperationAsync_WhenPredicateTrue_ThenRetriesUntilPredicateFalse()
    {
        #region Case 1. Without logger
        // Arrange
        static bool predicate(int r) => r < 3;

        // Act
        var actual = await Retry.OperationAsync(CountAsync, predicate, retryTimes: 4, waitTime: 0);

        // Assert
        Assert.Equal(3, actual);
        #endregion

        #region Case 2. With logger
        // Arrange
        _call = 0;

        // Act
        actual = await Retry.OperationAsync(CountAsync, predicate, retryTimes: 4, waitTime: 0, logger: _logger.Object);

        // Assert
        Assert.Equal(3, actual);
        _logger.Verify(
            x => x.LogWarning(It.IsAny<Func<string>>()),
            Times.Exactly(2));
        Assert.Equal(2, _logWarningMessages.Count);
        Assert.Equal("Ocelot Retry strategy for the operation of 'System.Func`1[System.Threading.Tasks.Task`1[System.Int32]]' type -> Retry No 1: The predicate has identified erroneous state in the returned result. For further details, implement logging of the result's value or properties within the predicate method.",
            _logWarningMessages[0]);
        _logger.Verify(
            x => x.LogDebug(It.IsAny<Func<string>>()),
            Times.Never);
        #endregion
    }
}
