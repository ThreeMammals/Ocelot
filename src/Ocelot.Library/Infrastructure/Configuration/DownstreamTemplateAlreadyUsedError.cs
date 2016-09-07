using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.Configuration
{
    public class DownstreamTemplateAlreadyUsedError : Error
    {
        public DownstreamTemplateAlreadyUsedError(string message) : base(message)
        {
        }
    }
}
