using Ocelot.Infrastructure.Extensions;
using System.Text;

namespace Ocelot.UnitTests.Infrastructure.Extensions;

public class StringBuilderExtensionsTests
{
    [Fact]
    public void AppendNext_ShouldAppendWithoutSeparator_WhenBuilderIsEmpty()
    {
        // Arrange
        var sb = new StringBuilder();

        // Act
        sb.AppendNext("first");

        // Assert
        Assert.Equal("first", sb.ToString());
    }

    [Fact]
    public void AppendNext_ShouldAppendWithSeparator_WhenBuilderHasContent()
    {
        // Arrange
        var sb = new StringBuilder("first");

        // Act
        sb.AppendNext("second");

        // Assert
        Assert.Equal("first,second", sb.ToString());
    }

    [Fact]
    public void AppendNext_ShouldUseCustomSeparator()
    {
        // Arrange
        var sb = new StringBuilder("first");

        // Act
        sb.AppendNext("second", ';');

        // Assert
        Assert.Equal("first;second", sb.ToString());
    }

    [Fact]
    public void AppendNext_ShouldAllowMultipleAppends()
    {
        // Arrange
        var sb = new StringBuilder();

        // Act
        sb.AppendNext("one");
        sb.AppendNext("two");
        sb.AppendNext("three");

        // Assert
        Assert.Equal("one,two,three", sb.ToString());
    }

    [Fact]
    public void AppendNext_ShouldHandleEmptyStringAsNext()
    {
        // Arrange
        var sb = new StringBuilder("first");

        // Act
        sb.AppendNext("");

        // Assert
        Assert.Equal("first,", sb.ToString());
    }

    [Fact]
    public void AppendNext_ShouldReturnSameBuilderInstance()
    {
        // Arrange
        var sb = new StringBuilder();

        // Act
        var result = sb.AppendNext("test");

        // Assert
        Assert.Same(sb, result);
    }
}
