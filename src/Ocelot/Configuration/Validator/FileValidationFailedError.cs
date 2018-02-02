using System;
using System.Collections.Generic;
using System.Text;
using Ocelot.Errors;

namespace Ocelot.Configuration.Validator
{
    public class FileValidationFailedError : Error
    {
        public FileValidationFailedError(string message) : base(message, OcelotErrorCode.FileValidationFailedError)
        {

        }
    }
}
