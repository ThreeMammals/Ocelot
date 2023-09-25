﻿using Ocelot.Errors;
using System.Collections.Generic;

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

        public bool IsError { get; }

        public List<Error> Errors { get; }
    }
}
