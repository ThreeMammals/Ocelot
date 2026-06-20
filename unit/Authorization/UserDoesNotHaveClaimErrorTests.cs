using Ocelot.Authorization;
using Ocelot.Errors;

namespace Ocelot.UnitTests.Authorization;

public class UserDoesNotHaveClaimErrorTests : UnitTest
{
    [Fact]
    public void Ctor()
    {
        UserDoesNotHaveClaimError error = new(TestID);

        Assert.Equal(TestID, error.Message);
        Assert.Equal(OcelotErrorCode.UserDoesNotHaveClaimError, error.Code);
        Assert.Equal(403, error.HttpStatusCode);
    }
}
