namespace Ocelot.Configuration.Validator
{
    using FluentValidation;
    using File;
    using Requester;

    public class FileGlobalConfigurationFluentValidator : AbstractValidator<FileGlobalConfiguration>
    {
        public FileGlobalConfigurationFluentValidator(QosDelegatingHandlerDelegate qosDelegatingHandlerDelegate)
        {
           RuleFor(configuration => configuration.QoSOptions)
                .SetValidator(new FileQoSOptionsFluentValidator(qosDelegatingHandlerDelegate));
        }
    }
}
