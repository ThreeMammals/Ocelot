using Ocelot.Configuration;

namespace Ocelot.Provider.Polly.v7;

[Obsolete("It is obsolete because now, we use IPollyQoSResiliencePipelineProvider with new v8 resilience strategies")]
public interface IPollyQoSProvider<TResult>
    where TResult : class
{
    PollyPolicyWrapper<TResult> GetPollyPolicyWrapper(DownstreamRoute route);
}
