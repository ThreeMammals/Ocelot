using HTTPlease;
using KubeClient.Models;
using KubeClient.ResourceClients;
using Ocelot.Provider.Kubernetes.Interfaces;

namespace Ocelot.Provider.Kubernetes
{
    public class EndPointClientV1 : KubeResourceClient, IEndPointClient
    {
        private readonly HttpRequest _byName;
        private readonly HttpRequest _watchByName;

        public EndPointClientV1(IKubeApiClient client) : base(client)
        {
            _byName = KubeRequest.Create("api/v1/namespaces/{Namespace}/endpoints/{ServiceName}");
            _watchByName = KubeRequest.Create("api/v1/watch/namespaces/{Namespace}/endpoints/{ServiceName}");
        }

        public async Task<EndpointsV1> GetAsync(string serviceName, string kubeNamespace = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentNullException(nameof(serviceName));
            }

            var request = _byName
                .WithTemplateParameters(new
                {
                    Namespace = kubeNamespace ?? KubeClient.DefaultNamespace,
                    ServiceName = serviceName,
                });

            var response = await Http.GetAsync(request, cancellationToken);

            return response.IsSuccessStatusCode
                ? await response.ReadContentAsAsync<EndpointsV1>()
                : null;
        }

        public IObservable<IResourceEventV1<EndpointsV1>> Watch(string serviceName, string kubeNamespace,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentNullException(nameof(serviceName));
            }

            return ObserveEvents<EndpointsV1>(
                _watchByName.WithTemplateParameters(new
                {
                    ServiceName = serviceName,
                    Namespace = kubeNamespace ?? KubeClient.DefaultNamespace,
                }),
                "watch v1/Endpoints '" + serviceName + "' in namespace " +
                (kubeNamespace ?? KubeClient.DefaultNamespace));
        }
    }
}
