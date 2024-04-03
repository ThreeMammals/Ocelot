Service Fabric
==============

If you have services deployed in `Azure Service Fabric <https://azure.microsoft.com/en-us/products/service-fabric/>`_ you will normally use the naming service to access them.

The following example shows how to set up a Route that will work in *Service Fabric*.
The most important thing is the **ServiceName** which is made up of the *Service Fabric* application name then the specific service name.
We also need to set up the **ServiceDiscoveryProvider** in **GlobalConfiguration**.
The example here shows a typical configuration.
It assumes *Service Fabric* is running on ``localhost`` and that the naming service is on port ``19081``.

The example below is taken from the `OcelotServiceFabric <https://github.com/ThreeMammals/Ocelot/tree/main/samples/OcelotServiceFabric>`_ sample, so please check it if this doesn't make sense!

.. code-block:: json

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
      "BaseUrl": "https://ocelot.com"
      "RequestIdKey": "OcRequestId",
      "ServiceDiscoveryProvider": {
        "Host": "localhost",
        "Port": 19081,
        "Type": "ServiceFabric"
      }
    }
  }

If you are using stateless / guest exe services, Ocelot will be able to proxy through the naming service without anything else.
However, if you are using statefull / actor services, you must send the **PartitionKind** and **PartitionKey** query string values with the client request e.g.

    GET ``http://ocelot.com/EquipmentInterfaces?PartitionKind=xxx&PartitionKey=xxx``

There is no way for Ocelot to work these out for you.

.. _sf-placeholders:

Placeholders in Service Name [#f1]_
-----------------------------------

In Ocelot, you can insert placeholders for variables into your ``UpstreamPathTemplate`` and ``ServiceName`` using the format ``{something}``.

Important Note: The placeholder variable must exist in both the ``DownstreamPathTemplate`` (or ``ServiceName``) and the ``UpstreamPathTemplate``.
Specifically, the ``UpstreamPathTemplate`` should include all placeholders from the ``DownstreamPathTemplate`` and ``ServiceName``.
Failure to do so will result in Ocelot not starting due to validation errors, which are logged.

Once the validation stage is cleared, Ocelot will replace the placeholder values in the ``UpstreamPathTemplate`` with those from the ``DownstreamPathTemplate`` and/or ``ServiceName`` for each processed request.
Thus, the :ref:`sf-placeholders` behave similarly to the :ref:`routing-placeholders` feature, but with the ``ServiceName`` property considered during processing.

Placeholders example
^^^^^^^^^^^^^^^^^^^^

Here is the example of the ``version`` variable in *Service Fabric* service name.

**Given** you have the following `ocelot.json`_:

.. code-block:: json

  {
    "Routes": [
      {
        "UpstreamPathTemplate": "/api/{version}/{endpoint}",
        "DownstreamPathTemplate": "/{endpoint}",
        "ServiceName": "Service_{version}/Api",
      }
    ],
    "GlobalConfiguration": {
      "BaseUrl": "https://ocelot.com"
      "ServiceDiscoveryProvider": {
        "Host": "localhost",
        "Port": 19081,
        "Type": "ServiceFabric"
      }
    }
  }

**When** you make a request: GET ``https://ocelot.com/api/1.0/products``

**Then** the *Service Fabric* request: GET ``http://localhost:19081/Service_1.0/Api/products``

""""

.. [#f1] ":ref:`sf-placeholders`" feature was requested in issue `721`_ and delivered by PR `722`_ as a part of version `13.0.0`_.

.. _ocelot.json: https://github.com/ThreeMammals/Ocelot/blob/main/test/Ocelot.ManualTest/ocelot.json
.. _721: https://github.com/ThreeMammals/Ocelot/issues/721
.. _722: https://github.com/ThreeMammals/Ocelot/pull/722
.. _13.0.0: https://github.com/ThreeMammals/Ocelot/releases/tag/13.0.0
