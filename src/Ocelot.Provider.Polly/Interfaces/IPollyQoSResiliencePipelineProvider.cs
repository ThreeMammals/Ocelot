using Ocelot.Configuration;

namespace Ocelot.Provider.Polly.Interfaces;

public interface IPollyQoSResiliencePipelineProvider<TResult> where TResult : class
{
    ResiliencePipeline<TResult> GetResiliencePipeline(DownstreamRoute route);
}
