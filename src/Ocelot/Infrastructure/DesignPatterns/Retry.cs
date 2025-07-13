using Ocelot.Logging;

namespace Ocelot.Infrastructure.DesignPatterns;

/// <summary>
/// Basic <seealso href="https://www.bing.com/search?q=Retry+pattern">Retry pattern</seealso> for stabilizing integrated services.
/// </summary>
/// <remarks>Docs:
/// <list type="bullet">
/// <item><see href="https://learn.microsoft.com/en-us/azure/architecture/patterns/retry">Microsoft Learn | Retry pattern</see></item>
/// </list>
/// </remarks>
public static class Retry
{
    public const int DefaultRetryTimes = 3;
    public const int DefaultWaitTimeMilliseconds = 25;

    private static string GetMessage<T>(T operation, int retryNo, string message)
        where T : Delegate
        => $"Ocelot {nameof(Retry)} strategy for the operation of '{operation.GetType()}' type -> {nameof(Retry)} No {retryNo}: {message}";

    /// <summary>
    /// Retry a synchronous operation when an exception occurs or predicate is true, then delay and retry again.
    /// </summary>
    /// <typeparam name="TResult">Type of the result of the sync operation.</typeparam>
    /// <param name="operation">Required Func-delegate of the operation.</param>
    /// <param name="predicate">Predicate to check, optionally.</param>
    /// <param name="retryTimes">Number of retries.</param>
    /// <param name="waitTime">Waiting time in milliseconds.</param>
    /// <param name="logger">Concrete logger from upper context.</param>
    /// <returns>A <typeparamref name="TResult"/> value as the result of the sync operation.</returns>
    public static TResult Operation<TResult>(
        Func<TResult> operation,
        Predicate<TResult> predicate = null,
        int retryTimes = DefaultRetryTimes, int waitTime = DefaultWaitTimeMilliseconds,
        IOcelotLogger logger = null)
    {
        if (waitTime < 0)
        {
            waitTime = 0; // 0 means no thread sleeping
        }

        for (int n = 1; n < retryTimes; n++)
        {
            TResult result;
            try
            {
                result = operation.Invoke();
            }
            catch (Exception e)
            {
                logger?.LogError(() => GetMessage(operation, n, $"Caught exception of the {e.GetType()} type -> Message: {e.Message}."), e);
                Thread.Sleep(waitTime);
                continue; // the result is unknown, so continue to retry
            }

            // Apply predicate for known result
            if (predicate?.Invoke(result) == true)
            {
                logger?.LogWarning(() => GetMessage(operation, n, $"The predicate has identified erroneous state in the returned result. For further details, implement logging of the result's value or properties within the predicate method."));
                Thread.Sleep(waitTime);
                continue; // on erroneous state
            }

            // Happy path
            return result;
        }

        // Last retry should generate native exception or other erroneous state(s)
        logger?.LogDebug(() => GetMessage(operation, retryTimes, $"Retrying lastly..."));
        return operation.Invoke(); // also final result must be analyzed in the upper context
    }

    /// <summary>
    /// Retry an asynchronous operation when an exception occurs or predicate is true, then delay and retry again.
    /// </summary>
    /// <typeparam name="TResult">Type of the result of the async operation.</typeparam>
    /// <param name="operation">Required Func-delegate of the operation.</param>
    /// <param name="predicate">Predicate to check, optionally.</param>
    /// <param name="retryTimes">Number of retries.</param>
    /// <param name="waitTime">Waiting time in milliseconds.</param>
    /// <param name="logger">Concrete logger from upper context.</param>
    /// <returns>A <typeparamref name="TResult"/> value as the result of the async operation.</returns>
    public static async Task<TResult> OperationAsync<TResult>(
        Func<Task<TResult>> operation, // required operation delegate
        Predicate<TResult> predicate = null, // optional retry predicate for the result
        int retryTimes = DefaultRetryTimes, int waitTime = DefaultWaitTimeMilliseconds, // retrying options
        IOcelotLogger logger = null) // static injections
    {
        for (int n = 1; n < retryTimes; n++)
        {
            TResult result;
            try
            {
                result = await operation?.Invoke();
            }
            catch (Exception e)
            {
                logger?.LogError(() => GetMessage(operation, n, $"Caught exception of the {e.GetType()} type -> Message: {e.Message}."), e);
                await (waitTime > 0 ? Task.Delay(waitTime) : Task.CompletedTask);
                continue; // the result is unknown, so continue to retry
            }

            // Apply predicate for known result
            if (predicate?.Invoke(result) == true)
            {
                logger?.LogWarning(() => GetMessage(operation, n, $"The predicate has identified erroneous state in the returned result. For further details, implement logging of the result's value or properties within the predicate method."));
                await (waitTime > 0 ? Task.Delay(waitTime) : Task.CompletedTask);
                continue; // on erroneous state
            }

            // Happy path
            return result;
        }

        // Last retry should generate native exception or other erroneous state(s)
        logger?.LogDebug(() => GetMessage(operation, retryTimes, $"Retrying lastly..."));
        return await operation?.Invoke(); // also final result must be analyzed in the upper context
    }
}
