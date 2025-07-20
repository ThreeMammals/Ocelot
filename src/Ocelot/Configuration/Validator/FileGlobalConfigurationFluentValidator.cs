using FluentValidation;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Validator;

public class FileGlobalConfigurationFluentValidator : AbstractValidator<FileGlobalConfiguration>
{
    //public FileGlobalConfigurationFluentValidator(FileQoSOptionsFluentValidator fileQoSOptionsFluentValidator)
    //{
    //    RuleFor(configuration => configuration.QoSOptions)
    //        .SetValidator(fileQoSOptionsFluentValidator);
    //}
    public FileGlobalConfigurationFluentValidator(FileQoSOptionsFluentValidator fileQoSOptValidator, FileAuthenticationOptionsValidator fileAuthOptValidator)
    {
        RuleFor(configuration => configuration.QoSOptions)
            .SetValidator(fileQoSOptValidator);

        RuleFor(configuration => configuration.AuthenticationOptions)
            .SetValidator(fileAuthOptValidator);
    }
}
