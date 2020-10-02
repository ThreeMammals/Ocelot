using FluentValidation;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Validator
{
    public class ClusterValidator : AbstractValidator<FileCluster>
    {
        public ClusterValidator(DestinationValidator destinationValidator)
        {
            //TODO: Rules here for load balance and http client when it is implemented?
            RuleFor(c => c.Destinations)
                .NotEmpty()
                .WithMessage("When not using service discovery Cluster.Destinations must be set or Ocelot cannot find your service!");

            RuleForEach(cluster => cluster.Destinations.Values)
                .SetValidator(destinationValidator);
        }
    }
}
