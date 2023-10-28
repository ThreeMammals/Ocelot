namespace Ocelot.Provider.Polly;

public class CircuitBreaker<TResult>
    where TResult : class
{
    public CircuitBreaker(params IAsyncPolicy<TResult>[] policies)
    {
        var allPolicies = policies.Where(p => p != null).ToArray();
        CircuitBreakerAsyncPolicy = allPolicies.FirstOrDefault();

        if (allPolicies.Length > 1)
        {
            CircuitBreakerAsyncPolicy = Policy.WrapAsync(allPolicies);
        }
    }
    public IAsyncPolicy<TResult> CircuitBreakerAsyncPolicy { get; }
}
