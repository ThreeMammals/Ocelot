using System.Collections.Generic;
using Ocelot.Errors;

namespace Ocelot.Configuration.Validator
{
    public class ConfigurationValidationResult
    {
        public ConfigurationValidationResult(bool isError)
        {
            IsError = isError;
            Errors = new List<Error>();
        }

        public ConfigurationValidationResult(bool isError, List<Error> errors)
        {
            IsError = isError;
            Errors = errors;
        }

        public bool IsError { get; private set; }

        public List<Error> Errors { get; private set; } 
    }
}
