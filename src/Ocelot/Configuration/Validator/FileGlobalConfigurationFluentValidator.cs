using Ocelot.Configuration.File;

using FluentValidation;

namespace Ocelot.Configuration.Validator
{
    public class FileGlobalConfigurationFluentValidator : AbstractValidator<FileGlobalConfiguration>
    {
        public FileGlobalConfigurationFluentValidator(FileQoSOptionsFluentValidator fileQoSOptionsFluentValidator)
        {
            RuleFor(configuration => configuration.QoSOptions)
                .SetValidator(fileQoSOptionsFluentValidator);
        }
    }
}
