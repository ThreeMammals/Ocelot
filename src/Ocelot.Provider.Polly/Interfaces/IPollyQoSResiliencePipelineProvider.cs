using Ocelot.Configuration;

namespace Ocelot.Provider.Polly.Interfaces;

/// <summary>Defines provider for Polly V8 pipelines.</summary>
/// <typeparam name="TResult">An HTTP result type, usually it is <see cref="HttpResponseMessage"/> type.</typeparam>
public interface IPollyQoSResiliencePipelineProvider<TResult>
    where TResult : IDisposable
{
    /// <summary>
    /// Gets Polly v8 pipeline.
    /// </summary>
    /// <param name="route">The route to apply a pipeline for.</param>
    /// <returns>A <see cref="ResiliencePipeline{T}"/> object where T is <typeparamref name="TResult"/>.</returns>
    ResiliencePipeline<TResult> GetResiliencePipeline(DownstreamRoute route);
}
