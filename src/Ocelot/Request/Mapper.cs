using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Ocelot.Request
{
    public class Mapper
    {
        public async Task<HttpRequestMessage> Map(Microsoft.AspNetCore.Http.HttpRequest request)
        {
            var requestMessage = new HttpRequestMessage()
            {
                Content = new ByteArrayContent(await ToByteArray(request.Body)),
                //Headers = request.Headers,
                //Method = request.Method,
                //Properties = request.P,
                //RequestUri = request.,
                //Version = null
            };

            return requestMessage;
        }

        private async Task<byte[]> ToByteArray(Stream stream)
        {
            using (stream)
            {
                using (var memStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memStream);
                    return memStream.ToArray();
                }
            }
        }
    }
}

