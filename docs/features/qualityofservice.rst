.. role:: htm(raw)
  :format: html
.. role:: pdf(raw)
  :format: latex pdflatex
.. _Program: https://github.com/ThreeMammals/Ocelot/blob/main/samples/Basic/Program.cs
.. _Polly: https://www.pollydocs.org
.. _documentation: https://www.pollydocs.org
.. _Resilience strategies: https://www.pollydocs.org/strategies/index.html
.. |QoS_label| image:: https://img.shields.io/badge/-QoS-D3ADAF.svg
  :target: https://github.com/ThreeMammals/Ocelot/labels/QoS
  :alt: label QoS
  :class: img-valign-textbottom

Quality of Service
==================

  Repository Label: |QoS_label|:pdf:`\href{https://github.com/ThreeMammals/Ocelot/labels/QoS}{QoS}`

Ocelot currently supports a single *Quality of Service* (QoS) capability.
It allows you to configure, on a per-route basis, the application of a circuit breaker when making requests to downstream services.
This feature leverages a well-regarded .NET library known as `Polly`_.
For more details, visit the `Polly`_ library's official `repository <https://github.com/App-vNext/Polly>`_.

.. note::
  
  `Polly`_ v7 syntax is no longer supported as of version `23.2`_, when the Ocelot team upgraded Polly `from v7 to v8 <https://www.pollydocs.org/migration-v8.html>`_.

Installation
------------

To utilize the *Quality of Service* via `Polly`_ library, begin by importing the appropriate `Ocelot.Provider.Polly <https://www.nuget.org/packages/Ocelot.Provider.Polly>`_ extension package:

.. code-block:: powershell

    Install-Package Ocelot.Provider.Polly

Next, in your `Program`_, incorporate `Polly`_ services by invoking the ``AddPolly()`` extension on the ``OcelotBuilder``, as shown below [#f1]_:

.. code-block:: csharp
  :emphasize-lines: 5

  using Ocelot.Provider.Polly;

  builder.Services
      .AddOcelot(builder.Configuration)
      .AddPolly();

.. _qos-schema:

``QoSOptions`` Schema
---------------------
.. _MinimumThroughput: https://www.pollydocs.org/api/Polly.CircuitBreaker.CircuitBreakerStrategyOptions-1.html#Polly_CircuitBreaker_CircuitBreakerStrategyOptions_1_MinimumThroughput
.. _BreakDuration: https://www.pollydocs.org/api/Polly.CircuitBreaker.CircuitBreakerStrategyOptions-1.html#Polly_CircuitBreaker_CircuitBreakerStrategyOptions_1_BreakDuration
.. _FailureRatio: https://www.pollydocs.org/api/Polly.CircuitBreaker.CircuitBreakerStrategyOptions-1.html#Polly_CircuitBreaker_CircuitBreakerStrategyOptions_1_FailureRatio
.. _SamplingDuration: https://www.pollydocs.org/api/Polly.CircuitBreaker.CircuitBreakerStrategyOptions-1.html#Polly_CircuitBreaker_CircuitBreakerStrategyOptions_1_SamplingDuration
.. _Timeout: https://www.pollydocs.org/api/Polly.Timeout.TimeoutStrategyOptions.html#Polly_Timeout_TimeoutStrategyOptions_Timeout
.. _FileQoSOptions: https://github.com/ThreeMammals/Ocelot/blob/main/src/Ocelot/Configuration/File/FileQoSOptions.cs

  Class: `FileQoSOptions`_

Here is the complete *Quality of Service* configuration, also known as the "QoS options schema".
Depending on your needs and choosen strategies definition of all properties are not required.
If you skip a property then a default value will be substituted as per Ocelot/Polly specification.

.. code-block:: json

  "QoSOptions": {
    // Circuit Breaker strategy
    "BreakDuration": 0, // integer
    "MinimumThroughput": 0, // integer
    "FailureRatio": 0.0, // floating number
    "SamplingDuration": 0, // integer
    // Timeout strategy
    "Timeout": 0, // integer
    // Deprecated options
    "DurationOfBreak": 0, // deprecated! -> use BreakDuration
    "ExceptionsAllowedBeforeBreaking": 0, // deprecated! -> use MinimumThroughput
    "TimeoutValue": 0, // deprecated! -> use Timeout
  }

.. list-table::
    :widths: 30 70
    :header-rows: 1

    * - *Ocelot Option and Polly equivalent*
      - *Description*
    * - ``BreakDuration`` (formerly ``DurationOfBreak``) as `BreakDuration`_
      - This is duration of break the circuit will stay open before resetting. The unit is milliseconds.
    * - ``MinimumThroughput`` (formerly ``ExceptionsAllowedBeforeBreaking``) as `MinimumThroughput`_, a primary option
      - This number of actions or more must pass through the circuit within the time slice for the statistics to be considered significant and for the circuit breaker to engage
    * - ``FailureRatio`` is `FailureRatio`_
      - This is the failure-to-success ratio at which the circuit will break
    * - ``SamplingDuration`` is `SamplingDuration`_
      - This is the duration of the sampling over which failure ratios are assessed. The unit is milliseconds.
    * - ``Timeout`` (formerly ``TimeoutValue``) as `Timeout`_, a primary option
      - This is the default timeout. The unit is milliseconds.

.. warning::
  The following options are deprecated in version `24.1`_: ``DurationOfBreak``, ``ExceptionsAllowedBeforeBreaking``, and ``TimeoutValue``!
  Use the appropriate new options as shown in the table above.
  These deprecated options will be removed in version `25.0`_.
  For backward compatibility in version `24.1`_, a deprecated option takes precedence over its replacement.

.. _break1: http://break.do

  **Note** [#f2]_: Ocelot checks that the values of options are valid during execution.
  If not, it logs errors or warnings (refer to the :ref:`qos-notes-value-constraints` section in :ref:`qos-notes`).
  For a complete explanation about strategies and mechanisms, consult Polly's `Resilience strategies`_ documentation.

.. _qos-global-configuration:

Global Configuration [#f3]_
---------------------------

According to the :ref:`config-global-configuration-schema`, global *Quality of Service* options for static routes were introduced in version `24.1`_.
These global options can also be overridden in the ``Routes`` configuration section, a capability that has been supported for a long time.

.. code-block:: json
  :emphasize-lines: 5-7, 12, 18-21

  {
    "Routes": [
      {
        "Key": "R0", // optional
        "QoSOptions": {
          "Timeout": 15000 // 15s
        },
        // ...
      },
      {
        "Key": "R1", // this route is part of a group
        "QoSOptions": {}, // optional due to grouping
        // ...
      }
    ],
    "GlobalConfiguration": {
      "BaseUrl": "https://ocelot.net",
      "QoSOptions": {
        "RouteKeys": ["R1",], // if undefined or empty array, opts will apply to all routes
        "BreakDuration": 1000, // 1s
        "MinimumThroughput": 3
      },
      // ...
    }
  }

Dynamic routes were not supported in versions prior to `24.1`_.
However, global *Quality of Service* options have been available in :ref:`Dynamic Routing <routing-dynamic>` mode for a long time.
Starting with version `24.1`_, global *QoS* options can also be overridden in the ``DynamicRoutes`` configuration section, as defined by the :ref:`config-dynamic-route-schema`.

.. code-block:: json
  :emphasize-lines: 6-8, 17-22

  {
    "DynamicRoutes": [
      {
        "Key": "", // optional
        "ServiceName": "my-service",
        "QoSOptions": {
          "Timeout": 15000 // 15s
        },
      }
    ],
    "GlobalConfiguration": {
      "BaseUrl": "https://ocelot.net",
      "DownstreamScheme": "http",
      "ServiceDiscoveryProvider": {
        // required section for dynamic routing
      },
      "QoSOptions": {
        "RouteKeys": [], // or null, no grouping, thus opts apply to all dynamic routes
        "BreakDuration": 1000, // 1s
        "MinimumThroughput": 3,
        "FailureRatio": 0.1, // 10%
        "SamplingDuration": 30000 // 30s
      }
    }
  }

In this dynamic routing configuration, the :ref:`qos-timeout-strategy` is applied to the ``my-service`` service in addition to the :ref:`qos-circuit-breaker-strategy`, resulting in `Polly`_ timing out after 15 seconds.
However, for all implicit dynamic routes, the :ref:`qos-timeout-strategy` is not globally configured, in favor of the standard :ref:`config-timeout` option managed by the Ocelot Core requester middleware.
Lastly, the :ref:`qos-circuit-breaker-strategy` has been globally configured for all routes due to the absence of route grouping, with the following options:
allow 3 errors before breaking the circuit for 1 second, and allow up to 10% errors during the default 30-second sampling period.

.. note::

  1. Please note that
  route-level options take precedence over global options.

  2. If the ``RouteKeys`` option is not defined or the array is empty in the global ``QoSOptions``, the global options will apply to all routes.
  If the array contains route keys, it defines a single group of routes to which the global options apply.
  Routes excluded from this group must specify their own route-level ``QoSOptions``.

  3. Since Ocelot's Polly provider utilizes the `Resilience pipeline registry`_, each route has a dedicated pipeline cached in Polly's registry using the route's load-balancing key.
  For a static route, the load-balancing key uniquely identifies the route by its upstream options, whereas for dynamic routes the load-balancing key is typically the service name from the discovery provider.
  Thus, Polly's registry maintains dedicated pipelines for each discovered service, and those pipelines behave independently.
  Finally, it is important to understand that global *QoS* options do not create a single shared resilience pipeline in the registry.

  4. Dynamic routes were not supported in versions prior to `24.1`_.
  Beginning with version `24.1`_, global *QoS* options for :ref:`Dynamic Routing <routing-dynamic>` may be overridden in the ``DynamicRoutes`` configuration section, as defined by the :ref:`config-dynamic-route-schema`.
  Additionally, global configuration for static routes (also known as ``Routes``) has been supported since version `24.1`_.

.. _Resilience pipeline registry: https://www.pollydocs.org/pipelines/resilience-pipeline-registry.html
.. _qos-circuit-breaker-strategy:

Circuit Breaker strategy
------------------------
.. _Circuit breaker resilience strategy: https://www.pollydocs.org/strategies/circuit-breaker.html

  | Documentation: `Circuit breaker resilience strategy`_
  | Primary option: ``MinimumThroughput``, formerly ``ExceptionsAllowedBeforeBreaking``

The options ``MinimumThroughput`` and ``BreakDuration`` can be configured independently from ``Timeout``:

.. code-block:: json

  "QoSOptions": {
    "MinimumThroughput": 3,
    "BreakDuration": 1000 // ms
  }

Alternatively, you can omit ``BreakDuration``, which will default to the implicit 5-second setting as specified in Polly's `BreakDuration`_ documentation:

.. code-block:: json

  "QoSOptions": {
    "MinimumThroughput": 3
  }

This setup activates only the `Circuit breaker resilience strategy`_.

Additionally, there is a failure handling strategy based on ``FailureRatio``, which serves as a counterpart to, or supplement for, the number of failures, also known as ``MinimumThroughput``.

.. code-block:: json

  "QoSOptions": {
    "MinimumThroughput": 10,
    "FailureRatio": 0.5, // 50%
    "SamplingDuration": 10000, // ms, 10 seconds
  }

Thus, a failure ratio of ``0.5`` indicates that the circuit will break if 50% or more of actions result in handled failures, after reaching the minimum threshold of 10 failures, also known as the ``MinimumThroughput`` option.
Additionally, the 10-second sampling duration defines the time window over which the 50% failure ratio is evaluated.

  **Note**: The ``MinimumThroughput`` option (also known as Polly's `MinimumThroughput`_) is the primary option that enables the *Circuit Breaker strategy*.
  Its value must be valid (set to 2 or greater, refer to the :ref:`qos-notes-value-constraints` section in :ref:`qos-notes`) and may be supplemented with additional Circuit Breaker options.

.. _qos-timeout-strategy:

Timeout strategy
----------------
.. _Timeout resilience strategy: https://www.pollydocs.org/strategies/timeout.html

  | Documentation: `Timeout resilience strategy`_
  | Primary option: ``Timeout``, formerly ``TimeoutValue``

The ``Timeout`` can be configured independently from the options of the :ref:`qos-circuit-breaker-strategy`:

.. code-block:: json

  "QoSOptions": {
    "Timeout": 5000 // ms
  }

This setup activates only the `Timeout resilience strategy`_.

To configure a global QoS timeout using the *Timeout strategy* for all routes (both static and dynamic) set the ``Timeout`` option as defined in the :ref:`config-global-configuration-schema`:

.. code-block:: json

  "GlobalConfiguration": {
    // other global props
    "QoSOptions": {
      "Timeout": 10000 // ms, 10 seconds
    }
  }

Please note that the route-level timeout takes precedence over the global timeout.
For example, a route timeout may be shorter, while the global timeout can be longer and apply to all routes.

  **Note**: There are :ref:`qos-notes-value-constraints` for ``Timeout``: it must be a positive number starting from *1 millisecond* to enable the *Timeout strategy*.
  If ``Timeout`` is undefined, zero or a negative number, the *Timeout strategy* will not be added to the resilience pipeline.
  Also, keep in mind Polly's `Timeout`_ constraint, thus Ocelot validates the ``Timeout``.
  If the value violates Polly's requirements, it will be rolled back to the default of *30 seconds*.

.. _qos-notes:

Notes
-----
.. _DefTimeout: https://github.com/search?q=repo%3AThreeMammals%2FOcelot+%22const+int+DefTimeout%22&type=code
.. _DefaultTimeoutSeconds: https://github.com/search?q=repo%3AThreeMammals%2FOcelot+%22static+int+DefaultTimeoutSeconds%22&type=code
.. _DefaultTimeout: https://github.com/search?q=repo%3AThreeMammals%2FOcelot+DefaultTimeout+path%3A%2F%5Esrc%5C%2FOcelot.Provider.Polly%5C%2F%2F&type=code


.. _qos-notes-absolute-timeout:

Absolute timeout [#f4]_
^^^^^^^^^^^^^^^^^^^^^^^

If a *QoS* section is not included, *QoS* will not be applied, and Ocelot will enforce an absolute timeout of 90 seconds (defined by the ``DownstreamRoute`` `DefTimeout`_ constant) for all downstream requests.
This absolute timeout is configurable via the ``DownstreamRoute`` `DefaultTimeoutSeconds`_ static C# property.
For more information, refer to the :ref:`config-default-timeout` section of the :doc:`../features/configuration` chapter.

.. _qos-notes-value-constraints:

Value constraints
^^^^^^^^^^^^^^^^^

Starting with `Polly`_ v8, the `Resilience strategies`_ documentation outlines the following constraints on values:

* The ``BreakDuration`` value must exceed **500** milliseconds and be less than **24** hours (1 day = ``86 400 000`` milliseconds).
  If unspecified or invalid, it defaults to **5000** milliseconds (5 seconds); refer to the `BreakDuration`_ documentation.
* The ``MinimumThroughput`` value must be **2** or greater.
  If unspecified or invalid, it defaults to **100** failures; refer to the `MinimumThroughput`_ documentation.
* The ``FailureRatio`` must be greater than **0.0** and no more than **1.0**.
  If unspecified or invalid, it defaults to **0.1** (10%); refer to the `FailureRatio`_ documentation.
* The ``SamplingDuration`` value must exceed **500** milliseconds and be less than **24** hours (1 day = ``86 400 000`` milliseconds).
  If unspecified or invalid, it defaults to **30000** milliseconds (30 seconds); refer to the `SamplingDuration`_ documentation.
* The ``Timeout`` must be greater than **10** milliseconds and less than **24** hours (1 day = ``86 400 000`` milliseconds).
  If unspecified or invalid, it defaults to **30000** milliseconds (30 seconds); refer to the `Timeout`_ documentation.
  And please note, when both route-level and global *QoS* timeouts have positive values but are invalid, a default value will be automatically substituted from the ``TimeoutStrategy`` class `DefaultTimeout`_ static C# property, which can also be configured in your `Program`_.

Ocelot logs warnings containing failed validation messages for all options, but it does not block Ocelot startup, even when *QoS* options are invalid.
Inspect your logs for these messages and adjust your configuration if necessary.

.. _qos-notes-qos-and-route-global-timeouts:

QoS and route (global) timeouts
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

The ``Timeout`` option in *QoS* always takes precedence over the route :ref:`config-timeout` property, so :ref:`config-timeout` will be ignored in favor of QoS ``Timeout``.
In Ocelot Core, ``Timeout`` and configuration :ref:`config-timeout` are not intended to be used together.
Moreover, there is an Ocelot Core design constraint: if the route or global ``Timeout`` duration is shorter than the *QoS* ``Timeout``, you may encounter warning messages in the logs that begin with the following sentence:

.. code-block:: text

  Route '/xxx' has Quality of Service settings (QoSOptions) enabled, but either the route Timeout or the QoS Timeout is misconfigured: ...

This warning means that the route or global timeout will occur before the *QoS* :ref:`qos-timeout-strategy` has a chance to handle its own timeout event, which is configured with a longer duration.
Technically, this situation results in the functional disabling of the Polly's `Timeout resilience strategy`_.
Ocelot handles this misconfiguration by logging a warning and automatically applying a longer timeout to the ``TimeoutDelegatingHandler`` in order to effectively unblock the *QoS* :ref:`qos-timeout-strategy`.
To avoid this warning, ensure that your *QoS* timeouts are shorter than the route or global timeouts, or remove the :ref:`config-timeout` property from routes where *QoS* is enabled with the ``Timeout`` option.

.. _qos-notes-global-and-default-qos-timeouts:

Global and default QoS timeouts
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

If a route-level *QoS* timeout is undefined, the global ``Timeout`` takes precedence over the default timeout (30 seconds, see the `Timeout`_ docs).
This means the global *QoS* timeout can override Polly's default of `30 seconds <https://github.com/search?q=repo%3AThreeMammals%2FOcelot+%22const+int+DefTimeout%22+path%3A%2F%5Esrc%5C%2FOcelot%5C.Provider%5C.Polly%5C%2F%2F&type=code>`_ via the :ref:`config-global-configuration-schema`.

.. _qos-extensibility:

Extensibility [#f5]_
--------------------

To use your ``ResiliencePipeline<T>`` provider, you can apply the following syntax:

.. code-block:: csharp
  :emphasize-lines: 3

  builder.Services
      .AddOcelot(builder.Configuration)
      .AddPolly<MyProvider>();
  // MyProvider should implement IPollyQoSResiliencePipelineProvider<HttpResponseMessage> 
  // Note: you can use standard provider PollyQoSResiliencePipelineProvider

Additionally, if you want to utilize your own ``DelegatingHandler``, the following syntax can be applied:

.. code-block:: csharp
  :emphasize-lines: 3

  builder.Services
      .AddOcelot(builder.Configuration)
      .AddPolly<MyProvider>(MyQosDelegatingHandlerDelegate);
  // MyQosDelegatingHandlerDelegate is a delegate use to get a DelegatingHandler. Refer to Ocelot's PollyResiliencePipelineDelegatingHandler

Finally, to define your own set of exceptions for mapping, you can apply the following syntax:

.. code-block:: csharp
  :emphasize-lines: 11

  static Error CreateError(Exception e) => new RequestTimedOutError(e);
  Dictionary<Type, Func<Exception, Error>> MyErrorMapping = new()
  {
      {typeof(TaskCanceledException), CreateError},
      {typeof(TimeoutRejectedException), CreateError},
      {typeof(BrokenCircuitException), CreateError},
      {typeof(BrokenCircuitException<HttpResponseMessage>), CreateError},
  };
  builder.Services
      .AddOcelot(builder.Configuration)
      .AddPolly<MyProvider>(MyErrorMapping);
  // Note: Default error mapping is defined in the DefaultErrorMapping field of the Ocelot.Provider.Polly.OcelotBuilderExtensions class

""""

.. [#f1] The :ref:`di-services-addocelot-method` adds default ASP.NET services to the DI container. You can call another extended :ref:`di-addocelotusingbuilder-method` while configuring services to develop your own :ref:`di-custom-builder`. See more instructions in the ":ref:`di-addocelotusingbuilder-method`" section of the :doc:`../features/dependencyinjection` feature.
.. [#f2] If something doesn't work or you're stuck, consider reviewing the current `QoS issues <https://github.com/search?q=repo%3AThreeMammals%2FOcelot+QoS&type=issues>`_ filtered by the |QoS_label| label.
.. [#f3] The :ref:`Global Configuration <qos-global-configuration>` for dynamic routes was first introduced in pull request `351`_ and released in version `7.0.1`_.
 Since then, global configuration for static routes was added in pull requests `2081`_ and `2339`_, and delivered in version `24.1`_.
 Support for dynamic routes was also added in pull request `2339`_ and delivered in version `24.1`_.
.. [#f4] The :ref:`Absolute timeout <qos-notes-absolute-timeout>` configuration, used as the :ref:`config-default-timeout`, and the :ref:`config-timeout` feature were requested in issue `1314`_, implemented in pull request `2073`_, and officially released in version `24.1`_.
.. [#f5] The :ref:`Extensibility <qos-extensibility>` feature was requested in issue `1875`_ and implemented through pull request `1914`_, as part of version `23.2`_.

.. _351: https://github.com/ThreeMammals/Ocelot/pull/351
.. _1314: https://github.com/ThreeMammals/Ocelot/issues/1314
.. _1875: https://github.com/ThreeMammals/Ocelot/issues/1875
.. _1914: https://github.com/ThreeMammals/Ocelot/pull/1914
.. _2073: https://github.com/ThreeMammals/Ocelot/pull/2073
.. _2081: https://github.com/ThreeMammals/Ocelot/pull/2081
.. _2339: https://github.com/ThreeMammals/Ocelot/pull/2339
.. _7.0.1: https://github.com/ThreeMammals/Ocelot/releases/tag/7.0.1
.. _23.2: https://github.com/ThreeMammals/Ocelot/releases/tag/23.2.0
.. _24.0: https://github.com/ThreeMammals/Ocelot/releases/tag/24.0.0
.. _24.1: https://github.com/ThreeMammals/Ocelot/releases/tag/24.1.0
.. _25.0: https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0
