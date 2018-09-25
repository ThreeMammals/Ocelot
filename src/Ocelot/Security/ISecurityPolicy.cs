using Ocelot.Middleware;
using Ocelot.Responses;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ocelot.Security
{
    public interface ISecurityPolicy
    {
        Task<Response> Security(DownstreamContext context);
    }
}
