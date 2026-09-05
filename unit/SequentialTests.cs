namespace Ocelot.UnitTests;

/// <summary>
/// Apply <see cref="CollectionAttribute"/> to classes to disable parallelization.
/// </summary>
[CollectionDefinition(nameof(SequentialTests), DisableParallelization = true)]
public class SequentialTests
{
    ///// <summary>
    ///// Unstable <see cref="Kubernetes.KubeTests"/>.
    ///// </summary>
    //[Collection(nameof(SequentialTests))]
    //public class KubeTests : Kubernetes.KubeTests
    //{ } // all tests
}
