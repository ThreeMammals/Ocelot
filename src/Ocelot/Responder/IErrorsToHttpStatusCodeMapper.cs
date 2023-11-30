using Ocelot.Errors;

namespace Ocelot.Responder
{
    /// <summary>
    /// Defines mapping a list of Ocelot errors to a single appropriate HTTP status code.
    /// </summary>
    public interface IErrorsToHttpStatusCodeMapper
    {
        /// <summary>
        /// Maps a list of Ocelot <see cref="Error"/> to a single appropriate HTTP status code.
        /// </summary>
        /// <param name="errors">The collection of errors.</param>
        /// <returns>An integer value with HTTP status code.</returns>
        int Map(List<Error> errors);
    }
}
