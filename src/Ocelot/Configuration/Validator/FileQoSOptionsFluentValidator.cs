using FluentValidation;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Validator
{
    using Requester;

    public class FileQoSOptionsFluentValidator : AbstractValidator<FileQoSOptions>
    {
        private readonly QosDelegatingHandlerDelegate _qosDelegatingHandlerDelegate;

        public FileQoSOptionsFluentValidator(QosDelegatingHandlerDelegate qosDelegatingHandlerDelegate)
        {
            _qosDelegatingHandlerDelegate = qosDelegatingHandlerDelegate;
            
            When(qosOptions => qosOptions.TimeoutValue > 0 && qosOptions.ExceptionsAllowedBeforeBreaking > 0, () => {
                RuleFor(qosOptions => qosOptions)
                .Must(HaveQosHandlerRegistered)
                .WithMessage("Unable to start Ocelot because either a ReRoute or GlobalConfiguration are using QoSOptions but no QosDelegatingHandlerDelegate has been registered in dependency injection container. Are you missing a package like Ocelot.Provider.Polly and services.AddPolly()?");            
            });
        }

        private bool HaveQosHandlerRegistered(FileQoSOptions arg)
        {
            return _qosDelegatingHandlerDelegate != null;
        }
    }
}
