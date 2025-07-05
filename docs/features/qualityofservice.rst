.. role:: htm(raw)
  :format: html
.. role:: pdf(raw)
  :format: latex pdflatex
.. _Program: https://github.com/ThreeMammals/Ocelot/blob/main/samples/Basic/Program.cs
.. _Polly: https://www.thepollyproject.org
.. _documentation: https://www.pollydocs.org
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

.. _qos-configuration-schema:

Configuration Schema
--------------------

Here is the complete *Quality of Service* configuration, also known as the "QoS options schema".
Depending on your needs and choosen strategies definition of all properties are not required.
If you skip a property then a default value will be substituted as per Ocelot/Polly specification.

.. code-block:: json

  "QoSOptions": {
    // Circuit Breaker strategy
    "DurationOfBreak": 1, // integer
    "ExceptionsAllowedBeforeBreaking": 1, // integer
    "FailureRatio": 0.1, // floating number
    "SamplingDuration": 1, // integer
    // Timeout strategy
    "TimeoutValue": 1, // integer
  }

- To implement this rule, you must set a value of **2** or higher for ``ExceptionsAllowedBeforeBreaking``. [#f2]_
- ``DurationOfBreak`` indicates that the circuit breaker will remain open for **1** second after being triggered.
- ``FailureRatio`` sets the failure-to-success ratio at which the circuit will break. Default value is ``0.1`` ~ 10% of failed.
- ``SamplingDuration``: 10000 // The time period over which the failure-success ratio is calculated (in milliseconds), default is 10000 (10s)
- ``TimeoutValue`` specifies that if a request exceeds **5** seconds, it will automatically time out.

  | Please note: if you use the Circuit-Breaker, Ocelot checks that the parameters are correct during execution. If not, it throws an exception.
  | For a complete explanation about Circuit-Breaker strategies and mechanisms, consult Polly documentation here <https://www.pollydocs.org/strategies/circuit-breaker>

.. _qos-circuit-breaker-strategy:

Circuit Breaker strategy
------------------------

The options ``ExceptionsAllowedBeforeBreaking`` and ``DurationOfBreak`` can be configured independently from ``TimeoutValue``:

.. code-block:: json

  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 1000 // ms
  }

Alternatively, you can omit ``DurationOfBreak``, which will default to the implicit 5-second setting as specified in Polly's `documentation`_:

.. code-block:: json

  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3
  }

This setup activates only the `Circuit breaker <https://www.pollydocs.org/strategies/circuit-breaker.html>`_ strategy.

.. _qos-timeout-strategy:

Timeout strategy
----------------
.. _Timeout: https://www.pollydocs.org/strategies/timeout.html

The ``TimeoutValue`` can be configured independently from the options of the :ref:`qos-circuit-breaker-strategy`:

.. code-block:: json

  "QoSOptions": {
    "TimeoutValue": 5000 // ms
  }

This setup activates only the `Timeout`_ strategy.
To configure a global QoS timeout using the `Timeout`_ strategy for all static routes (excluding dynamic routes), set the ``TimeoutValue`` option according to the :ref:`config-global-configuration-schema`:

.. code-block:: json

  "GlobalConfiguration": {
    // other global props
    "QoSOptions": {
      "TimeoutValue": 10000 // ms, 10 seconds
    }
  }

Please note that the route-level timeout takes precedence over the global timeout.
For example, a route timeout may be shorter, while the global timeout can be longer and apply to all routes.

.. _TimeoutStrategyOptions.Timeout: https://www.pollydocs.org/api/Polly.Timeout.TimeoutStrategyOptions.html#Polly_Timeout_TimeoutStrategyOptions_Timeout

  There are value constraints for ``TimeoutValue``: it must be a positive number starting from *1 millisecond* to enable the `Timeout`_ strategy.
  If ``TimeoutValue`` is set to zero or a negative number, the `Timeout`_ strategy will not be added to the resilience pipeline.
  Also, keep in mind Polly's `TimeoutStrategyOptions.Timeout`_ constraint, thus Ocelot validates the ``TimeoutValue``.
  If the value violates Polly's requirements, it will be rolled back to the default of *30 seconds*, as specified in the `Polly`_ documentation.

.. _qos-notes:

Notes
-----
.. _DownstreamRoute.DefTimeout: https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20DownstreamRoute.DefTimeout&type=code

1. **Absolute timeout** [#f3]_. If a *QoS* section is not included, *QoS* will not be applied, and Ocelot will enforce an absolute timeout of 90 seconds (defined by the `DownstreamRoute.DefTimeout`_ constant) for all downstream requests.
   This absolute timeout is configurable via the ``DownstreamRoute.DefaultTimeoutSeconds`` static C# property.
   For more information, refer to the :ref:`config-default-timeout` section of the :doc:`../features/configuration` chapter.

2. The `Polly`_ V7 syntax is no longer supported as of version `23.2`_. [#f4]_

3. Starting with `Polly`_ V8, the `documentation`_ outlines the following constraints on values:

   * The ``ExceptionsAllowedBeforeBreaking`` value must be **2** or higher.
   * The ``DurationOfBreak`` value must exceed **500** milliseconds, defaulting to **5000** milliseconds (5 seconds) if unspecified or if the value is **500** milliseconds or less.
   * The ``TimeoutValue`` must be over **10** milliseconds.

   Refer to the `Resilience strategies <https://www.pollydocs.org/strategies/index.html>`_ documentation for a comprehensive explanation of each option.

4. **QoS and route/global timeouts**.
   The ``TimeoutValue`` option in *QoS* always takes precedence over the route-level ``Timeout`` property, so ``Timeout`` will be ignored in favor of ``TimeoutValue``.
   In Ocelot Core, ``TimeoutValue`` and ``Timeout`` are not intended to be used together.
   Moreover, there is an Ocelot Core design constraint: if the route or global ``Timeout`` duration is shorter than the *QoS* ``TimeoutValue``, you may encounter warning messages in the logs that begin with the following sentence:

   .. code-block:: text

    Route '/xxx' has Quality of Service settings (QoSOptions) enabled, but either the route Timeout or the QoS TimeoutValue is misconfigured: ...

   This warning means that the route or global timeout will occur before the *QoS* :ref:`qos-timeout-strategy` has a chance to handle its own timeout event, which is configured with a longer duration.
   Technically, this situation results in the functional disabling of the Polly's `Timeout`_ strategy.
   Ocelot handles this misconfiguration by logging a warning and automatically applying a longer timeout to the ``TimeoutDelegatingHandler`` in order to effectively unblock the *QoS* :ref:`qos-timeout-strategy`.
   To avoid this warning, ensure that your *QoS* timeouts are shorter than the route or global timeouts, or remove the ``Timeout`` property from routes where *QoS* is enabled with the ``TimeoutValue`` option.

5. Both route-level and global *QoS* options apply only to static routes, as defined by the :ref:`config-route-schema`.
   Since the :ref:`config-dynamic-route-schema` does not support *QoS* options, *Quality of Service* is not applied to dynamic routes in :ref:`routing-dynamic`.

.. _qos-extensibility:

Extensibility [#f4]_
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
.. [#f3] The absolute timeout configuration, used as the :ref:`config-default-timeout`, and the :ref:`config-timeout` feature were requested in issue `1314`_, implemented in pull request `2073`_, and officially released in version `24.1`_.
.. [#f4] We upgraded `Polly`_ from version 7.x to 8.x! The :ref:`qos-extensibility` feature was requested in issue `1875`_ and implemented through pull request `1914`_, as part of version `23.2`_.

.. _1314: https://github.com/ThreeMammals/Ocelot/issues/1314
.. _1875: https://github.com/ThreeMammals/Ocelot/issues/1875
.. _1914: https://github.com/ThreeMammals/Ocelot/pull/1914
.. _2073: https://github.com/ThreeMammals/Ocelot/pull/2073
.. _23.2: https://github.com/ThreeMammals/Ocelot/releases/tag/23.2.0
.. _24.0: https://github.com/ThreeMammals/Ocelot/releases/tag/24.0.0
.. _24.1: https://github.com/ThreeMammals/Ocelot/releases/tag/24.1.0
