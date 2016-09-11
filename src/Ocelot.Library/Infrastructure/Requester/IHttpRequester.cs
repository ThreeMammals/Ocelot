using System.Net.Http;
using System.Threading.Tasks;
using Flurl.Http;

namespace Ocelot.Library.Infrastructure.Requester
{
    public interface IHttpRequester
    {
        Task<HttpResponseMessage> GetResponse(string httpMethod, string downstreamUrl);
    }
}
