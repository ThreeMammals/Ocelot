namespace Ocelot.Configuration.Validator
{
    using System.Collections.Generic;
    using Ocelot.Errors;

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

        public bool IsError { get; }

        public List<Error> Errors { get; } 
    }
}
