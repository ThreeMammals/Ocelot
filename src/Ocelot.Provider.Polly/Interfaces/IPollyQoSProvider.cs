using Ocelot.Configuration;

namespace Ocelot.Provider.Polly.Interfaces;

[Obsolete("Due to new v8 policy definition in Polly 8 (use IPollyQoSResiliencePipelineProvider)")]
public interface IPollyQoSProvider<TResult>
    where TResult : class
{
    [Obsolete("Due to new v8 policy definition in Polly 8 (use GetResiliencePipeline)")]
    PollyPolicyWrapper<TResult> GetPollyPolicyWrapper(DownstreamRoute route);
}
