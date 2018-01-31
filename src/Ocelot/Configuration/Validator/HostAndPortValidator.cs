using FluentValidation;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Validator
{
    public class HostAndPortValidator : AbstractValidator<FileHostAndPort>
    {
        public HostAndPortValidator()
        {
            RuleFor(r => r.DownstreamHost).NotEmpty().WithMessage("When not using service discovery DownstreamHost must be set on DownstreamHostAndPorts if you are not using ReRoute.DownstreamHost or Ocelot cannot find your service!");
        }
    }
}
