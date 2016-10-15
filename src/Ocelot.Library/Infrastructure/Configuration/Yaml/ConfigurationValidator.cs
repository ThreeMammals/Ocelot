using System.Collections.Generic;
using System.Linq;
using Ocelot.Library.Infrastructure.Errors;
using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.Configuration.Yaml
{
    public class ConfigurationValidator : IConfigurationValidator
    {
        public Response<ConfigurationValidationResult> IsValid(YamlConfiguration configuration)
        {
            var duplicateUpstreamTemplates = configuration.ReRoutes
                .Select(r => r.DownstreamTemplate)
                .GroupBy(r => r)
                .Where(r => r.Count() > 1)
                .Select(r => r.Key)
                .ToList();

            if (duplicateUpstreamTemplates.Count <= 0)
            {
                return new OkResponse<ConfigurationValidationResult>(new ConfigurationValidationResult(false));
            }
                
            var errors = new List<Error>();

            foreach (var duplicateUpstreamTemplate in duplicateUpstreamTemplates)
            {
                var error = new DownstreamTemplateAlreadyUsedError(string.Format("Duplicate DownstreamTemplate: {0}", 
                    duplicateUpstreamTemplate));
                errors.Add(error);
            }

            return new OkResponse<ConfigurationValidationResult>(new ConfigurationValidationResult(true, errors));
        }
    }
}
