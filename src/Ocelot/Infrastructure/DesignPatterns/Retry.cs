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
        for (int i = 0; i < retryTimes - 1; i++)
        {
            TResult result = default;
            try
            {
                result = operation.Invoke();
            }
            catch (Exception e)
            {
                logger?.LogWarning(() => $"Ocelot {nameof(Retry)} strategy -> {nameof(Retry)} {i + 1}: Getting exception -> '{e.Message}'");
                Thread.Sleep(waitTime);
                continue; // the result is unknown, so continue to retry
            }

            // Apply predicate for known result
            if (predicate?.Invoke(result) == true)
            {
                logger?.LogDebug(() => $"Ocelot {nameof(Retry)} strategy -> {nameof(Retry)} {i + 1}: Operation predicate failed.");
                Thread.Sleep(waitTime);
                continue; // on erroneous state
            }

            // Happy path
            return result;
        }

        // Last retry should generate native exception or other erroneous state(s)
        logger?.LogDebug(() => $"Ocelot {nameof(Retry)} strategy -> {nameof(Retry)} {retryTimes}: Retrying lastly...");
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
        for (int i = 0; i < retryTimes - 1; i++)
        {
            TResult result = default;
            try
            {
                result = await operation?.Invoke();
            }
            catch (Exception e)
            {
                logger?.LogWarning(() => $"Ocelot {nameof(Retry)} strategy -> {nameof(Retry)} {i + 1}: Getting exception -> '{e.Message}'");
                await Task.Delay(waitTime);
                continue; // the result is unknown, so continue to retry
            }

            // Apply predicate for known result
            if (predicate?.Invoke(result) == true)
            {
                logger?.LogDebug(() => $"Ocelot {nameof(Retry)} strategy -> {nameof(Retry)} {i + 1}: Operation predicate failed.");
                await Task.Delay(waitTime);
                continue; // on erroneous state
            }

            // Happy path
            return result;
        }

        // Last retry should generate native exception or other erroneous state(s)
        logger?.LogDebug(() => $"Ocelot {nameof(Retry)} strategy -> {nameof(Retry)} {retryTimes}: Retrying lastly...");
        return await operation?.Invoke(); // also final result must be analyzed in the upper context
    }
}
