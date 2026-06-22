using Ocelot.Configuration.Parser;
using Ocelot.Errors;

namespace Ocelot.UnitTests.Configuration.Parser;

public class ParsingConfigurationHeaderErrorTests : UnitTest
{
    [Fact]
    public void Ctor()
    {
        Exception exception = new(TestID);
        ParsingConfigurationHeaderError error = new(exception);

        Assert.Equal($"Parsing configuration exception is {TestID}", error.Message);
        Assert.Equal(OcelotErrorCode.ParsingConfigurationHeaderError, error.Code);
        Assert.Equal(404, error.HttpStatusCode);
    }
}
