using Ocelot.Configuration.File;

using FluentValidation;

namespace Ocelot.Configuration.Validator
{
    public class HostAndPortValidator : AbstractValidator<FileDownstreamHostConfig>
    {
        public HostAndPortValidator()
        {
            RuleFor(r => r.Host)
                .NotEmpty()
                .WithMessage("When not using service discovery Host must be set on DownstreamHostAndPorts if you are not using Route.Host or Ocelot cannot find your service!");
        }
    }
}
