using System.Net.Http;
using System.Threading.Tasks;

using Ocelot.Configuration;

using Microsoft.AspNetCore.Http;

using Ocelot.Responses;

namespace Ocelot.Request.Mapper
{
    public interface IRequestMapper
    {
        Task<Response<HttpRequestMessage>> Map(HttpRequest request, DownstreamRoute downstreamRoute);
    }
}
