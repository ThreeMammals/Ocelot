namespace Ocelot.Library.Configuration.Yaml
{
    using System.Collections.Generic;
    using Errors;

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
