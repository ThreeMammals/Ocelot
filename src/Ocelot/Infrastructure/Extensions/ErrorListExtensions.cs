using Ocelot.Errors;

namespace Ocelot.Infrastructure.Extensions;

public static class ErrorListExtensions
{
    private static readonly string Nl = Environment.NewLine;
    private static readonly string Em = string.Empty;

    /// <summary>
    /// Joins all errors using <see cref="Environment.NewLine"/> separator, in the format "Code: Message".
    /// </summary>
    /// <param name="errors">The list of errors to extend.</param>
    /// <param name="before">Flag to insert new line before.</param>
    /// <param name="after">Flag to insert new line after.</param>
    /// <returns>Single <see cref="string"/> with all errors.</returns>
    public static string ToErrorString(this List<Error> errors, bool before = false, bool after = false)
        => (before ? Nl : Em) + string.Join(Nl, errors.Select(e => e.ToString())) + (after ? Nl : Em);
}
