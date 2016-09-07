using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.Configuration
{
    public interface IConfigurationValidator
    {
        Response<ConfigurationValidationResult> IsValid(Configuration configuration);
    }
}
