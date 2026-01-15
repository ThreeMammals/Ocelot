.. _ocelot.json: https://github.com/ThreeMammals/Ocelot/blob/main/samples/Basic/ocelot.json
.. _Program: https://github.com/ThreeMammals/Ocelot/blob/main/samples/Basic/Program.cs

Load Balancer
=============

Ocelot can load balance across available downstream services for each route.
This means you can scale your downstream services, and Ocelot can use them effectively.

``LoadBalancerOptions`` Schema
------------------------------

.. _FileLoadBalancerOptions: https://github.com/ThreeMammals/Ocelot/blob/main/src/Ocelot/Configuration/File/FileLoadBalancerOptions.cs

  Class: `FileLoadBalancerOptions`_

The following is the full *load balancer* configuration, used in both the :ref:`config-route-schema` and the :ref:`config-dynamic-route-schema`.
Not all of these options need to be configured; however, the ``Type`` option is mandatory.

.. code-block:: json

  "LoadBalancerOptions": {
    "Type": "",
    "Key": "", // CookieStickySessions balancer
    "Expiry": 1 // ms, CookieStickySessions balancer
  }

.. list-table::
  :widths: 15 85
  :header-rows: 1

  * - *Option*
    - *Description*
  * - ``Type``
    - An in-built *load balancer* type selected from the list of available :ref:`lb-balancers`, or a user-defined type (refer to the ":ref:`Custom Balancers <lb-custom-balancers>`" section).
  * - ``Key``
    - The name of the cookie you wish to use for sticky sessions. This option is applicable only to the :ref:`CookieStickySessions type <lb-cookiestickysessions-type>`.
  * - ``Expiry``
    - Expiration period specifies how long, in milliseconds, the session should remain sticky.
      This value refreshes with each request to mimic typical session behavior. Note: This option applies only to the :ref:`CookieStickySessions type <lb-cookiestickysessions-type>`.

The actual ``LoadBalancerOptions`` schema with all the properties can be found in the C# `FileLoadBalancerOptions`_ class.

.. _lb-configuration:

Configuration
-------------

The following shows how to set up multiple downstream services for a static route using `ocelot.json`_ and then select the ``LeastConnection`` *load balancer*.
This is the simplest way to configure load balancing without using service discovery.

.. code-block:: json
  :emphasize-lines: 10-12

  {
    "UpstreamPathTemplate": "/posts/{postId}",
    "UpstreamHttpMethod": [ "Put", "Delete" ],
    "DownstreamPathTemplate": "/api/posts/{postId}",
    "DownstreamScheme": "https",
    "DownstreamHostAndPorts": [
      { "Host": "10.0.1.10", "Port": 5000 },
      { "Host": "10.0.1.11", "Port": 5000 }
    ],
    "LoadBalancerOptions": {
      "Type": "LeastConnection"
    }
  }

The following shows how to set up a route using :doc:`../features/servicediscovery` and then select the ``RoundRobin`` *load balancer*.

.. code-block:: json

  {
    // ...
    "ServiceName": "product",
    "LoadBalancerOptions": {
      "Type": "RoundRobin"
    }
  }

When this is set up, Ocelot will look up the downstream host and port from the :doc:`../features/servicediscovery` provider and load balance requests across any available services.
If you add and remove services from the :doc:`../features/servicediscovery` provider [#f1]_,
Ocelot should respect this and stop calling services that have been removed and start calling services that have been added.

.. _lb-global-configuration:

Global Configuration [#f2]_
^^^^^^^^^^^^^^^^^^^^^^^^^^^

A complete configuration consists of both route-level and global *load balancing*.
You can configure the following options in the ``GlobalConfiguration`` section of `ocelot.json`_:

.. code-block:: json
  :emphasize-lines: 4-8, 12, 17-20

  "Routes": [
    {
      "Key": "R0", // optional
      "LoadBalancerOptions": {
        "Type": "CookieStickySessions",
        "Key": ".AspNetCore.Session",
        "Expiry": 1200000 // milliseconds, 20 minutes
      }
    },
    {
      "Key": "R1", // this route is part of a group
      "LoadBalancerOptions": {} // optional due to grouping
    }
  ],
  "GlobalConfiguration": {
    "BaseUrl": "https://ocelot.net",
    "LoadBalancerOptions": {
      "RouteKeys": ["R1"], // if undefined or empty array, opts will apply to all routes
      "Type": "LeastConnection"
    }
  }

:doc:`../features/servicediscovery` dynamic routes intentionally override the global :ref:`dynamic routing <sd-dynamic-routing>` configuration:

.. code-block:: json
  :emphasize-lines: 5-7, 16-19

  "DynamicRoutes": [
    {
      "Key": "", // optional
      "ServiceName": "my-service",
      "LoadBalancerOptions": {
        "Type": "LeastConnection" // switch from RoundRobin to LeastConnection
      }
    }
  ],
  "GlobalConfiguration": {
    "BaseUrl": "https://ocelot.net",
    "DownstreamScheme": "http",
    "ServiceDiscoveryProvider": {
      // required section for dynamic routing
    },
    "LoadBalancerOptions": {
      "RouteKeys": [], // no grouping, thus opts apply to all dynamic routes
      "Type": "RoundRobin"
    }
  }

In this configuration, the ``RoundRobin`` balancer is used for all implicit dynamic routes.
However, for the "my-service" service, the load balancer type has been explicitly switched from ``RoundRobin`` to ``LeastConnection``.

.. note::

  1. If the ``RouteKeys`` option is not defined or the array is empty in the global ``LoadBalancerOptions``, the global options will apply to all routes.
  If the array contains route keys, it defines a single group of routes to which the global options apply.
  Routes excluded from this group must specify their own route-level ``LoadBalancerOptions``.

  2. Prior to version `24.1`_, global ``LoadBalancerOptions`` were only accessible in the special :ref:`Dynamic Routing <routing-dynamic>` mode.
  Since version `24.1`_, global configuration has been available for both static and dynamic routes.
  As a team, we would consider the idea of implementing such a global configuration for aggregated routes.
  However, an aggregated route is essentially a combination of static routes.

.. _lb-balancers:

Balancers
---------

The available types of built-in *load balancers* are:

.. list-table::
  :widths: 25 75
  :header-rows: 1

  * - *Type*
    - *Description*
  * - ``CookieStickySessions``
    - This uses a cookie to stick all requests to a specific server. More information can be found in the ":ref:`CookieStickySessions Type<lb-cookiestickysessions-type>`" section.
  * - ``LeastConnection``
    - This tracks which services are dealing with requests and sends new requests to the service with the fewest ("least") existing requests. The algorithm state is not distributed across a cluster of Ocelots.
  * - ``RoundRobin``
    - This loops through available services and sends requests. The algorithm state is not distributed across a cluster of Ocelots.
  * - ``NoLoadBalancer``
    - This takes the first available service from :ref:`configuration <lb-configuration>` or :doc:`../features/servicediscovery` provider.

You must choose which *load balancer* to use in your :ref:`configuration <lb-configuration>`.

.. _lb-cookiestickysessions-type:

``CookieStickySessions`` Type [#f3]_
------------------------------------

We have implemented a basic sticky session type of *load balancer*.
The scenario it is meant to support involves having a number of downstream servers that do not share session state.
If you receive more than one request for one of these servers, it should go to the same server each time; otherwise, the session state might be incorrect for the given user.

In order to set up the ``CookieStickySessions`` *load balancer*, you need to do something like the following:

.. code-block:: json

  {
    "UpstreamPathTemplate": "/posts/{postId}",
    "UpstreamHttpMethod": [ "Put", "Delete" ],
    "DownstreamPathTemplate": "/api/posts/{postId}",
    "DownstreamScheme": "https",
    "DownstreamHostAndPorts": [
      { "Host": "10.0.1.10", "Port": 5000 },
      { "Host": "10.0.1.11", "Port": 5000 }
    ],
    "LoadBalancerOptions": {
      "Type": "CookieStickySessions",
      "Key": ".AspNetCore.Session",
      "Expiry": 1200000 // milliseconds, 20 minutes
    }
  }

These ``LoadBalancerOptions`` configure the ``CookieStickySessions`` load balancer using the standard session cookie ``Key`` for ASP.NET Core apps with sessions enabled.
The default expiration time is 20 minutes, matching the default session timeout in ASP.NET Core.

  **Note 1**: If you have multiple routes with the same ``LoadBalancerOptions``, then all of those routes will use the same *load balancer* for their subsequent requests.
  This means the sessions will be stuck across routes.

  **Note 2**: If you define more than one ``DownstreamHostAndPort``, or if you are using a :doc:`../features/servicediscovery` provider such as :ref:`sd-consul` and it returns more than one service, then ``CookieStickySessions`` uses ``RoundRobin`` to select the next server.
  This is hard-coded at the moment but could be changed.

.. _lb-custom-balancers:

Custom Balancers [#f4]_
-----------------------

In order to create and use a custom *load balancer*, you can do the following.
Below, we set up a basic load balancing configuration, and note that the ``Type`` is ``MyLoadBalancer``, which is the name of a class we will set up to perform load balancing.

.. code-block:: json

  {
    // ...
    "DownstreamHostAndPorts": [
      { "Host": "10.0.1.10", "Port": 5000 },
      { "Host": "10.0.1.11", "Port": 5000 }
    ],
    "LoadBalancerOptions": {
      "Type": "MyLoadBalancer"
    }
  }

Then, you need to create a class that implements the ``ILoadBalancer`` interface. Below is a simple round-robin example:

.. code-block:: csharp

  using Ocelot.LoadBalancer.LoadBalancers;
  using Ocelot.Responses;
  using Ocelot.Values;

  public class MyLoadBalancer : ILoadBalancer
  {
      private readonly Func<Task<List<Service>>> _services;
      private static object Locker = new();
      private int _last;

      public MyLoadBalancer() { }
      public MyLoadBalancer(Func<Task<List<Service>>> services)
          => _services = services;

      public string Type => nameof(MyLoadBalancer);
      public void Release(ServiceHostAndPort hostAndPort) { }

      public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext context)
      {
          var services = await _services.Invoke();
          lock (Locker)
          {
              _last = (_last >= services.Count) ? 0 : _last;
              var next = services[_last++];
              return new OkResponse<ServiceHostAndPort>(next.HostAndPort);
          }
      }
  }

Finally, you need to register this class with Ocelot.
We have used the most complex example below to show all of the data and types that can be passed into the factory that creates *load balancers*.

.. code-block:: csharp

    using Ocelot.Configuration;
    using Ocelot.DependencyInjection;
    using Ocelot.ServiceDiscovery.Providers;

    Func<IServiceProvider, DownstreamRoute, IServiceDiscoveryProvider, MyLoadBalancer> lbFactory
        = (serviceProvider, Route, discoveryProvider) => new MyLoadBalancer(discoveryProvider.GetAsync);
    builder.Services
        .AddOcelot(builder.Configuration)
        .AddCustomLoadBalancer(lbFactory);

However, there is a much simpler example that will work the same way:

.. code-block:: csharp

  using Ocelot.DependencyInjection;

  builder.Services
      .AddOcelot(builder.Configuration)
      .AddCustomLoadBalancer<MyLoadBalancer>();

.. note::

  1. There are numerous ``IOcelotBuilder`` `methods <https://github.com/search?q=repo%3AThreeMammals%2FOcelot+%22IOcelotBuilder+AddCustomLoadBalancer%3CT%3E%28%22+language%3AC%23&type=code>`_ to add a custom *load balancer*.
  The interface is as follows:

  .. code-block:: csharp

      IOcelotBuilder AddCustomLoadBalancer<T>()
          where T : ILoadBalancer, new();
      IOcelotBuilder AddCustomLoadBalancer<T>(Func<T> loadBalancerFactoryFunc)
          where T : ILoadBalancer;
      IOcelotBuilder AddCustomLoadBalancer<T>(Func<IServiceProvider, T> loadBalancerFactoryFunc)
          where T : ILoadBalancer;
      IOcelotBuilder AddCustomLoadBalancer<T>(Func<DownstreamRoute, IServiceDiscoveryProvider, T> loadBalancerFactoryFunc)
          where T : ILoadBalancer;
      IOcelotBuilder AddCustomLoadBalancer<T>(Func<IServiceProvider, DownstreamRoute, IServiceDiscoveryProvider, T> loadBalancerFactoryFunc)
          where T : ILoadBalancer;

  2. When you enable custom *load balancers*, Ocelot looks up your *load balancer* by its class name when it decides whether to perform load balancing.

  * If it finds a match, it will use your load balancer to load balance.
  * If Ocelot cannot match the *load balancer* type in your configuration with the name of the registered *load balancer* class, then you will receive an HTTP `500 Internal Server Error`_.
  * If your *load balancer* factory throws an exception when Ocelot calls it, you will receive an HTTP `500 Internal Server Error`_.

.. warning::

  Remember, if you specify no *load balancer* in your :ref:`lb-configuration`, Ocelot will not attempt to load balance.

""""

.. [#f1] Currently supported :doc:`../features/servicediscovery` providers are :ref:`sd-consul`, :doc:`Kubernetes <../features/kubernetes>`, :ref:`Eureka <sd-eureka>`, :doc:`../features/servicefabric`, and manually developed :ref:`sd-custom-providers`.
.. [#f2] The ":ref:`Global Configuration <lb-global-configuration>`" feature, as part of issue `585`_, was introduced in pull request `2324`_ and released in version `24.1`_.
.. [#f3] The ":ref:`CookieStickySessions Type <lb-cookiestickysessions-type>`" feature was requested in issue `322`_, though what the user wants is more complicated than just sticky sessions. Anyway, we thought this would be a nice feature to have! Initially, the feature was released in version `6.0.0`_.
.. [#f4] The ":ref:`Custom Balancers <lb-custom-balancers>`" feature by `David Lievrouw`_ implemented a way to provide Ocelot with a custom *load balancer* in pull request `1155`_ (issue `961`_, released in version `15.0.3`_).

.. _322: https://github.com/ThreeMammals/Ocelot/issues/322
.. _585: https://github.com/ThreeMammals/Ocelot/issues/585
.. _961: https://github.com/ThreeMammals/Ocelot/issues/961
.. _1155: https://github.com/ThreeMammals/Ocelot/pull/1155
.. _2324: https://github.com/ThreeMammals/Ocelot/pull/2324
.. _6.0.0: https://github.com/ThreeMammals/Ocelot/releases/tag/6.0.0
.. _15.0.3: https://github.com/ThreeMammals/Ocelot/releases/tag/15.0.3
.. _24.1: https://github.com/ThreeMammals/Ocelot/releases/tag/24.1.0
.. _David Lievrouw: https://github.com/DavidLievrouw
.. _500 Internal Server Error: https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/500
