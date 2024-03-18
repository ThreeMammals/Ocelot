namespace Ocelot.Provider.Polly.v7;

public class PollyPolicyWrapper<TResult>
    where TResult : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PollyPolicyWrapper{TResult}"/> class.
    /// We expect at least one policy to be passed in, default can't be null.
    /// </summary>
    /// <param name="policies">The policies with at least a <see cref="Policy.Timeout(int)"/> policy.</param>
    public PollyPolicyWrapper(params IAsyncPolicy<TResult>[] policies)
    {
        var allPolicies = policies.Where(p => p != null).ToArray();

        AsyncPollyPolicy = allPolicies.Length > 1 ?
            Policy.WrapAsync(allPolicies) :
            allPolicies[0];
    }

    public IAsyncPolicy<TResult> AsyncPollyPolicy { get; }
}
