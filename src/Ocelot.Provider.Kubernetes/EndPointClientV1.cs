﻿using HTTPlease;
using KubeClient.Models;
using KubeClient.ResourceClients;

namespace Ocelot.Provider.Kubernetes
{
    public class EndPointClientV1 : KubeResourceClient
    {
        private readonly HttpRequest _collection;

        public EndPointClientV1(IKubeApiClient client) : base(client)
        {
            _collection = KubeRequest.Create("api/v1/namespaces/{Namespace}/endpoints/{ServiceName}");
        }

        public async Task<EndpointsV1> Get(string serviceName, string kubeNamespace = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentNullException(nameof(serviceName));
            }

            var request = _collection
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
    }
}
