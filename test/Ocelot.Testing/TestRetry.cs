using Ocelot.Infrastructure.DesignPatterns;
using Ocelot.Logging;

namespace Ocelot.Testing;

public static class TestRetry
{
    public static TResult NoWait<TResult>(
        Func<TResult> operation,
        Predicate<TResult> predicate = null,
        int retryTimes = Retry.DefaultRetryTimes,
        IOcelotLogger logger = null)
        => Retry.Operation(operation, predicate, retryTimes, 0, logger);

    public static Task<TResult> NoWaitAsync<TResult>(
        Func<Task<TResult>> operation,
        Predicate<TResult> predicate = null,
        int retryTimes = Retry.DefaultRetryTimes,
        IOcelotLogger logger = null)
        => Retry.OperationAsync(operation, predicate, retryTimes, 0, logger);
}
