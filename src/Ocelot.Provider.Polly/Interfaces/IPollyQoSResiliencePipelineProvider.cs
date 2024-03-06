using Ocelot.Configuration;

namespace Ocelot.Provider.Polly.Interfaces;

public interface IPollyQoSResiliencePipelineProvider<TResult>
    where TResult : class
{
    /// <summary>
    /// Gets Polly v8 pipeline.
    /// </summary>
    /// <param name="route">The route to apply a pipeline for.</param>
    /// <returns>A <see cref="ResiliencePipeline{TResult}"/> object, by Polly v8.</returns>
    ResiliencePipeline<TResult> GetResiliencePipeline(DownstreamRoute route);
}
