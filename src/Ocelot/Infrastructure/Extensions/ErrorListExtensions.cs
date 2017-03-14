using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Primitives;
using Ocelot.Errors;

namespace Ocelot.Infrastructure.Extensions
{
    public static class ErrorListExtensions
    {
        public static string ToErrorString(this List<Error> errors)
        {
            var listOfErrorStrings = errors.Select(x => "Error Code: " + x.Code.ToString() + " Message: " + x.Message);
            return string.Join(" ", listOfErrorStrings);
        }
     }
}
