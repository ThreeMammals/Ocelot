using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ocelot.Library.Infrastructure.Errors;
using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.DownstreamRouteFinder
{
    public class UnableToFindDownstreamRouteError : Error
    {
        public UnableToFindDownstreamRouteError() : base("UnableToFindDownstreamRouteError", OcelotErrorCode.UnableToFindDownstreamRouteError)
        {
        }
    }
}
