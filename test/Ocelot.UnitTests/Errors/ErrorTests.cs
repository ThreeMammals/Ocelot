using Ocelot.Infrastructure.RequestData;

namespace Ocelot.UnitTests.Errors;

public class ErrorTests
{
    [Fact]
    public void Should_return_message()
    {
        // Arrange
        var error = new CannotAddDataError("message");

        // Act
        var result = error.ToString();

        // Assert
        result.ShouldBe("CannotAddDataError: message");
    }
}
