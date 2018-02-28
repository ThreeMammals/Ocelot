using System;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Butterfly.DataContract.Tracing;
using MessagePack;
using Newtonsoft.Json;

namespace Butterfly.Client
{
    public class HttpButterflySender : IButterflySender
    {
        protected const string spanUrl = "/api/span";

        protected readonly HttpClient _httpClient;

        // ReSharper disable once PublicConstructorInAbstractClass
        public HttpButterflySender(string collectorUrl)
            : this(new HttpClient(new HttpClientHandler() {UseProxy = false}), collectorUrl)
        {
        }

        // ReSharper disable once PublicConstructorInAbstractClass
        public HttpButterflySender(HttpClient httpClient, string collectorUrl)
        {
            if (collectorUrl == null)
            {
                throw new ArgumentNullException(nameof(collectorUrl));
            }

            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _httpClient.BaseAddress = new Uri(collectorUrl);
        }

        public virtual Task SendSpanAsync(Span[] spans, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (spans == null)
            {
                throw new ArgumentNullException(nameof(spans));
            }

            //var content = new StringContent(JsonConvert.SerializeObject(spans), Encoding.UTF8, "application/json");
            var content = new ByteArrayContent(MessagePackSerializer.Serialize(spans));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-msgpack");
            return _httpClient.PostAsync(spanUrl, content, cancellationToken);
        }
    }
}