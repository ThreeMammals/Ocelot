using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.File;
using Ocelot.QualityOfService;

namespace Ocelot.Configuration.Validator;

public class FileQoSOptionsFluentValidator : AbstractValidator<FileQoSOptions>
{
    private readonly IServiceProvider _provider;

    public FileQoSOptionsFluentValidator(IServiceProvider provider)
    {
        _provider = provider;
        When(UseQos, CheckRules);
    }

    private bool UseQos(FileQoSOptions opts) => new QoSOptions(opts).UseQos;
    private void CheckRules()
    {
        RuleFor(qos => qos)
            .Must(HaveQosHandlerRegistered)
            .WithMessage($"Unable to start Ocelot because either a {nameof(Route)} or {nameof(FileConfiguration.GlobalConfiguration)} is using {nameof(FileRoute.QoSOptions)}, but no {nameof(QosDelegatingHandlerDelegate)} has been registered in the dependency injection container. Are you missing an external package like Ocelot.QualityOfService.Polly (and calling AddPolly()), or the built-in QoS support (via AddQualityOfService())?");
    }

    private bool HaveQosHandlerRegistered(FileQoSOptions arg)
    {
        var _qosDelegate = _provider.GetService<QosDelegatingHandlerDelegate>();
        return _qosDelegate != null;
    }
}
