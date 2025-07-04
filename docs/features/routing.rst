Routing
=======

Ocelot's primary function is to handle incoming HTTP requests and forward them to a downstream service.
Currently, Ocelot supports this only through HTTP requests. In the future, it might support other transport mechanisms.

Ocelot defines the process of routing one request to another as a "Route".
To make Ocelot functional, you must set up a *route* in its configuration.

.. code-block:: json

  {
    "Routes": []
  }

To configure a *route*, you need to add one to the ``Routes`` JSON array.

.. code-block:: json

  {
    "UpstreamHttpMethod": [ "Get", "Post" ],
    "UpstreamPathTemplate": "/posts/{postId}",
    "DownstreamPathTemplate": "/api/posts/{postId}",
    "DownstreamScheme": "https",
    "DownstreamHostAndPorts": [
      { "Host": "localhost", "Port": 80 }
    ]
  }

The ``DownstreamPathTemplate``, ``DownstreamScheme``, and ``DownstreamHostAndPorts`` properties define the URL to which a request will be forwarded.

The ``DownstreamHostAndPorts`` property is a collection that specifies the host and port of downstream services to which you intend to forward requests.
Typically, it contains a single entry; however, in cases where *load balancing* is required, Ocelot allows you to add multiple entries and select an appropriate :doc:`../features/loadbalancer`.

The ``UpstreamPathTemplate`` property specifies the URL that Ocelot uses to determine the appropriate ``DownstreamPathTemplate`` for a given request.
The ``UpstreamHttpMethod`` property enables Ocelot to differentiate between requests with different HTTP verbs directed to the same URL.
You can either specify a particular list of HTTP methods or leave the list empty to allow all methods.

  **Note**: The complete schema on a single *route* object is described in the :ref:`config-route-schema` section of the :doc:`../features/configuration` feature.

.. _routing-placeholders:

Placeholders
------------

In Ocelot, you can add placeholders for variables to your templates using the format of ``{something}``.
The placeholder variable must be included in both the ``DownstreamPathTemplate`` and ``UpstreamPathTemplate`` properties.
When present, Ocelot attempts to substitute the value of the placeholder from the ``UpstreamPathTemplate`` into the ``DownstreamPathTemplate`` for each request it processes.

You can also do a :ref:`routing-catch-all` type of *route* e.g. 

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

This will forward all path and query string combinations to the downstream service, appending them after the ``/api`` path.

  **Note**: The default routing configuration is **case-insensitive**.
  To change this, you can specify the following setting on a per-route basis:

  .. code-block:: json

    "RouteIsCaseSensitive": true

  This means that when Ocelot attempts to match an incoming upstream URL with an upstream template, the evaluation will be *case-sensitive*.

.. _routing-embedded-placeholders:

Embedded Placeholders [#f1]_
^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Before version `23.4`_, Ocelot could not evaluate multiple placeholders embedded between two forward slashes (``/``).
Additionally, it faced difficulties distinguishing placeholders from other elements within the slashes.
For example, when the pattern ``/{url}-2/`` was applied to ``/y-2/``, it would produce ``{url}`` with ``y-2`` value.

We have introduced an improved method for placeholder evaluation, making it easier to identify placeholders in complex URLs.

**Example**:

* **Path Pattern**: ``/api/invoices_{url0}/{url1}-{url2}_abcd/{url3}?urlId={url4}``
* **Upstream URL Path**: ``/api/invoices_super/123-456_abcd/789?urlId=987``
* **Resulting Placeholders**:

  - ``{url0}`` = ``super``
  - ``{url1}`` = ``123``
  - ``{url2}`` = ``456``
  - ``{url3}`` = ``789``
  - ``{url4}`` = ``987``

.. _break: http://break.do

  **Note**, we believe this feature should be compatible with any URL query strings, although it has not been thoroughly tested.

.. _routing-empty-placeholders:

Empty Placeholders [#f2]_
^^^^^^^^^^^^^^^^^^^^^^^^^

This represents a special edge case of :ref:`routing-placeholders`, in which the value of the placeholder is simply an empty string (``""``).

For example, given the following *route* configuration:

.. code-block:: json

  {
    "UpstreamPathTemplate": "/invoices/{url}",
    "DownstreamPathTemplate": "/api/invoices/{url}",
  }

.. role::  htm(raw)
    :format: html

This route works correctly when ``{url}`` is specified. For instance:

* ``/invoices/123``  :htm:`&rarr;`  ``/api/invoices/123``

**Edge Cases with Empty Placeholder Values**:

1. **Empty Placeholder**: When ``{url}`` is empty, the upstream path ``/invoices/`` routes to the downstream path ``/api/invoices/``.
2. **Omitting the Last Slash**: When the trailing slash is omitted, the upstream path ``/invoices`` should still route to the downstream path ``/api/invoices``.
   This behavior aligns intuitively with user expectations.

.. _routing-catch-all:

Catch All
---------

Ocelot's *routing* supports a *"Catch All"* style, allowing users to specify routes that match all incoming traffic.

If you configure your settings as shown below, all requests will be proxied directly.
The placeholder ``{catchAll}`` is not significant, and any name can be used.

.. code-block:: json

  {
    "UpstreamPathTemplate": "/{catchAll}",
    "DownstreamPathTemplate": "/{catchAll}",
    // ...
  }

The *"Catch All"* route has a lower :ref:`routing-priority` than other routes.
If the following route is included in your configuration, Ocelot will match it before the *"Catch All"* route.

.. code-block:: json

  {
    "UpstreamPathTemplate": "/",
    "DownstreamPathTemplate": "/",
    // ...
  }

.. _routing-priority:

Priority [#f3]_
---------------

You can define the order in which your *routes* match the upstream URL by including a ``Priority`` property in the `ocelot.json`_ file.

.. code-block:: json

  {
    "Priority": 0
  }

Priority **0** is the lowest *priority*.
Ocelot always assigns ``0`` to :ref:`routing-catch-all` routes, and this value is still hardcoded.
Beyond that, you are free to assign any *priority* you wish.

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

In the example above, if a request is made to Ocelot on ``/goods/delete``, it will match the ``/goods/delete`` route.
Previously, it would have matched ``/goods/{catchAll}``, as this was the first *route* in the list.

Query Placeholders
------------------

In addition to URL path :ref:`routing-placeholders`, Ocelot can forward query string parameters, processing them in the form of ``{something}``.
The query parameter placeholder must be included in both the ``DownstreamPathTemplate`` and ``UpstreamPathTemplate`` properties.
Placeholder replacement works bi-directionally between paths and query strings, although it is subject to certain restrictions (see :ref:`routing-merging-of-query-parameters`).

Path to Query String direction
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Ocelot allows you to include a query string as part of the ``DownstreamPathTemplate``, as demonstrated in the following example:

.. code-block:: json

  {
    "UpstreamPathTemplate": "/api/units/{subscription}/{unit}/updates",
    "DownstreamPathTemplate": "/api/subscriptions/{subscription}/updates?unitId={unit}",
  }

In this example, Ocelot uses the value of the ``{unit}`` placeholder from the upstream path template and includes it in the downstream request as a query string parameter named ``unitId``.

  **Note**: Ensure that the placeholder is named differently to account for the :ref:`routing-merging-of-query-parameters`.

Query String to Path direction
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Ocelot also allows you to include query string parameters in the ``UpstreamPathTemplate``, enabling you to match specific queries to corresponding services:

.. code-block:: json

  {
    "UpstreamPathTemplate": "/api/subscriptions/{subscriptionId}/updates?unitId={uid}",
    "DownstreamPathTemplate": "/api/units/{subscriptionId}/{uid}/updates",
  }

In this example, Ocelot matches only requests with a corresponding URL path where the query string begins with ``unitId=something``.
Additional queries are permitted but must follow the matching parameter.
Additionally, Ocelot replaces the ``{uid}`` parameter in the query string and incorporates it into the downstream request path.

  **Note**: The best practice is to use a placeholder name that differs from the name of the query parameter to accommodate the :ref:`routing-merging-of-query-parameters`.

.. _routing-catch-all-query-string:

Catch All Query String
^^^^^^^^^^^^^^^^^^^^^^

Ocelot's *routing* also supports a ":ref:`routing-catch-all`" style, allowing all query string parameters to be forwarded.
The placeholder ``{query}`` is not significant, and any name can be used.

.. code-block:: json

  {
    "UpstreamPathTemplate": "/contracts?{query}",
    "DownstreamPathTemplate": "/apipath/contracts?{query}",
  }

This query string routing feature is particularly useful in scenarios where the query string needs to be routed without any transformations—for example, OData filters (see issue `1174`_).

  **Note**: The ``{query}`` placeholder can remain empty while catching all query strings, as this functionality is part of the ":ref:`routing-empty-placeholders`" feature [#f2]_.
  Consequently, upstream paths ``/contracts?`` and ``/contracts`` are routed to the downstream path ``/apipath/contracts``, with no query string attached.

.. _routing-merging-of-query-parameters:

Merging of Query Parameters
^^^^^^^^^^^^^^^^^^^^^^^^^^^

Query string parameters are unsorted and merged to form the final downstream URL.
This process is crucial because the ``DownstreamUrlCreatorMiddleware`` requires control over placeholder replacement and the merging of duplicate parameters.
A parameter that appears first in the ``UpstreamPathTemplate`` may occupy a different position in the final downstream URL.
Moreover, if the ``DownstreamPathTemplate`` includes query parameters at the beginning, their positions in the ``UpstreamPathTemplate`` will remain undefined unless explicitly specified.

In a typical scenario, the merging algorithm constructs the final downstream URL query string as follows:

1. It begins by taking the initially defined query parameters in the ``DownstreamPathTemplate`` and placing them at the start, along with any necessary placeholder replacements.
2. Next, it adds all parameters from the :ref:`routing-catch-all-query-string`, represented by the placeholder ``{query}``, in the second position—following the explicitly defined parameters from *step 1*.
3. Finally, it appends any remaining replaced placeholder values as parameter values to the end of the query string, if present.

Array parameters in ASP.NET API's model binding
"""""""""""""""""""""""""""""""""""""""""""""""

Due to the merging of parameters, ASP.NET API's special `model binding`_ for arrays does not support the array item representation format ``selectedCourses=1050&selectedCourses=2000``.
This query string will be merged into ``selectedCourses=1050`` in the downstream URL, leading to the loss of array data.
It is essential for upstream clients to generate the correct query string for array models, such as ``selectedCourses[0]=1050&selectedCourses[1]=2000``.
For a detailed explanation of array model binding, refer to the documentation: "`Bind arrays and string values from headers and query strings`_".

Control over parameter existence
""""""""""""""""""""""""""""""""

Be aware that query string placeholders are subject to naming restrictions due to the implementation of the ``DownstreamUrlCreatorMiddleware`` merging algorithm.
Nevertheless, this restriction also offers flexibility in managing the presence of parameters in the final downstream URL based on their names.

Consider the following two development scenarios :htm:`&rarr;`

1. A developer wishes **to preserve a parameter** after substituting a placeholder (refer to issue `473`_).
   This requires the use of the following template definition:

   .. code-block:: json
  
     {
       "UpstreamPathTemplate": "/path/{serverId}/{action}",
       "DownstreamPathTemplate": "/path2/{action}?server={serverId}"
     }

   | In this case, the ``{serverId}`` placeholder and the server parameter **names differ**. As a result, the ``server`` parameter is retained.
   | It is important to note that, due to the case-sensitive comparison of names, the ``server`` parameter will not be preserved with the ``{server}`` placeholder. However, using the ``{Server}`` placeholder is acceptable for retaining the parameter.

2. The developer intends **to remove an outdated parameter** after substituting a placeholder (refer to issue `952`_).
   To achieve this, identical names must be used, adhering to case-sensitive comparison rules.

   .. code-block:: json
  
     {
       "UpstreamPathTemplate": "/users?userId={userId}",
       "DownstreamPathTemplate": "/persons?personId={userId}"
     }

   | Thus, the ``{userId}`` placeholder and the ``userId`` parameter **have identical names**. As a result, the ``userId`` parameter is eliminated.
   | Be aware that, due to the case-sensitive nature of the comparison, the ``userId`` parameter will not be removed if the ``{userid}`` placeholder is used.

.. _routing-upstream-host:

Upstream Host [#f4]_
--------------------

This feature allows you to define routes based on the *upstream host*.
It works by examining the ``Host`` header used by the client and incorporating it into the information used to identify a *route*.

In order to use this feature, add the following to your configuration:

.. code-block:: json

  {
    "UpstreamHost": "mydomain.com"
  }

The *route* above will only match requests where the ``Host`` header value is ``mydomain.com``.

If you do not set the ``UpstreamHost`` on a *route*, any ``Host`` header will match it.
As a result, if you have two routes that are identical except for the ``UpstreamHost``, where one is null and the other is set, Ocelot will prioritize the one that is set.

.. _routing-upstream-headers:

Upstream Headers [#f5]_
-----------------------

In addition to routing by ``UpstreamPathTemplate``, you can also define ``UpstreamHeaderTemplates``.
For a *route* to match, all headers specified in this dictionary object must be included in the request headers.

.. code-block:: json
  :emphasize-lines: 3

  {
    "UpstreamPathTemplate": "/",
    "UpstreamHeaderTemplates": { // dictionary
      "country": "uk", // 1st header
      "version": "v1"  // 2nd header
    }
  }

In this scenario, the *route* matches only if a request contains both headers with the specified values.

Header placeholders
^^^^^^^^^^^^^^^^^^^

Let's explore a more interesting scenario where placeholders can be effectively utilized within your ``UpstreamHeaderTemplates``.

Consider the following approach using the special placeholder format ``{header:placeholdername}``:

.. code-block:: json

  {
    // downstream opts...
    "DownstreamPathTemplate": "/{versionnumber}/api", // with placeholder
    // upstream opts...
    "UpstreamHeaderTemplates": {
      "version": "{header:versionnumber}" // 'header:' prefix vs placeholder
    }
  }

In this scenario, the entire value of the request header ``version`` is inserted into the ``DownstreamPathTemplate``.
If needed, a more complex upstream header template can be specified using placeholders such as ``version-{header:version}_country-{header:country}``.

  **Note 1**: Placeholders are not required in the ``DownstreamPathTemplate``. This scenario can be used to enforce a specific header, regardless of its value.

  **Note 2**: Additionally, the ``UpstreamHeaderTemplates`` dictionary options are applicable for :doc:`../features/aggregation` as well.

.. _routing-security-options:

Security Options [#f6]_
-----------------------

Ocelot facilitates the management of multiple patterns for allowed and blocked IPs using the `IPAddressRange <https://github.com/jsakamoto/ipaddressrange>`_ package, which is licensed under the `MPL-2.0 license <https://github.com/jsakamoto/ipaddressrange/blob/master/LICENSE>`_.

This feature is designed to enhance IP management, allowing for the inclusion or exclusion of a broad IP range using CIDR notation or specific IP ranges.
The current managed patterns are as follows:

.. list-table::
    :widths: 35 65
    :header-rows: 1

    * - *IP Rule*
      - *Example*
    * - Single IP
      - ``192.168.1.1``
    * - IP Range
      - ``192.168.1.1-192.168.1.250``
    * - IP Short Range
      - ``192.168.1.1-250``
    * - IP Subnet
      - ``192.168.1.0/255.255.255.0``
    * - CIDR IPv4
      - ``192.168.1.0/24``
    * - CIDR IPv6
      - ``fe80::/10``

Here is a simple example:

.. code-block:: json

  {
    "SecurityOptions": { 
      "IPBlockedList": [ "192.168.0.0/23" ], 
      "IPAllowedList": ["192.168.0.15", "192.168.1.15"], 
      "ExcludeAllowedFromBlocked": true 
    }
  }

Please **note**:

* The allowed/blocked lists are evaluated during configuration loading.
* The ``ExcludeAllowedFromBlocked`` property enables specifying a wide range of blocked IP addresses while allowing a subrange of IP addresses. Default value: ``false``.
* The absence of a property in *Security Options* is permitted, as it takes the default value.
* *Security Options* can be configured *globally* in the ``GlobalConfiguration`` JSON [#f7]_. However, they are ignored if overriding options are specified at the route level.

.. _routing-dynamic:

Dynamic Routing [#f8]_
----------------------

The concept of dynamic *routing* allows you to use a :doc:`../features/servicediscovery` provider, eliminating the need to manually configure *route* settings.
For more details, refer to the :ref:`sd-dynamic-routing` documentation if this feature interests you.

Errors and Gotchas
------------------
.. _Show and tell: https://github.com/ThreeMammals/Ocelot/discussions/categories/show-and-tell
.. _499 (Client Closed Request): https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.statuscodes.status499clientclosedrequest
.. _503 (Service Unavailable): https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Status/503

In this section, Ocelot team has gathered user scenarios where routing behavior was unclear or errors appeared in the logs.
Please note that the failed routing cases listed below do not represent all application configurations.
If your case is not included, feel free to open a "`Show and tell`_" discussion.

* **Magic 499 status**.
  According to Ocelot Core's design, HTTP status code `499 (Client Closed Request)`_ is returned in cases involving an ``OperationCanceledException``.
  Please note that due to extensive warning-level logging, you may encounter spikes in ``499`` responses—as discussed in thread `2072`_.
  This status is typically caused by:

  A) Forced cancellation of the request by the client
  B) Browser events such as page refreshes or closures while the downstream request is still in progress

  As a quick recipe, the Ocelot team recommends ensuring client stability and, if necessary, adjusting the :ref:`config-timeout` strategy:
  either increasing or decreasing the route :ref:`config-timeout` depending on your usage scenario and the behavior of the downstream service.

* **Timeout errors aka 503 status**.
  According to Ocelot Core's design, HTTP status code `503 (Service Unavailable)`_ is returned in cases involving a ``TimeoutException``.
  This status is typically caused by:

  A) Slow downstream services that may fail to respond
  B) Large requests forwarded to slow downstream services

  As a quick recipe, the Ocelot team recommends increasing the route :ref:`config-timeout` in your configuration.
  This adjustment can help resolve timeout-related issues with sluggish downstream services, ultimately reducing occurrences of `503 (Service Unavailable)`_.

.. _break: http://break.do

  **Note**: For comprehensive documentation regarding errors and status codes in Ocelot, please refer to the :doc:`../features/errorcodes` chapter.

""""

.. [#f1] ":ref:`routing-embedded-placeholders`" feature was requested as part of issue `2199`_ , and released in version `23.4`_
.. [#f2] ":ref:`routing-empty-placeholders`" feature is available starting in version `23.0`_, see issue `748`_ and the `23.0`_ release notes for details.
.. [#f3] ":ref:`routing-priority`" feature was requested as part of issue `270`_, and released in version `5.0.1`_
.. [#f4] ":ref:`routing-upstream-host`" feature was requested as part of issue `209`_ (PR `216`_), and released in version `3.0.1`_
.. [#f5] ":ref:`routing-upstream-headers`" feature was proposed in issue `360`_ (PR `1312`_), and released in version `23.3`_.
.. [#f6] ":ref:`routing-security-options`" feature was requested as part of issue `628`_ (version `12.0.1`_), then redesigned and improved by issue `1400`_ (version `23.4.1`_), and published in version `20.0`_ docs.
.. [#f7] Global ":ref:`routing-security-options`" feature was requested as part of issue `2165`_ , and released in version `23.4.1`_.
.. [#f8] ":ref:`routing-dynamic`" feature was requested as part of issue `340`_, and released in version `7.0.1`_. Refer to complete reference: :ref:`sd-dynamic-routing`.

.. _model binding: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-8.0#collections
.. _Bind arrays and string values from headers and query strings: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-8.0#bind-arrays-and-string-values-from-headers-and-query-strings
.. _ocelot.json: https://github.com/ThreeMammals/Ocelot/blob/main/samples/Basic/ocelot.json

.. _209: https://github.com/ThreeMammals/Ocelot/issues/209
.. _216: https://github.com/ThreeMammals/Ocelot/pull/216
.. _270: https://github.com/ThreeMammals/Ocelot/issues/270
.. _340: https://github.com/ThreeMammals/Ocelot/issues/340
.. _360: https://github.com/ThreeMammals/Ocelot/issues/360
.. _473: https://github.com/ThreeMammals/Ocelot/issues/473
.. _628: https://github.com/ThreeMammals/Ocelot/issues/628
.. _748: https://github.com/ThreeMammals/Ocelot/issues/748
.. _952: https://github.com/ThreeMammals/Ocelot/issues/952
.. _1174: https://github.com/ThreeMammals/Ocelot/issues/1174
.. _1312: https://github.com/ThreeMammals/Ocelot/pull/1312
.. _1400: https://github.com/ThreeMammals/Ocelot/issues/1400
.. _2072: https://github.com/ThreeMammals/Ocelot/discussions/2072
.. _2165: https://github.com/ThreeMammals/Ocelot/issues/2165
.. _2199: https://github.com/ThreeMammals/Ocelot/issues/2199

.. _3.0.1: https://github.com/ThreeMammals/Ocelot/releases/tag/3.0.1
.. _5.0.1: https://github.com/ThreeMammals/Ocelot/releases/tag/5.0.1
.. _7.0.1: https://github.com/ThreeMammals/Ocelot/releases/tag/7.0.1
.. _12.0.1: https://github.com/ThreeMammals/Ocelot/releases/tag/12.0.1
.. _20.0: https://github.com/ThreeMammals/Ocelot/releases/tag/20.0.0
.. _23.0: https://github.com/ThreeMammals/Ocelot/releases/tag/23.0.0
.. _23.3: https://github.com/ThreeMammals/Ocelot/releases/tag/23.3.0
.. _23.4: https://github.com/ThreeMammals/Ocelot/releases/tag/23.4.0
.. _23.4.1: https://github.com/ThreeMammals/Ocelot/releases/tag/23.4.1
