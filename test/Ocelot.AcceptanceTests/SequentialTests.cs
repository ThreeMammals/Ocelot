using Xunit;

namespace Ocelot.AcceptanceTests;

/// <summary>
/// Apply <see cref="CollectionAttribute"/> to classes to disable parallelization.
/// </summary>
[CollectionDefinition(nameof(SequentialTests), DisableParallelization = true)]
public class SequentialTests
{
}
