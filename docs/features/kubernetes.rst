.. |K8s Logo| image:: https://kubernetes.io/images/favicon.png
  :alt: K8s Logo
  :width: 40

|K8s Logo| Kubernetes [#f1]_ aka K8s
====================================

    A part of feature: :doc:`../features/servicediscovery` [#f2]_

Ocelot will call the `K8s <https://kubernetes.io/>`_ endpoints API in a given namespace to get all of the endpoints for a pod and then load balance across them.
Ocelot used to use the services API to send requests to the `K8s <https://kubernetes.io/>`__ service but this was changed in `PR 1134 <https://github.com/ThreeMammals/Ocelot/pull/1134>`_ because the service did not load balance as expected.

Install
-------

The first thing you need to do is install the `NuGet package <https://www.nuget.org/packages/Ocelot.Provider.Kubernetes>`_ that provides **Kubernetes** [#f1]_ support in Ocelot:

.. code-block:: powershell

    Install-Package Ocelot.Provider.Kubernetes

Then add the following to your ``ConfigureServices`` method:

.. code-block:: csharp

    services.AddOcelot().AddKubernetes();

If you have services deployed in Kubernetes, you will normally use the naming service to access them.
Default ``usePodServiceAccount = true``, which means that Service Account using Pod to access the service of the K8s cluster needs to be Service Account based on RBAC authorization:

.. code-block:: csharp

    public static class OcelotBuilderExtensions
    {
        public static IOcelotBuilder AddKubernetes(this IOcelotBuilder builder, bool usePodServiceAccount = true);
    }

You can replicate a Permissive using RBAC role bindings (see `Permissive RBAC Permissions <https://kubernetes.io/docs/reference/access-authn-authz/rbac/#permissive-rbac-permissions>`_),
K8s API server and token will read from pod.

.. code-block:: bash

    kubectl create clusterrolebinding permissive-binding --clusterrole=cluster-admin --user=admin --user=kubelet --group=system:serviceaccounts

Configuration
-------------

The following examples show how to set up a Route that will work in Kubernetes.
The most important thing is the **ServiceName** which is made up of the Kubernetes service name.
We also need to set up the **ServiceDiscoveryProvider** in **GlobalConfiguration**.

Kube default provider
^^^^^^^^^^^^^^^^^^^^^

The example here shows a typical configuration:

.. code-block:: json

    "Routes": [
      {
        "ServiceName": "downstreamservice",
        // ...
      }
    ],
    "GlobalConfiguration": {
      "ServiceDiscoveryProvider": {
        "Host": "192.168.0.13",
        "Port": 443,
        "Token": "txpc696iUhbVoudg164r93CxDTrKRVWG",
        "Namespace": "Dev",
        "Type": "Kube"
      }
    }

Service deployment in **Namespace** ``Dev``, **ServiceDiscoveryProvider** type is ``Kube``, you also can set :ref:`k8s-pollkube-provider` type.
Note: **Host**, **Port** and **Token** are no longer in use.

.. _k8s-pollkube-provider:

PollKube provider
^^^^^^^^^^^^^^^^^

You use Ocelot to poll Kubernetes for latest service information rather than per request.
If you want to poll Kubernetes for the latest services rather than per request (default behaviour) then you need to set the following configuration:

.. code-block:: json

  "ServiceDiscoveryProvider": {
    "Namespace": "dev",
    "Type": "PollKube",
    "PollingInterval": 100 // ms
  } 

The polling interval is in milliseconds and tells Ocelot how often to call Kubernetes for changes in service configuration.

Please note, there are tradeoffs here.
If you poll Kubernetes, it is possible Ocelot will not know if a service is down depending on your polling interval and you might get more errors than if you get the latest services per request.
This really depends on how volatile your services are.
We doubt it will matter for most people and polling may give a tiny performance improvement over calling Kubernetes per request.
There is no way for Ocelot to work these out for you. 

Global vs Route levels
^^^^^^^^^^^^^^^^^^^^^^

If your downstream service resides in a different namespace, you can override the global setting at the Route-level by specifying a **ServiceNamespace**:

.. code-block:: json

  "Routes": [
    {
      "ServiceName": "downstreamservice",
      "ServiceNamespace": "downstream-namespace"
    }
  ]

""""

.. [#f1] `Wikipedia <https://en.wikipedia.org/wiki/Kubernetes>`_ | `K8s Website <https://kubernetes.io/>`_ | `K8s Documentation <https://kubernetes.io/docs/>`_ | `K8s GitHub <https://github.com/kubernetes/kubernetes>`_
.. [#f2] This feature was requested as part of `issue 345 <https://github.com/ThreeMammals/Ocelot/issues/345>`_ to add support for `Kubernetes <https://kubernetes.io/>`_ :doc:`../features/servicediscovery` provider. 
