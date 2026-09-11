using Ocelot.Configuration.File;
using System.Globalization;

namespace Ocelot.UnitTests.Configuration.FileModels;

public class FileMetadataOptionsTests
{
    [Fact]
    public void Ctor_Default()
    {
        // Arrange, Act
        FileMetadataOptions actual = new();

        // Assert
        Assert.Contains(",", actual.Separators);
        Assert.Contains(" ", actual.TrimChars);
    }

    [Fact]
    public void Ctor_CopyingFrom()
    {
        // Arrange
        FileMetadataOptions from = new()
        {
            CurrentCulture = CultureInfo.GetCultureInfo("uk").Name,
            NumberStyle = NumberStyles.None.ToString(),
            Separators = ["|"],
            StringSplitOption = StringSplitOptions.TrimEntries.ToString(),
            TrimChars = ['x'],
        };

        // Act
        FileMetadataOptions actual = new(from);

        // Assert
        Assert.False(ReferenceEquals(from, actual));
        Assert.Equivalent(from, actual);
        Assert.Equal("uk", actual.CurrentCulture);
        Assert.Equal("None", actual.NumberStyle);
        Assert.Contains("|", actual.Separators);
        Assert.Equal("TrimEntries", actual.StringSplitOption);
        Assert.Contains('x', actual.TrimChars);
    }
}
