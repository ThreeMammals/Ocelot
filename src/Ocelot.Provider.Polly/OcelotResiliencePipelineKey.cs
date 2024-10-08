using Polly.Registry;

namespace Ocelot.Provider.Polly;

/// <summary>
/// Object used to identify a resilience pipeline in <see cref="ResiliencePipelineRegistry{OcelotResiliencePipelineKey}"/>.
/// </summary>
/// <param name="Key">The key for the resilience pipeline.</param>
public record OcelotResiliencePipelineKey(string Key);
