using Polly.Registry;

namespace Ocelot.Provider.Polly;

/// <summary>
/// Object used to identify a resilience pipeline in <see cref="ResiliencePipelineRegistry{OcelotResiliencePipelineKey}"/>.
/// </summary>
/// <value>An <see cref="OcelotResiliencePipelineKey"/> record object.</value>
/// <param name="Key">The key for the resilience pipeline.</param>
public record OcelotResiliencePipelineKey(string Key);
