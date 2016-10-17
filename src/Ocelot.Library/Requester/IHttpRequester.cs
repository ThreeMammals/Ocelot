namespace Ocelot.Library.Requester
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using RequestBuilder;
    using Responses;

    public interface IHttpRequester
    {
        Task<Response<HttpResponseMessage>> GetResponse(Request request);
    }
}
