using Microsoft.AspNetCore.Http;
using Ocelot.Errors;
using Ocelot.Requester;

namespace Ocelot.UnitTests.Requester;

public class BadRequestErrorTests
{
    [Fact]
    [Trait("Bug", "2376")] // https://github.com/ThreeMammals/Ocelot/issues/2376
    [Trait("PR", "2379")] // https://github.com/ThreeMammals/Ocelot/pull/2379
    public void Ctor_String()
    {
        // Arrange
        var message = "This is a bad request message.";

        // Act
        var error = new BadRequestError(message);

        // Assert
        error.Message.ShouldBe(message);
        error.Code.ShouldBe(OcelotErrorCode.BadRequestError);
        error.HttpStatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }
}
