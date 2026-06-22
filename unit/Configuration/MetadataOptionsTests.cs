using Ocelot.Configuration;
using Ocelot.Configuration.File;
using System.Globalization;

namespace Ocelot.UnitTests.Configuration;

public class MetadataOptionsTests
{
    [Fact]
    public void Ctor_Parameterless()
    {
        // Arrange, Act
        MetadataOptions actual = new();

        // Assert
        Assert.Equal(CultureInfo.CurrentCulture, actual.CurrentCulture);
        Assert.Equal(NumberStyles.Any, actual.NumberStyle);
        Assert.Contains(",", actual.Separators);
        Assert.Equal(StringSplitOptions.None, actual.StringSplitOption);
        Assert.Contains(' ', actual.TrimChars);
        Assert.NotNull(actual.Metadata);
    }

    [Fact]
    [Trait("PR", "2324")]
    public void Ctor_CopyingFrom_MetadataOptions()
    {
        // Arrange
        MetadataOptions from = new(
            separators: ["x"],
            trimChars: ['y'],
            stringSplitOption: StringSplitOptions.TrimEntries,
            numberStyle: NumberStyles.Number,
            currentCulture: CultureInfo.GetCultureInfo("uk"),
            metadata: new Dictionary<string, string>()
            {
                { "key", "value" },
            });

        // Act
        MetadataOptions actual = new(from);

        // Assert
        Assert.False(ReferenceEquals(from, actual));
        Assert.Equivalent(from, actual);
    }

    [Fact]
    [Trait("PR", "2324")]
    public void Ctor_CopyingFrom_FileMetadataOptions()
    {
        // Arrange
        FileMetadataOptions from = new()
        {
            CurrentCulture = "uk",
            NumberStyle = nameof(NumberStyles.Number),
            Separators = ["x"],
            StringSplitOption = nameof(StringSplitOptions.RemoveEmptyEntries),
            TrimChars = [';'],
        };

        // Act
        MetadataOptions actual = new(from);

        // Assert
        Assert.False(ReferenceEquals(from, actual));
        Assert.Equal(CultureInfo.GetCultureInfo("uk"), actual.CurrentCulture);
        Assert.Equal(NumberStyles.Number, actual.NumberStyle);
        Assert.Contains("x", actual.Separators);
        Assert.Equal(StringSplitOptions.RemoveEmptyEntries, actual.StringSplitOption);
        Assert.Contains(';', actual.TrimChars);
        Assert.NotNull(actual.Metadata);
    }
}
