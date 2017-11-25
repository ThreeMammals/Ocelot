Request Id / Correlation Id
===========================

Ocelot supports a client sending a request id in the form of a header. If set Ocelot will
use the requestid for logging as soon as it becomes available in the middleware pipeline. 
Ocelot will also forward the request id with the specified header to the downstream service.
I'm not sure if have this spot on yet in terms of the pipeline order becasue there are a few logs
that don't get the users request id at the moment and ocelot just logs not set for request id
which sucks. You can still get the framework request id in the logs if you set 
IncludeScopes true in your logging config. This can then be used to match up later logs that do
have an OcelotRequestId.

In order to use the requestid feature in your ReRoute configuration add this setting

.. code-block:: json

    "RequestIdKey": "OcRequestId"

In this example OcRequestId is the request header that contains the clients request id.

There is also a setting in the GlobalConfiguration section which will override whatever has been
set at ReRoute level for the request id. The setting is as fllows.

.. code-block:: json

    "RequestIdKey": "OcRequestId"

It behaves in exactly the same way as the ReRoute level RequestIdKey settings.