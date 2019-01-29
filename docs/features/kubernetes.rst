Kubernetes
==============

This feature was requested as part of `Issue 345 <https://github.com/ThreeMammals/Ocelot/issues/345>`_ . to add support for kubernetes's service discovery provider. 

The first thing you need to do is install the NuGet package that provides kubernetes support in Ocelot.

``Install-Package Ocelot.Provider.Kubernetes``

Then add the following to your ConfigureServices method.

.. code-block:: csharp

    s.AddOcelot()
     .AddKubernetes();

If you have services deployed in kubernetes you will normally use the naming service to access them.

The following example shows how to set up a ReRoute that will work in kubernetes. The most important thing is the ServiceName which is made up of the 
kubernetes service name. We also need to set up the ServiceDiscoveryProvider in 
GlobalConfiguration. The example here shows a typical configuration. It assumes kubernetes api server is running on 192.168.0.13 and that the api service is on port 443.

The example below is taken from the samples folder so please check it if this doesnt make sense!

.. code-block:: json

    {
  "ReRoutes": [
    {
      "DownstreamPathTemplate": "/api/values",
      "DownstreamScheme": "http",
      "UpstreamPathTemplate": "/values",
      "ServiceName": "downstreamservice",
      "UpstreamHttpMethod": [ "Get" ],
      "QoSOptions": {
        "ExceptionsAllowedBeforeBreaking": 3,
        "DurationOfBreak": 10000,
        "TimeoutValue": 5000
      },
      "FileCacheOptions": { "TtlSeconds": 15 }
    }
  ],
  "GlobalConfiguration": {
    "RequestIdKey": "OcRequestId",
    "AdministrationPath": "/administration",
    "ServiceDiscoveryProvider": {
      "Host": "192.168.0.13",
      "Port": 443,
      "Token": "txpc696iUhbVoudg164r93CxDTrKRVWG",
      "Namespace": "dev",
      "Type": "kube"
    }
  }
}

There is no way for Ocelot to work these out for you. 
