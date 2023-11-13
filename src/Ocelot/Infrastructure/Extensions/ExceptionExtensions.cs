namespace Ocelot.Infrastructure.Extensions;

public static class ExceptionExtensions
{
    /// <summary>
    /// Gets all exception messages via traversing all inner exceptions placing each message onto new line.
    /// </summary>
    /// <param name="ex">Current exception.</param>
    /// <param name="builder">A string builder to accumulate all messages.</param>
    /// <returns>A <see cref="StringBuilder"/> object with all messages inside.</returns>
    public static StringBuilder AllMessages(this Exception ex, StringBuilder builder = null)
    {
        builder ??= new StringBuilder();
        return ex.InnerException != null
                ? AllMessages(ex.InnerException, builder)
                : builder.AppendLine(ex.Message);
    }

    /// <summary>
    /// Gets all exception messages of this and inner exceptions as one string.
    /// </summary>
    /// <param name="ex">Current exception.</param>
    /// <returns>A <see langword="string" /> with all messages inside.</returns>
    public static string GetMessages(this Exception ex)
        => AllMessages(ex).ToString();
}
