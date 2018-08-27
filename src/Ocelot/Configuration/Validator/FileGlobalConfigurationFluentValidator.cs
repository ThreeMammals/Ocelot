namespace Ocelot.Configuration.Validator
{
    using System;
    using File;
    using FluentValidation;
    using Microsoft.Extensions.DependencyInjection;
    using Requester;

    public class FileQoSOptionsFluentValidator : AbstractValidator<FileQoSOptions>
    {
        private readonly IServiceProvider _serviceProvider;

        public FileQoSOptionsFluentValidator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            When(config => config != null, () => {
                RuleFor(config => config)
                    .Must(HasQosDelegatingHandlerRegistered)
                    .WithMessage("When using QoSOptions you must have a QosDelegatingHandlerDelegate registered in the DI container..maybe you got the Ocelot.Provider.Polly package!");
            });
        }

        private bool HasQosDelegatingHandlerRegistered(FileQoSOptions qosOptions)
        {
            var handler = _serviceProvider.GetService<QosDelegatingHandlerDelegate>();

            //see QoSOptions.UseQoS property for this...todo consolidate..
            if (qosOptions.ExceptionsAllowedBeforeBreaking > 0 &&
                qosOptions.TimeoutValue > 0 && handler == null)
            {
                return false;
            }

            return true;
        }
    }

    public class FileGlobalConfigurationFluentValidator : AbstractValidator<FileGlobalConfiguration>
    {
        public FileGlobalConfigurationFluentValidator(IServiceProvider serviceProvider)
        {
            RuleFor(configuration => configuration.QoSOptions)
                .SetValidator(new FileQoSOptionsFluentValidator(serviceProvider));
        }
    }
}
