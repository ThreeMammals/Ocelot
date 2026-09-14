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

        When(configuration => configuration.WebSocket != null, () =>
        {
            RuleFor(configuration => configuration.WebSocket.BufferSize)
                .Must(size => !size.HasValue || size.Value > 0)
                .WithMessage("GlobalConfiguration.WebSocket.BufferSize is negative or zero");
        });
    }
}
