namespace Ocelot.Request.Mapper
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Responses;

    public interface IRequestMapper
    {
        Task<Response<HttpRequestMessage>> Map(HttpRequest request);
    }
}
