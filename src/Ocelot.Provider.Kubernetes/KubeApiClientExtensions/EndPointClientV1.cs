using HTTPlease;
using KubeClient;
using KubeClient.Models;
using KubeClient.ResourceClients;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ocelot.Provider.Kubernetes.KubeApiClientExtensions
{
    public class EndPointClientV1 : KubeResourceClient
    {
        public EndPointClientV1(IKubeApiClient client) : base(client)
        {
        }

        public async Task<EndpointsV1> Get(string serviceName, string kubeNamespace = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(serviceName)) throw new ArgumentNullException(nameof(serviceName));

            var response = await Http.GetAsync(
                Requests.Collection.WithTemplateParameters(new
                {
                    Namespace = kubeNamespace ?? KubeClient.DefaultNamespace,
                    ServiceName = serviceName
                }),
                cancellationToken: cancellationToken
            );

            if (response.IsSuccessStatusCode)
                return await response.ReadContentAsAsync<EndpointsV1>();

            return null;
        }

        public static class Requests
        {
            public static readonly HttpRequest Collection = KubeRequest.Create("api/v1/namespaces/{Namespace}/endpoints/{ServiceName}");
        }
    }
}
