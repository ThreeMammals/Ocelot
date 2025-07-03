namespace Ocelot.Infrastructure.Extensions;

public static class IntExtensions
{
    public static int Ensure(this int value, int low = 0) => value < low ? low : value;
    public static int Positive(this int value) => Ensure(value, 1);

    /// <summary>
    /// Ensures nullable integer is positive, otherwise converts the value to default one.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="toDefault">Default integer to convert to.</param>
    /// <returns>A nullable <see cref="int"/> value.</returns>
    public static int? Positive(this int? value, int toDefault = 1)
        => value.HasValue
            ? (value.Value > 0 ? value.Value : toDefault)
            : null;
}
