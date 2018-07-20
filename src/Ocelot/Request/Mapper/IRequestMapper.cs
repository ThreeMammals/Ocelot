namespace Ocelot.Request.Mapper
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Ocelot.Responses;

    public interface IRequestMapper
    {
        Response<HttpRequestMessage> Map(HttpRequest request);
    }
}
