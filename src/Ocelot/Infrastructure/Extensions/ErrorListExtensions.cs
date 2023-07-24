using Ocelot.Errors;

namespace Ocelot.Infrastructure.Extensions
{
    public static class ErrorListExtensions
    {
        public static string ToErrorString(this List<Error> errors)
        {
            var listOfErrorStrings = errors.Select(x => "Error Code: " + x.Code + " Message: " + x.Message);
            return string.Join(' ', listOfErrorStrings);
        }
    }
}
