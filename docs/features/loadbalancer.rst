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

Here is the complete *load balancer* configuration (the schema) of top-level properties.
You do not need to set all of these options, but the ``Type`` option is required.

.. code-block:: json

  "LoadBalancerOptions": {
    "Expiry": 2147483647,
    "Key": "",
    "Type": ""
  }

The actual ``LoadBalancerOptions`` schema with all the properties can be found in the C# `FileLoadBalancerOptions`_ class.

.. _lb-configuration:

Configuration
-------------

The types of *load balancer* available are:

.. list-table::
  :widths: 25 75
  :header-rows: 1

  * - *Type*
    - *Description*
  * - ``CookieStickySessions``
    - This uses a cookie to stick all requests to a specific server. More information can be found in the :ref:`lb-cookiestickysessions-type` section.
  * - ``LeastConnection``
    - This tracks which services are dealing with requests and sends new requests to the service with the fewest ("least") existing requests. The algorithm state is not distributed across a cluster of Ocelots.
  * - ``NoLoadBalancer``
    - This takes the first available service from configuration or service discovery.
  * - ``RoundRobin``
    - This loops through available services and sends requests. The algorithm state is not distributed across a cluster of Ocelots.

You must choose which *load balancer* to use in your configuration.

The following shows how to set up multiple downstream services for a route using `ocelot.json`_ and then select the ``LeastConnection`` *load balancer*.
This is the simplest way to set up load balancing.

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
      "Type": "LeastConnection"
    }
  }

Service Discovery [#f1]_
------------------------

The following shows how to set up a route using :doc:`../features/servicediscovery` and then select the ``LeastConnection`` *load balancer*.

.. code-block:: json

  {
    // ...
    "ServiceName": "product",
    "LoadBalancerOptions": {
      "Type": "LeastConnection"
    }
  }

When this is set up, Ocelot will look up the downstream host and port from the :doc:`../features/servicediscovery` provider and load balance requests across any available services.
If you add and remove services from the :doc:`../features/servicediscovery` provider [#f1]_,
Ocelot should respect this and stop calling services that have been removed and start calling services that have been added.

.. _lb-cookiestickysessions-type:

``CookieStickySessions`` Type [#f2]_
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
      "Key": "ASP.NET_SessionId",
      "Expiry": 1800000 // milliseconds
    }
  }

The ``LoadBalancerOptions`` are:

.. list-table::
  :widths: 15 85
  :header-rows: 1

  * - *Option*
    - *Description*
  * - ``Type``
    - This needs to be ``CookieStickySessions``.
  * - ``Key``
    - This is the key of the cookie you wish to use for the sticky sessions.
  * - ``Expiry``
    - This is how long, in milliseconds, you want the session to be stuck for. Remember, this refreshes on every request, which is meant to mimic how sessions usually work.

.. _break: http://break.do

  **Note 1**: If you have multiple routes with the same ``LoadBalancerOptions``, then all of those routes will use the same *load balancer* for their subsequent requests.
  This means the sessions will be stuck across routes.

  **Note 2**: If you define more than one ``DownstreamHostAndPort``, or if you are using a :doc:`../features/servicediscovery` provider such as :ref:`sd-consul` and it returns more than one service, then ``CookieStickySessions`` uses ``RoundRobin`` to select the next server.
  This is hard-coded at the moment but could be changed.

.. _lb-custom-balancers:

Custom Balancers [#f3]_
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

1. We have used the most complex example below to show all of the data and types that can be passed into the factory that creates *load balancers*.

.. code-block:: csharp

    using Ocelot.Configuration;
    using Ocelot.DependencyInjection;
    using Ocelot.ServiceDiscovery.Providers;

    Func<IServiceProvider, DownstreamRoute, IServiceDiscoveryProvider, MyLoadBalancer> lbFactory
        = (serviceProvider, Route, discoveryProvider) => new MyLoadBalancer(discoveryProvider.GetAsync);
    builder.Services
        .AddOcelot(builder.Configuration)
        .AddCustomLoadBalancer(lbFactory);

2. However, there is a much simpler example that will work the same way:

.. code-block:: csharp

  using Ocelot.DependencyInjection;

  builder.Services
      .AddOcelot(builder.Configuration)
      .AddCustomLoadBalancer<MyLoadBalancer>();

Notes
-----

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

3. Remember, if you specify no *load balancer* in your :ref:`lb-configuration`, Ocelot will not attempt to load balance.

""""

.. [#f1] Currently supported :doc:`../features/servicediscovery` providers are :ref:`sd-consul`, :doc:`../features/kubernetes`, :ref:`sd-eureka`, :doc:`../features/servicefabric`, and manually developed :ref:`sd-custom-providers`.
.. [#f2] The ":ref:`lb-cookiestickysessions-type`" feature was requested in issue `322`_, though what the user wants is more complicated than just sticky sessions. Anyway, we thought this would be a nice feature to have! Initially, the feature was released in version `6.0.0`_.
.. [#f3] The ":ref:`lb-custom-balancers`" feature by `David Lievrouw`_ implemented a way to provide Ocelot with a custom *load balancer* in PR `1155`_ (his issue `961`_, released in version `15.0.3`_).

.. _322: https://github.com/ThreeMammals/Ocelot/issues/322
.. _961: https://github.com/ThreeMammals/Ocelot/issues/961
.. _1155: https://github.com/ThreeMammals/Ocelot/pull/1155
.. _6.0.0: https://github.com/ThreeMammals/Ocelot/releases/tag/6.0.0
.. _15.0.3: https://github.com/ThreeMammals/Ocelot/releases/tag/15.0.3
.. _David Lievrouw: https://github.com/DavidLievrouw
.. _500 Internal Server Error: https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/500
