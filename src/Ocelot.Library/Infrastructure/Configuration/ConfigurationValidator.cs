using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.Configuration
{
    public class ConfigurationValidator : IConfigurationValidator
    {
        public Response<ConfigurationValidationResult> IsValid(Configuration configuration)
        {
            var duplicateUpstreamTemplates = configuration.ReRoutes
                .Select(r => r.DownstreamTemplate)
                .GroupBy(r => r)
                .Where(r => r.Count() > 1)
                .Select(r => r.Key)
                .ToList();

            if (duplicateUpstreamTemplates.Count <= 0)
            {
                return new OkResponse<ConfigurationValidationResult>(new ConfigurationValidationResult());
            }
                
            var errors = new List<Error>();

            foreach (var duplicateUpstreamTemplate in duplicateUpstreamTemplates)
            {
                var error = new DownstreamTemplateAlreadyUsedError(string.Format("Duplicate DownstreamTemplate: {0}", 
                    duplicateUpstreamTemplate));
                errors.Add(error);
            }

            return new ErrorResponse<ConfigurationValidationResult>(errors);
        }
    }
}
