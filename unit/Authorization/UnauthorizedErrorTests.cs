using Ocelot.Authorization;
using Ocelot.Errors;

namespace Ocelot.UnitTests.Authorization;

public class UnauthorizedErrorTests : UnitTest
{
    [Fact]
    public void Ctor()
    {
        UnauthorizedError error = new(TestID);

        Assert.Equal(TestID, error.Message);
        Assert.Equal(OcelotErrorCode.UnauthorizedError, error.Code);
        Assert.Equal(403, error.HttpStatusCode);
    }
}
