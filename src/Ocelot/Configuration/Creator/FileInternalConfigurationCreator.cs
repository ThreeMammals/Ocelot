using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Ocelot.Cache;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Validator;
using Ocelot.DependencyInjection;
using Ocelot.Logging;
using Ocelot.Responses;
using Microsoft.Extensions.DependencyInjection;

namespace Ocelot.Configuration.Creator
{
    using LoadBalancer.LoadBalancers;

    /// <summary>
    /// Register as singleton
    /// </summary>
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

            var reRoutes = _reRoutesCreator.ReRoutes(fileConfiguration);

            var aggregates = _aggregatesCreator.Aggregates(fileConfiguration, reRoutes);

            var dynamicReRoute = _dynamicsCreator.Dynamics(fileConfiguration);

            var mergedReRoutes = reRoutes
                .Union(aggregates)
                .Union(dynamicReRoute)
                .ToList();

            var config = _configCreator.InternalConfiguration(fileConfiguration, mergedReRoutes);

            return new OkResponse<IInternalConfiguration>(config);
        }
    }
}
