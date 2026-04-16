using Microsoft.AspNetCore.Http;
using Ocelot.Errors;
using Ocelot.Requester;

namespace Ocelot.UnitTests.Requester;

[Trait("Bug", "2376")] // https://github.com/ThreeMammals/Ocelot/issues/2376
[Trait("PR", "2379")] // https://github.com/ThreeMammals/Ocelot/pull/2379
public class BadRequestErrorTests
{
    [Fact]
    public void Ctor_String()
    {
        // Arrange
        var message = "This is a bad request message.";

        // Act
        var error = new BadRequestError(message);

        // Assert
        Assert.Equal(message, error.Message);
        Assert.Equal(OcelotErrorCode.BadRequestError, error.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, error.HttpStatusCode);
    }

    [Fact]
    public void Ctor_Exception()
    {
        // Arrange
        var ex = new Exception("This is a bad request message.");

        // Act
        var error = new BadRequestError(ex);

        // Assert
        Assert.NotNull(error.Exception);
        Assert.Same(ex, error.Exception);
        Assert.Equal(ex.Message, error.Message);
        Assert.Equal(OcelotErrorCode.BadRequestError, error.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, error.HttpStatusCode);
    }
}
