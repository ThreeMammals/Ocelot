using Microsoft.AspNetCore.Http;
using Ocelot.Errors;
using Ocelot.Requester;

namespace Ocelot.UnitTests.Requester;

public class BadRequestErrorTests
{
    [Fact]
    public void Should_create_bad_request_error()
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
