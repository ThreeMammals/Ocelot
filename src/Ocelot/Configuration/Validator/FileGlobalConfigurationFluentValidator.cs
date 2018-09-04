using FluentValidation;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Validator
{
    using Requester;

    public class FileGlobalConfigurationFluentValidator : AbstractValidator<FileGlobalConfiguration>
    {
        private readonly QosDelegatingHandlerDelegate _qosDelegatingHandlerDelegate;

        public FileGlobalConfigurationFluentValidator(QosDelegatingHandlerDelegate qosDelegatingHandlerDelegate)
        {
            _qosDelegatingHandlerDelegate = qosDelegatingHandlerDelegate;
            
           RuleFor(configuration => configuration.QoSOptions)
                .SetValidator(new FileQoSOptionsFluentValidator(_qosDelegatingHandlerDelegate));
        }

    }
}
