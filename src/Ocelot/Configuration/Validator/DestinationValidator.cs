using FluentValidation;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Validator
{
    public class DestinationValidator : AbstractValidator<FileDestination>
    {
        public DestinationValidator()
        {
            //TODO: add tests for address making sure its valid?
            RuleFor(d => d.Address)
                .NotEmpty()
                .WithMessage("{PropertyName} cannot be empty");
        }
    }
}
