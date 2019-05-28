namespace Ocelot.Configuration.Creator
{
    using File;
    using Responses;
    using System.Linq;
    using System.Threading.Tasks;
    using Validator;

    public class FileInternalConfigurationCreator : IInternalConfigurationCreator
    {
        private readonly IConfigurationValidator _configurationValidator;
        private readonly IConfigurationCreator _configCreator;
        private readonly IDynamicsCreator _dynamicsCreator;
        private readonly IReRoutesCreator _reRoutesCreator;
        private readonly IAggregatesCreator _aggregatesCreator;

        public FileInternalConfigurationCreator(
            IConfigurationValidator configurationValidator,
            IReRoutesCreator reRoutesCreator,
            IAggregatesCreator aggregatesCreator,
            IDynamicsCreator dynamicsCreator,
            IConfigurationCreator configCreator
            )
        {
            _configCreator = configCreator;
            _dynamicsCreator = dynamicsCreator;
            _aggregatesCreator = aggregatesCreator;
            _reRoutesCreator = reRoutesCreator;
            _configurationValidator = configurationValidator;
        }

        public async Task<Response<IInternalConfiguration>> Create(FileConfiguration fileConfiguration)
        {
            var response = await _configurationValidator.IsValid(fileConfiguration);

            if (response.Data.IsError)
            {
                return new ErrorResponse<IInternalConfiguration>(response.Data.Errors);
            }

            var reRoutes = _reRoutesCreator.Create(fileConfiguration);

            var aggregates = _aggregatesCreator.Create(fileConfiguration, reRoutes);

            var dynamicReRoute = _dynamicsCreator.Create(fileConfiguration);

            var mergedReRoutes = reRoutes
                .Union(aggregates)
                .Union(dynamicReRoute)
                .ToList();

            var config = _configCreator.Create(fileConfiguration, mergedReRoutes);

            return new OkResponse<IInternalConfiguration>(config);
        }
    }
}
