using FluentValidation;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Validator
{
    public class HostAndPortValidator : AbstractValidator<FileHostAndPort>
    {
        public HostAndPortValidator()
        {
            RuleFor(r => r.Host).NotEmpty().WithMessage("When not using service discovery Host must be set on DownstreamHostAndPorts if you are not using ReRoute.Host or Ocelot cannot find your service!");
        }
    }
}
