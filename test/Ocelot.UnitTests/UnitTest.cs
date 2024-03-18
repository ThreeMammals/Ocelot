namespace Ocelot.UnitTests;

public class UnitTest
{
    protected readonly Guid _testId = Guid.NewGuid();

    protected string TestID { get => _testId.ToString("N"); }
}
