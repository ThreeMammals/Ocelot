using FluentValidation;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Validator;

public class FileGlobalConfigurationFluentValidator : AbstractValidator<FileGlobalConfiguration>
{
    public FileGlobalConfigurationFluentValidator(
        FileQoSOptionsFluentValidator qosValidator,
        FileAuthenticationOptionsValidator authValidator)
    {
        RuleFor(configuration => configuration.QoSOptions)
            .SetValidator(qosValidator);

        RuleFor(configuration => configuration.AuthenticationOptions)
            .SetValidator(authValidator);
    }
}
