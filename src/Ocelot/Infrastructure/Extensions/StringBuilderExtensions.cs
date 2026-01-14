namespace Ocelot.Infrastructure.Extensions;

public static class StringBuilderExtensions
{

    /// <summary>Helper method to add a string to the key builder, using a comma as the default separator.</summary>
    /// <param name="builder">The key builder instance.</param>
    /// <param name="next">The next string to append.</param>
    /// <param name="separator">The character used to separate entries.</param>
    /// <returns>Returns the same builder instance.</returns>
    public static StringBuilder AppendNext(this StringBuilder builder, string next, char separator = ',')
    {
        if (builder.Length > 0)
        {
            builder.Append(separator);
        }

        return builder.Append(next);
    }
}
