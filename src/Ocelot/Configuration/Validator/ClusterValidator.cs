namespace Ocelot.Configuration.Validator
{
    using FluentValidation;
    using Ocelot.Configuration.File;

    public class ClusterValidator : AbstractValidator<FileCluster>
    {
        public ClusterValidator()
        {
            RuleFor(r => r.Destinations)
                .NotEmpty()
                .WithMessage("When not using service discovery Cluster.Destinations must be set or Ocelot cannot find your service!");
        }
    }
}
