using Ocelot.Testing.Steps;

namespace Ocelot.Acceptance.ServiceDiscovery;

public class DynamicSteps : DiscoverySteps
{
    public override string ServiceNamespace() => GetType().Name; // base version returns long namespace, that's bad
    public override CancellationToken CancelMe => TestContext.Current.CancellationToken;
}
