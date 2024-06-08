Routing
=======

Ocelot's primary functionality is to take incoming HTTP requests and forward them on to a downstream service.
Ocelot currently only supports this in the form of another HTTP request (in the future this could be any transport mechanism).

Ocelot describes the routing of one request to another as a Route.
In order to get anything working in Ocelot you need to set up a Route in the configuration.

.. code-block:: json

  {
    "Routes": []
  }

To configure a Route you need to add one to the Routes JSON array.

.. code-block:: json

  {
    "UpstreamHttpMethod": [ "Put", "Delete" ],
    "UpstreamPathTemplate": "/posts/{postId}",
    "DownstreamPathTemplate": "/api/posts/{postId}",
    "DownstreamScheme": "https",
    "DownstreamHostAndPorts": [
      { "Host": "localhost", "Port": 80 }
    ]
  }

The **DownstreamPathTemplate**, **DownstreamScheme** and **DownstreamHostAndPorts** define the URL that a request will be forwarded to. 

The **DownstreamHostAndPorts** property is a collection that defines the host and port of any downstream services that you wish to forward requests to.
Usually this will just contain a single entry, but sometimes you might want to load balance requests to your downstream services and Ocelot allows you add more than one entry and then select a load balancer.

The **UpstreamPathTemplate** property is the URL that Ocelot will use to identify which **DownstreamPathTemplate** to use for a given request.
The **UpstreamHttpMethod** is used so Ocelot can distinguish between requests with different HTTP verbs to the same URL.
You can set a specific list of HTTP methods or set an empty list to allow any of them. 

.. _routing-placeholders:

Placeholders
------------

In Ocelot you can add placeholders for variables to your Templates in the form of ``{something}``.
The placeholder variable needs to be present in both the **DownstreamPathTemplate** and **UpstreamPathTemplate** properties.
When it is Ocelot will attempt to substitute the value in the **UpstreamPathTemplate** placeholder into the **DownstreamPathTemplate** for each request Ocelot processes.

You can also do a :ref:`routing-catch-all` type of Route e.g. 

.. code-block:: json

  {
    "UpstreamHttpMethod": [ "Get", "Post" ],
    "UpstreamPathTemplate": "/{everything}",
    "DownstreamPathTemplate": "/api/{everything}",
    "DownstreamScheme": "https",
    "DownstreamHostAndPorts": [
      { "Host": "localhost", "Port": 80 }
    ]
  }

This will forward any path + query string combinations to the downstream service after the path ``/api``.

**Note**, the default Routing configuration is case insensitive!

In order to change this you can specify on a per Route basis the following setting:

.. code-block:: json

  "RouteIsCaseSensitive": true

This means that when Ocelot tries to match the incoming upstream URL with an upstream template the evaluation will be case sensitive. 

.. _routing-empty-placeholders:

Empty Placeholders [#f1]_
^^^^^^^^^^^^^^^^^^^^^^^^^

This is a special edge case of :ref:`routing-placeholders`, where the value of the placeholder is simply an empty string ``""``.

For example, **Given a route**: 

.. code-block:: json

  {
    "UpstreamPathTemplate": "/invoices/{url}",
    "DownstreamPathTemplate": "/api/invoices/{url}",
  }

.. role::  htm(raw)
    :format: html

| **Then**, it works correctly when ``{url}`` is specified: ``/invoices/123`` :htm:`&rarr;` ``/api/invoices/123``.
| **And then**, there are two edge cases with empty placeholder value:

* Also, it works when ``{url}`` is empty. We would expect upstream path ``/invoices/`` to route to downstream path ``/api/invoices/``
* Moreover, it should work when omitting last slash. We also expect upstream ``/invoices`` to be routed to downstream ``/api/invoices``, which is intuitive to humans

.. _routing-catch-all:

Catch All
---------

Ocelot's routing also supports a *Catch All* style routing where the user can specify that they want to match all traffic.

If you set up your config like below, all requests will be proxied straight through.
The placeholder ``{url}`` name is not significant, any name will work.

.. code-block:: json

  {
    "UpstreamHttpMethod": [ "Get" ],
    "UpstreamPathTemplate": "/{url}",
    "DownstreamPathTemplate": "/{url}",
    "DownstreamScheme": "https",
    "DownstreamHostAndPorts": [
      { "Host": "localhost", "Port": 80 }
    ]
  }

The *Catch All* has a lower priority than any other Route.
If you also have the Route below in your config then Ocelot would match it before the *Catch All*. 

.. code-block:: json

  {
    "UpstreamHttpMethod": [ "Get" ],
    "UpstreamPathTemplate": "/",
    "DownstreamPathTemplate": "/",
    "DownstreamScheme": "https",
    "DownstreamHostAndPorts": [
      { "Host": "10.0.10.1", "Port": 80 }
    ]
  }

.. _routing-upstream-host:

Upstream Host [#f2]_
--------------------

This feature allows you to have Routes based on the *upstream host*.
This works by looking at the ``Host`` header the client has used and then using this as part of the information we use to identify a Route.

In order to use this feature please add the following to your config:

.. code-block:: json

  {
    "UpstreamHost": "somedomain.com"
  }

The Route above will only be matched when the ``Host`` header value is ``somedomain.com``.

If you do not set **UpstreamHost** on a Route then any ``Host`` header will match it.
This means that if you have two Routes that are the same, apart from the **UpstreamHost**, where one is null and the other set Ocelot will favour the one that has been set. 

.. _routing-upstream-headers:

Upstream Headers [#f3]_
-----------------------

In addition to routing by ``UpstreamPathTemplate``, you can also define ``UpstreamHeaderTemplates``.
For a route to match, all headers specified in this dictionary object must be present in the request headers.

.. code-block:: json

  {
    // ...
    "UpstreamPathTemplate": "/",
    "UpstreamHttpMethod": [ "Get" ],
    "UpstreamHeaderTemplates": { // dictionary
      "country": "uk", // 1st header
      "version": "v1"  // 2nd header
    }
  }

In this scenario, the route will only match if a request includes both headers with the specified values.

Header placeholders
^^^^^^^^^^^^^^^^^^^

Let's explore a more intriguing scenario where placeholders can be effectively utilized within your ``UpstreamHeaderTemplates``.

Consider the following approach using the special placeholder format ``{header:placeholdername}``:

.. code-block:: json

  {
    "DownstreamPathTemplate": "/{versionnumber}/api", // with placeholder
    "DownstreamScheme": "https",
    "DownstreamHostAndPorts": [
      { "Host": "10.0.10.1", "Port": 80 }
    ],
    "UpstreamPathTemplate": "/api",
    "UpstreamHttpMethod": [ "Get" ],
    "UpstreamHeaderTemplates": {
      "version": "{header:versionnumber}" // 'header:' prefix vs placeholder
    }
  }

In this scenario, the entire value of the request header "**version**" is inserted into the ``DownstreamPathTemplate``.
If necessary, a more intricate upstream header template can be specified, using placeholders such as ``version-{header:version}_country-{header:country}``.

  **Note 1**: Placeholders are not required in ``DownstreamPathTemplate``.
  This scenario can be utilized to mandate a specific header regardless of its value.

  **Note 2**: Additionally, the ``UpstreamHeaderTemplates`` dictionary options are applicable for :doc:`../features/requestaggregation` as well.

Priority
--------

You can define the order you want your Routes to match the Upstream ``HttpRequest`` by including a **Priority** property in **ocelot.json**.
See issue `270`_ for reference.

.. code-block:: json

  {
    "Priority": 0
  }

``0`` is the lowest priority, Ocelot will always use ``0`` for ``/{catchAll}`` Routes and this is still hardcoded.
After that you are free to set any priority you wish.

e.g. you could have

.. code-block:: json

  {
    "UpstreamPathTemplate": "/goods/{catchAll}",
    "Priority": 0
  }

and

.. code-block:: json

  {
    "UpstreamPathTemplate": "/goods/delete",
    "Priority": 1
  }

In the example above if you make a request into Ocelot on ``/goods/delete``, Ocelot will match ``/goods/delete`` Route.
Previously it would have matched ``/goods/{catchAll}``, because this is the first Route in the list!

Query String Placeholders
-------------------------

In addition to URL path :ref:`routing-placeholders` Ocelot is able to forward query string parameters with their processing in the form of ``{something}``.
Also, the query parameter placeholder needs to be present in both the **DownstreamPathTemplate** and **UpstreamPathTemplate** properties.
Placeholder replacement works bi-directionally between path and query strings, with some restrictions on usage (see :ref:`routing-merging-of-query-parameters`).

Path to Query String direction
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Ocelot allows you to specify a query string as part of the **DownstreamPathTemplate** like the example below:

.. code-block:: json

  {
    "UpstreamPathTemplate": "/api/units/{subscription}/{unit}/updates",
    "DownstreamPathTemplate": "/api/subscriptions/{subscription}/updates?unitId={unit}",
  }

In this example Ocelot will use the value from the ``{unit}`` placeholder in the upstream path template and add it to the downstream request as a query string parameter called ``unitId``!

  Note! Make sure you name the placeholder differently due to :ref:`routing-merging-of-query-parameters`.


Query String to Path direction
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Ocelot will also allow you to put query string parameters in the **UpstreamPathTemplate** so you can match certain queries to certain services:

.. code-block:: json

  {
    "UpstreamPathTemplate": "/api/subscriptions/{subscriptionId}/updates?unitId={uid}",
    "DownstreamPathTemplate": "/api/units/{subscriptionId}/{uid}/updates",
  }

In this example Ocelot will only match requests that have a matching URL path and the query string starts with ``unitId=something``.
You can have other queries after this but you must start with the matching parameter.
Also Ocelot will swap the ``{uid}`` parameter from the query string and use it in the downstream request path.

  Note, the best practice is giving different placeholder name than the name of query parameter due to :ref:`routing-merging-of-query-parameters`.

.. _routing-catch-all-query-string:

Catch All Query String
^^^^^^^^^^^^^^^^^^^^^^

Ocelot's routing also supports a :ref:`routing-catch-all` style routing to forward all query string parameters.
The placeholder ``{everything}`` name does not matter, any name will work.

.. code-block:: json

  {
    "UpstreamPathTemplate": "/contracts?{everything}",
    "DownstreamPathTemplate": "/apipath/contracts?{everything}",
  }

This entire query string routing feature is very useful in cases where the query string should not be transformed but rather routed without any changes,
such as OData filters and etc (see issue `1174`_).

  **Note**, the ``{everything}`` placeholder can be empty while catching all query strings, because this is a part of the :ref:`routing-empty-placeholders` feature! [#f1]_
  Thus, upstream paths ``/contracts?`` and ``/contracts`` are routed to downstream path ``/apipath/contracts``, which has no query string at all.

.. _routing-merging-of-query-parameters:

Merging of Query Parameters
^^^^^^^^^^^^^^^^^^^^^^^^^^^

Query string parameters are unsorted and merged to create the final downstream URL.
This process is essential as the ``DownstreamUrlCreatorMiddleware`` requires control over placeholder replacement and the merging of duplicate parameters.
A parameter that appears first in the **UpstreamPathTemplate** may occupy a different position in the final downstream URL.
Furthermore, if the **DownstreamPathTemplate** includes query parameters at the beginning, their position in the **UpstreamPathTemplate** will be indeterminate unless explicitly defined.

In a typical scenario, the merging algorithm constructs the final downstream URL query string by:

1. Taking the initially defined query parameters in **DownstreamPathTemplate** and placing them at the beginning, with any necessary placeholder replacements.
2. Adding all parameters from the :ref:`routing-catch-all-query-string`, represented by the placeholder ``{everything}``, into the second position (following the explicitly defined parameters from **step 1**).
3. Appending any remaining replaced placeholder values as parameter values to the end of the string, if they are present.

Array parameters in ASP.NET API's model binding
"""""""""""""""""""""""""""""""""""""""""""""""

Due to parameters merging, ASP.NET API's special `model binding`_ for arrays **is not supported** having the array item representation format of ``selectedCourses=1050&selectedCourses=2000``.
This query string will be merged into ``selectedCourses=1050`` in the downstream URL, resulting in the loss of array data.
It is crucial for upstream clients to generate the correct query string for array models, such as ``selectedCourses[0]=1050&selectedCourses[1]=2000``.
For a comprehensive understanding of array model bindings, refer to the documentation: `Bind arrays and string values from headers and query strings`_.

Control over parameter existence
""""""""""""""""""""""""""""""""

Be aware that query string placeholders are subject to naming restrictions due to the ``DownstreamUrlCreatorMiddleware``'s merging algorithm implementation.
However, this also provides the flexibility to manage the presence of parameters in the final downstream URL by their names.

Consider the following 2 development scenarios :htm:`&rarr;`

1. A developer wishes **to preserve a parameter** after substituting a placeholder (refer to issue `473`_).
   This requires the use of the template definition below:

   .. code-block:: json
  
     {
       "UpstreamPathTemplate": "/path/{serverId}/{action}",
       "DownstreamPathTemplate": "/path2/{action}?server={serverId}"
     }

   | Here, the ``{serverId}`` placeholder and the ``server`` parameter **names differ**! Ultimately, the ``server`` parameter is retained.
   | It is important to note that due to the case-sensitive comparison of names, the ``server`` parameter will not be preserved with the ``{server}`` placeholder. However, using the ``{Server}`` placeholder is acceptable for retaining the parameter.

2. The developer intends **to remove an outdated parameter** after substituting a placeholder (refer to issue `952`_).
   For this action, you must use identical names having the case-sensitive comparison:

   .. code-block:: json
  
     {
       "UpstreamPathTemplate": "/users?userId={userId}",
       "DownstreamPathTemplate": "/persons?personId={userId}"
     }

   | Thus, the ``{userId}`` placeholder and the ``userId`` parameter **have identical names**! Subsequently, the ``userId`` parameter is eliminated.
   | Be aware that due to the case sensitive nature of the comparison, if the ``{userid}`` placeholder is used, the ``userId`` parameter will not be removed!

.. _routing-security-options:

Security Options [#f4]_
-----------------------

Ocelot allows you to manage multiple patterns for allowed/blocked IPs using the `IPAddressRange <https://github.com/jsakamoto/ipaddressrange>`_ package
with `MPL-2.0 License <https://github.com/jsakamoto/ipaddressrange/blob/master/LICENSE>`_.

This feature is designed to allow greater IP management in order to include or exclude a wide IP range via CIDR notation or IP range.
The current patterns managed are the following:

* Single IP: :code:`192.168.1.1`
* IP Range: :code:`192.168.1.1-192.168.1.250`
* IP Short Range: :code:`192.168.1.1-250`
* IP Range with subnet: :code:`192.168.1.0/255.255.255.0`
* CIDR: :code:`192.168.1.0/24`
* CIDR for IPv6: :code:`fe80::/10`
* The allowed/blocked lists are evaluated during configuration loading
* The **ExcludeAllowedFromBlocked** property is intended to provide the ability to specify a wide range of blocked IP addresses and allow a subrange of IP addresses.
  Default value: :code:`false`
* The absence of a property in **SecurityOptions** is allowed, it takes the default value.

.. code-block:: json

  {
    "SecurityOptions": { 
      "IPBlockedList": [ "192.168.0.0/23" ], 
      "IPAllowedList": ["192.168.0.15", "192.168.1.15"], 
      "ExcludeAllowedFromBlocked": true 
    }
  }

.. _routing-dynamic:

Dynamic Routing [#f5]_
----------------------

The idea is to enable dynamic routing when using a :doc:`../features/servicediscovery` provider so you don't have to provide the Route config.
See the :ref:`sd-dynamic-routing` docs if this sounds interesting to you.

""""

.. [#f1] ":ref:`routing-empty-placeholders`" feature is available starting in version `23.0 <https://github.com/ThreeMammals/Ocelot/releases/tag/23.0.0>`_, see issue `748 <https://github.com/ThreeMammals/Ocelot/issues/748>`_ and the `23.0 <https://github.com/ThreeMammals/Ocelot/releases/tag/23.0.0>`__ release notes for details.
.. [#f2] ":ref:`routing-upstream-host`" feature was requested as part of `issue 216 <https://github.com/ThreeMammals/Ocelot/pull/216>`_.
.. [#f3] ":ref:`routing-upstream-headers`" feature was proposed in `issue 360 <https://github.com/ThreeMammals/Ocelot/issues/360>`_, and released in version `24.0 <https://github.com/ThreeMammals/Ocelot/releases/tag/24.0.0>`_.
.. [#f4] ":ref:`routing-security-options`" feature was requested as part of `issue 628 <https://github.com/ThreeMammals/Ocelot/issues/628>`_ (of `12.0.1 <https://github.com/ThreeMammals/Ocelot/releases/tag/12.0.1>`_ version), then redesigned and improved by `issue 1400 <https://github.com/ThreeMammals/Ocelot/issues/1400>`_, and published in version `20.0 <https://github.com/ThreeMammals/Ocelot/releases/tag/20.0.0>`_ docs.
.. [#f5] ":ref:`routing-dynamic`" feature was requested as part of `issue 340 <https://github.com/ThreeMammals/Ocelot/issues/340>`_. Complete reference: :ref:`sd-dynamic-routing`.

.. _model binding: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-8.0#collections
.. _Bind arrays and string values from headers and query strings: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-8.0#bind-arrays-and-string-values-from-headers-and-query-strings
.. _270: https://github.com/ThreeMammals/Ocelot/issues/270
.. _473: https://github.com/ThreeMammals/Ocelot/issues/473
.. _952: https://github.com/ThreeMammals/Ocelot/issues/952
.. _1174: https://github.com/ThreeMammals/Ocelot/issues/1174
