using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;

namespace Ocelot.Request.Mapper;

public interface IRequestMapper
{
    HttpRequestMessage Map(HttpRequest request, DownstreamRoute downstreamRoute);
}
