namespace Ocelot.Configuration.Validator
{
    using FluentValidation;
    using File;

    public class FileGlobalConfigurationFluentValidator : AbstractValidator<FileGlobalConfiguration>
    {
        public FileGlobalConfigurationFluentValidator(FileQoSOptionsFluentValidator fileQoSOptionsFluentValidator)
        {
            RuleFor(configuration => configuration.QoSOptions)
                .SetValidator(fileQoSOptionsFluentValidator);
        }
    }
}
