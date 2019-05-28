namespace Ocelot.Configuration.Validator
{
    using Errors;
    using File;
    using FluentValidation;
    using Microsoft.Extensions.DependencyInjection;
    using Responses;
    using ServiceDiscovery;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class FileConfigurationFluentValidator : AbstractValidator<FileConfiguration>, IConfigurationValidator
    {
        private readonly List<ServiceDiscoveryFinderDelegate> _serviceDiscoveryFinderDelegates;

        public FileConfigurationFluentValidator(IServiceProvider provider, ReRouteFluentValidator reRouteFluentValidator, FileGlobalConfigurationFluentValidator fileGlobalConfigurationFluentValidator)
        {
            _serviceDiscoveryFinderDelegates = provider
                .GetServices<ServiceDiscoveryFinderDelegate>()
                .ToList();

            RuleFor(configuration => configuration.ReRoutes)
                .SetCollectionValidator(reRouteFluentValidator);

            RuleFor(configuration => configuration.GlobalConfiguration)
                .SetValidator(fileGlobalConfigurationFluentValidator);

            RuleForEach(configuration => configuration.ReRoutes)
                .Must((config, reRoute) => IsNotDuplicateIn(reRoute, config.ReRoutes))
                .WithMessage((config, reRoute) => $"{nameof(reRoute)} {reRoute.UpstreamPathTemplate} has duplicate");

            RuleForEach(configuration => configuration.ReRoutes)
                .Must((config, reRoute) => HaveServiceDiscoveryProviderRegistered(reRoute, config.GlobalConfiguration.ServiceDiscoveryProvider))
                .WithMessage((config, reRoute) => $"Unable to start Ocelot, errors are: Unable to start Ocelot because either a ReRoute or GlobalConfiguration are using ServiceDiscoveryOptions but no ServiceDiscoveryFinderDelegate has been registered in dependency injection container. Are you missing a package like Ocelot.Provider.Consul and services.AddConsul() or Ocelot.Provider.Eureka and services.AddEureka()?");

            RuleFor(configuration => configuration.GlobalConfiguration.ServiceDiscoveryProvider)
                .Must(HaveServiceDiscoveryProviderRegistered)
                .WithMessage((config, reRoute) => $"Unable to start Ocelot, errors are: Unable to start Ocelot because either a ReRoute or GlobalConfiguration are using ServiceDiscoveryOptions but no ServiceDiscoveryFinderDelegate has been registered in dependency injection container. Are you missing a package like Ocelot.Provider.Consul and services.AddConsul() or Ocelot.Provider.Eureka and services.AddEureka()?");

            RuleForEach(configuration => configuration.ReRoutes)
                .Must((config, reRoute) => IsNotDuplicateIn(reRoute, config.Aggregates))
                .WithMessage((config, reRoute) => $"{nameof(reRoute)} {reRoute.UpstreamPathTemplate} has duplicate aggregate");

            RuleForEach(configuration => configuration.Aggregates)
                .Must((config, aggregateReRoute) => IsNotDuplicateIn(aggregateReRoute, config.Aggregates))
                .WithMessage((config, aggregate) => $"{nameof(aggregate)} {aggregate.UpstreamPathTemplate} has duplicate aggregate");

            RuleForEach(configuration => configuration.Aggregates)
                .Must((config, aggregateReRoute) => AllReRoutesForAggregateExist(aggregateReRoute, config.ReRoutes))
                .WithMessage((config, aggregateReRoute) => $"ReRoutes for {nameof(aggregateReRoute)} {aggregateReRoute.UpstreamPathTemplate} either do not exist or do not have correct ServiceName property");

            RuleForEach(configuration => configuration.Aggregates)
                .Must((config, aggregateReRoute) => DoesNotContainReRoutesWithSpecificRequestIdKeys(aggregateReRoute, config.ReRoutes))
                .WithMessage((config, aggregateReRoute) => $"{nameof(aggregateReRoute)} {aggregateReRoute.UpstreamPathTemplate} contains ReRoute with specific RequestIdKey, this is not possible with Aggregates");
        }

        private bool HaveServiceDiscoveryProviderRegistered(FileReRoute reRoute, FileServiceDiscoveryProvider serviceDiscoveryProvider)
        {
            if (string.IsNullOrEmpty(reRoute.ServiceName))
            {
                return true;
            }

            if (serviceDiscoveryProvider?.Type?.ToLower() == "servicefabric")
            {
                return true;
            }

            return _serviceDiscoveryFinderDelegates.Any();
        }

        private bool HaveServiceDiscoveryProviderRegistered(FileServiceDiscoveryProvider serviceDiscoveryProvider)
        {
            if (serviceDiscoveryProvider == null)
            {
                return true;
            }

            if (serviceDiscoveryProvider?.Type?.ToLower() == "servicefabric")
            {
                return true;
            }

            return string.IsNullOrEmpty(serviceDiscoveryProvider.Type) || _serviceDiscoveryFinderDelegates.Any();
        }

        public async Task<Response<ConfigurationValidationResult>> IsValid(FileConfiguration configuration)
        {
            var validateResult = await ValidateAsync(configuration);

            if (validateResult.IsValid)
            {
                return new OkResponse<ConfigurationValidationResult>(new ConfigurationValidationResult(false));
            }

            var errors = validateResult.Errors.Select(failure => new FileValidationFailedError(failure.ErrorMessage));

            var result = new ConfigurationValidationResult(true, errors.Cast<Error>().ToList());

            return new OkResponse<ConfigurationValidationResult>(result);
        }

        private bool AllReRoutesForAggregateExist(FileAggregateReRoute fileAggregateReRoute, List<FileReRoute> reRoutes)
        {
            var reRoutesForAggregate = reRoutes.Where(r => fileAggregateReRoute.ReRouteKeys.Contains(r.Key));

            return reRoutesForAggregate.Count() == fileAggregateReRoute.ReRouteKeys.Count;
        }

        private static bool DoesNotContainReRoutesWithSpecificRequestIdKeys(FileAggregateReRoute fileAggregateReRoute,
            List<FileReRoute> reRoutes)
        {
            var reRoutesForAggregate = reRoutes.Where(r => fileAggregateReRoute.ReRouteKeys.Contains(r.Key));

            return reRoutesForAggregate.All(r => string.IsNullOrEmpty(r.RequestIdKey));
        }

        private static bool IsNotDuplicateIn(FileReRoute reRoute,
            List<FileReRoute> reRoutes)
        {
            var matchingReRoutes = reRoutes
                .Where(r => r.UpstreamPathTemplate == reRoute.UpstreamPathTemplate
                            && (r.UpstreamHost == reRoute.UpstreamHost || reRoute.UpstreamHost == null))
                .ToList();

            if (matchingReRoutes.Count == 1)
            {
                return true;
            }

            var allowAllVerbs = matchingReRoutes.Any(x => x.UpstreamHttpMethod.Count == 0);

            var duplicateAllowAllVerbs = matchingReRoutes.Count(x => x.UpstreamHttpMethod.Count == 0) > 1;

            var specificVerbs = matchingReRoutes.Any(x => x.UpstreamHttpMethod.Count != 0);

            var duplicateSpecificVerbs = matchingReRoutes.SelectMany(x => x.UpstreamHttpMethod).GroupBy(x => x.ToLower()).SelectMany(x => x.Skip(1)).Any();

            if (duplicateAllowAllVerbs || duplicateSpecificVerbs || (allowAllVerbs && specificVerbs))
            {
                return false;
            }

            return true;
        }

        private static bool IsNotDuplicateIn(FileReRoute reRoute,
            List<FileAggregateReRoute> aggregateReRoutes)
        {
            var duplicate = aggregateReRoutes
                .Any(a => a.UpstreamPathTemplate == reRoute.UpstreamPathTemplate
                            && a.UpstreamHost == reRoute.UpstreamHost
                            && reRoute.UpstreamHttpMethod.Select(x => x.ToLower()).Contains("get"));

            return !duplicate;
        }

        private static bool IsNotDuplicateIn(FileAggregateReRoute reRoute,
            List<FileAggregateReRoute> aggregateReRoutes)
        {
            var matchingReRoutes = aggregateReRoutes
                .Where(r => r.UpstreamPathTemplate == reRoute.UpstreamPathTemplate
                            && r.UpstreamHost == reRoute.UpstreamHost)
                .ToList();

            return matchingReRoutes.Count <= 1;
        }
    }
}
