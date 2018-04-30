﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Ocelot.Configuration;
using Ocelot.Responses;

namespace Ocelot.Requester.QoS
{
    public class QosProviderHouse : IQosProviderHouse
    {
        private readonly ConcurrentDictionary<string, IQoSProvider> _qoSProviders;
        private readonly IQoSProviderFactory _qoSProviderFactory;

        public QosProviderHouse(IQoSProviderFactory qoSProviderFactory)
        {
            _qoSProviderFactory = qoSProviderFactory;
            _qoSProviders = new ConcurrentDictionary<string, IQoSProvider>();
        }

        public Response<IQoSProvider> Get(DownstreamReRoute reRoute)
        {
            try
            {
                if (_qoSProviders.TryGetValue(reRoute.QosKey, out var qosProvider))
                {
                    if (reRoute.IsQos && qosProvider.CircuitBreaker == null)
                    {
                        qosProvider = _qoSProviderFactory.Get(reRoute);
                        Add(reRoute.QosKey, qosProvider);
                    }

                    return new OkResponse<IQoSProvider>(_qoSProviders[reRoute.QosKey]);
                }

                qosProvider = _qoSProviderFactory.Get(reRoute);
                Add(reRoute.QosKey, qosProvider);
                return new OkResponse<IQoSProvider>(qosProvider);
            }
            catch (Exception ex)
            {
                return new ErrorResponse<IQoSProvider>(new List<Ocelot.Errors.Error>()
                {
                    new UnableToFindQoSProviderError($"unabe to find qos provider for {reRoute.QosKey}, exception was {ex}")
                });
            }
        }

        private void Add(string key, IQoSProvider qosProvider)
        {
            _qoSProviders.AddOrUpdate(key, qosProvider, (x, y) => qosProvider);
        }
    }
}
