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
For more details, visit the Polly library's official `repository <https://github.com/App-vNext/Polly>`_.

Installation
------------

To utilize the :ref:`administration-api`, begin by importing the appropriate `NuGet package <https://www.nuget.org/packages/Ocelot.Provider.Polly>`_:

.. code-block:: powershell

    Install-Package Ocelot.Provider.Polly

Next, in your `Program`_, incorporate `Polly`_ services by invoking the ``AddPolly()`` extension on the ``OcelotBuilder``, as shown below [#f1]_:

.. code-block:: csharp
  :emphasize-lines: 5

  using Ocelot.Provider.Polly;

  builder.Services
      .AddOcelot(builder.Configuration)
      .AddPolly();

.. _qos-configuration:

Configuration
-------------

Then add the following section to a route configuration: 

.. code-block:: json

  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 1000, // ms
    "TimeoutValue": 5000 // ms
  }

- To implement this rule, you must set a value of **2** or higher for ``ExceptionsAllowedBeforeBreaking``. [#f2]_
- ``DurationOfBreak`` indicates that the circuit breaker will remain open for **1** second after being triggered.
- ``TimeoutValue`` specifies that if a request exceeds **5** seconds, it will automatically time out.

.. _qos-circuit-breaker-strategy:

Circuit Breaker strategy
------------------------

The options ``ExceptionsAllowedBeforeBreaking`` and ``DurationOfBreak`` can be configured independently from ``TimeoutValue``:

.. code-block:: json

  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 1000
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

The ``TimeoutValue`` can be configured independently from the ``ExceptionsAllowedBeforeBreaking`` and ``DurationOfBreak`` options:

.. code-block:: json

  "QoSOptions": {
    "TimeoutValue": 5000 // ms
  }

This setup activates only the `Timeout <https://www.pollydocs.org/strategies/timeout.html>`_ strategy.

Notes
-----

1. If a *QoS* section is not included, *QoS* will not be applied, and Ocelot will enforce a default timeout of **90** `seconds <https://github.com/search?q=repo%3AThreeMammals%2FOcelot+90+language%3AC%23&type=code&l=C%23>`_ for all downstream requests.
   To request additional configurability, consider opening an issue. [#f3]_

2. The `Polly`_ V7 syntax is no longer supported as of version `23.2`_. [#f4]_

3. Starting with `Polly`_ V8, the `documentation`_ outlines the following constraints on values:

   * The ``ExceptionsAllowedBeforeBreaking`` value must be **2** or higher.
   * The ``DurationOfBreak`` value must exceed **500** milliseconds, defaulting to **5000** milliseconds (5 seconds) if unspecified or if the value is **500** milliseconds or less.
   * The ``TimeoutValue`` must be over **10** milliseconds.

   Refer to the `Resilience strategies <https://www.pollydocs.org/strategies/index.html>`_ documentation for a comprehensive explanation of each option.

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
.. [#f3] Recently, surrounding the release of version `24.0`_, we opened pull request `2073`_ to address the issue of default timeout configurations. This is a high-priority pull request, and the feature will be included in an upcoming major or minor release (excluding patches).
.. [#f4] We upgraded `Polly`_ from version 7.x to 8.x! The :ref:`qos-extensibility` feature was requested in issue `1875`_ and implemented through pull request `1914`_, as part of version `23.2`_.

.. _1875: https://github.com/ThreeMammals/Ocelot/issues/1875
.. _1914: https://github.com/ThreeMammals/Ocelot/pull/1914
.. _2073: https://github.com/ThreeMammals/Ocelot/pull/2073
.. _23.2: https://github.com/ThreeMammals/Ocelot/releases/tag/23.2.0
.. _24.0: https://github.com/ThreeMammals/Ocelot/releases/tag/24.0.0
