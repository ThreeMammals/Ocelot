Service Fabric
==============

  [#f1]_ Feature of: :doc:`../features/servicediscovery`

If you have services deployed in Azure `Service Fabric`_ you will normally use the naming service to access them.

This feature allows to set up a route that will work in `Service Fabric`_.

Configuration
-------------

The most important thing is the ``ServiceName``, which is composed of the `Service Fabric`_ application name followed by the specific service name.
Additionally, the ``ServiceDiscoveryProvider`` needs to be configured in ``GlobalConfiguration``.
The example below demonstrates a typical configuration.
It assumes that *Service Fabric* is running on ``localhost`` and that the naming service is using port ``19081``.

  The example below is taken from the :ref:`sf-sample`, so please check it if this doesn't make sense!

.. code-block:: json
  :emphasize-lines: 8, 17

  {
    "Routes": [
      {
        "DownstreamScheme": "http",
        "DownstreamPathTemplate": "/api/values",
        "UpstreamPathTemplate": "/EquipmentInterfaces",
        "UpstreamHttpMethod": [ "Get" ],
        "ServiceName": "OcelotServiceApplication/OcelotApplicationService"
      }
    ],
    "GlobalConfiguration": {
      "BaseUrl": "https://ocelot.net",
      "RequestIdKey": "Oc-RequestId",
      "ServiceDiscoveryProvider": {
        "Host": "localhost",
        "Port": 19081,
        "Type": "ServiceFabric"
      }
    }
  }

If you are using stateless or guest exe services, Ocelot can proxy through the naming service without requiring additional configuration.
However, if you are using stateful or actor services, you must include the ``PartitionKind`` and ``PartitionKey`` query string values in the client request, e.g.,

  GET ``http://ocelot.com/EquipmentInterfaces?PartitionKind=xxx&PartitionKey=xxx``

There is no way for Ocelot to determine these values automatically.

.. _sf-placeholders:

Placeholders [#f2]_
-------------------

In Ocelot, *placeholders* for variables can be inserted into the ``UpstreamPathTemplate`` and ``ServiceName`` using the format ``{something}``.

  **Note**: The *placeholder* variable must exist in both the ``DownstreamPathTemplate`` (or ``ServiceName``) and the ``UpstreamPathTemplate``.
  Specifically, the ``UpstreamPathTemplate`` must include all *placeholders* found in the ``DownstreamPathTemplate`` and ``ServiceName``.
  Failure to meet this requirement will prevent Ocelot from starting due to validation errors, which are logged.

Once the validation stage is completed, Ocelot replaces the placeholder values in the ``UpstreamPathTemplate`` with those from the ``DownstreamPathTemplate`` and/or ``ServiceName`` for each processed request.
Thus, the *Service Fabric* :ref:`sf-placeholders` feature operates similarly to the original routing :ref:`routing-placeholders` feature but includes the ``ServiceName`` property in its processing.

| Here is an example of the ``version`` variable in the *Service Fabric* service name.
| Given the following `ocelot.json`_:

.. code-block:: json
  :emphasize-lines: 6, 14

  {
    "Routes": [
      {
        "UpstreamPathTemplate": "/api/{version}/{endpoint}",
        "DownstreamPathTemplate": "/{endpoint}",
        "ServiceName": "Service_{version}/Api",
      }
    ],
    "GlobalConfiguration": {
      "BaseUrl": "https://ocelot.com",
      "ServiceDiscoveryProvider": {
        "Host": "localhost",
        "Port": 19081,
        "Type": "ServiceFabric"
      }
    }
  }

When you make Ocelot request:

* ``GET https://ocelot.com/api/1.0/products``

The *Service Fabric* request will be:

* ``GET http://localhost:19081/Service_1.0/Api/products``

.. _sf-sample:

Sample
------

In order to introduce the *Service Fabric* feature, we have prepared a sample:

  | Project: `samples <https://github.com/ThreeMammals/Ocelot/tree/main/samples>`_ / `ServiceFabric <https://github.com/ThreeMammals/Ocelot/tree/main/samples/ServiceFabric/>`_
  | Solution: `Ocelot.Samples.ServiceFabric.sln <https://github.com/ThreeMammals/Ocelot/tree/main/samples/ServiceFabric/Ocelot.Samples.ServiceFabric.sln>`_

This solution includes the following projects:

- ``Ocelot.Samples.ServiceFabric.ApiGateway.csproj``
- ``Ocelot.Samples.ServiceFabric.DownstreamService.csproj``

Complete instructions for running this solution can be found in the `README.md <https://github.com/ThreeMammals/Ocelot/blob/main/samples/ServiceFabric/README.md>`_ file.

.. note::
  Please consider this solution as a demonstration of integration; it is outdated as of 2025.
  Therefore, this solution is a draft and requires further development for practical usage and deployment in the Azure cloud.
  Additionally, refer to the team's notes in the :ref:`sd-service-fabric` section!

""""

.. [#f1] Historically, the "`Service Fabric <#service-fabric>`__" feature is one of Ocelot's earliest and foundational features, first requested in issue `238`_. It was initially released in version `3.1.9`_.
.. [#f2] The ":ref:`Placeholders <sf-placeholders>`" feature was requested in issue `721`_ and implemented by pull request `722`_ as part of version `13.0.0`_.

.. _Service Fabric: https://azure.microsoft.com/en-us/products/service-fabric/
.. _ocelot.json: https://github.com/ThreeMammals/Ocelot/blob/main/samples/ServiceFabric/ApiGateway/ocelot.json

.. _238: https://github.com/ThreeMammals/Ocelot/issues/238
.. _721: https://github.com/ThreeMammals/Ocelot/issues/721
.. _722: https://github.com/ThreeMammals/Ocelot/pull/722
.. _3.1.9: https://github.com/ThreeMammals/Ocelot/releases/tag/3.1.9
.. _13.0.0: https://github.com/ThreeMammals/Ocelot/releases/tag/13.0.0
