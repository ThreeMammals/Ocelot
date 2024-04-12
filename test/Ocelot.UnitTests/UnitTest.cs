using TestStack.BDDfy.Configuration;

namespace Ocelot.UnitTests;

public class UnitTest
{
    public UnitTest()
    {
        Configurator.Processors.ConsoleReport.Disable();
    }

    protected readonly Guid _testId = Guid.NewGuid();

    protected string TestID { get => _testId.ToString("N"); }
}
