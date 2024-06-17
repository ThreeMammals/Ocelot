Quality of Service
==================

    Label: `QoS <https://github.com/ThreeMammals/Ocelot/labels/QoS>`_

Ocelot currently supports a single **QoS** capability.
It allows you to configure, on a per-route basis, the use of a circuit breaker when making requests to downstream services.
This feature leverages a superb .NET library known as `Polly`_. For more information, visit their `official repository <https://github.com/App-vNext/Polly>`_.

Installation
------------

To use the :doc:`../features/administration` API, the first step is to import the relevant NuGet `package <https://www.nuget.org/packages/Ocelot.Provider.Polly>`_:

.. code-block:: powershell

    Install-Package Ocelot.Provider.Polly

Next, within your ``ConfigureServices`` method, to incorporate `Polly`_ services, invoke the ``AddPolly()`` extension on the ``OcelotBuilder`` returned by ``AddOcelot()`` [#f1]_ as shown below:

.. code-block:: csharp

    services.AddOcelot()
        .AddPolly();

.. _qos-configuration:

Configuration
-------------

Then add the following section to a Route configuration: 

.. code-block:: json

  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 1000,
    "TimeoutValue": 5000,
	"FailureRatio": .8 // .8 = 80% of failed, this is default value
	"SamplingDuration": 10000 // The time period over which the failure-success ratio is calculated (in milliseconds), default is 10000 (10s)
  }

- You must set a number equal or greater than ``2`` against ``ExceptionsAllowedBeforeBreaking`` for this rule to be implemented. [#f2]_
- ``DurationOfBreak`` means the circuit breaker will stay open for 1 second after it is tripped.
- ``TimeoutValue`` means if a request takes more than 5 seconds, it will automatically be timed out. 

  | Please note: if you use the Circuit-Breaker, Ocelot checks that the parameters are correct during execution. If not, it throws an exception.
  | For a complete explanation about Circuit-Breaker strategies and mechanisms, consult Polly documentation here <https://www.pollydocs.org/strategies/circuit-breaker>

.. _qos-circuit-breaker-strategy:

Circuit Breaker strategy
------------------------

The options ``ExceptionsAllowedBeforeBreaking`` and ``DurationOfBreak`` can be configured independently of ``TimeoutValue``:

.. code-block:: json

  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 1000
  }

Alternatively, you may omit ``DurationOfBreak`` to default to the implicit 5 seconds as per Polly `documentation <https://www.pollydocs.org/>`_:

.. code-block:: json

  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3
  }

This setup activates only the `Circuit breaker <https://www.pollydocs.org/strategies/circuit-breaker.html>`_ strategy.

.. _qos-timeout-strategy:

Timeout strategy
----------------

The ``TimeoutValue`` can be configured independently from the ``ExceptionsAllowedBeforeBreaking`` and ``DurationOfBreak`` settings:

.. code-block:: json

  "QoSOptions": {
    "TimeoutValue": 5000
  }

This setup activates only the `Timeout <https://www.pollydocs.org/strategies/timeout.html>`_ strategy.

Notes
-----

1. Without a QoS section, QoS will not be utilized, and Ocelot will impose a default timeout of **90** seconds for all downstream requests.
   To request configurability, please open an issue. [#f2]_

2. `Polly`_ V7 syntax is no longer supported as of version `23.2`_. [#f3]_

3. For `Polly`_ version 8 and above, the following constraints on values are specified in `the documentation <https://www.pollydocs.org/>`_:

   * The ``ExceptionsAllowedBeforeBreaking`` value must be **2** or higher.
   * The ``DurationOfBreak`` value must exceed **500** milliseconds, defaulting to **5000** milliseconds (5 seconds) if unspecified or if the value is **500** milliseconds or less.
   * The ``TimeoutValue`` must be over **10** milliseconds.

   Consult the `Resilience strategies <https://www.pollydocs.org/strategies/index.html>`_ documentation for a detailed understanding of each option.

.. _qos-extensibility:

Extensibility [#f3]_
--------------------

If you want to use your ``ResiliencePipeline<T>`` provider, you can use the following syntax:

.. code-block:: csharp

    services.AddOcelot()
        .AddPolly<MyProvider>();
   // MyProvider should implement IPollyQoSResiliencePipelineProvider<HttpResponseMessage> 
   // Note: you can use standard provider PollyQoSResiliencePipelineProvider

If, in addition, you want to use your own ``DelegatingHandler``, you can use the following syntax:

.. code-block:: csharp

    services.AddOcelot()
        .AddPolly<MyProvider>(MyQosDelegatingHandlerDelegate);
   // MyProvider should implement IPollyQoSResiliencePipelineProvider<HttpResponseMessage> 
   // Note: you can use standard provider PollyQoSResiliencePipelineProvider
   // MyQosDelegatingHandlerDelegate is a delegate use to get a DelegatingHandler

And finally, if you want to define your own set of exceptions to map, you can use the following syntax:

.. code-block:: csharp

    services.AddOcelot()
        .AddPolly<MyProvider>(MyErrorMapping);
    // MyProvider should implement IPollyQoSResiliencePipelineProvider<HttpResponseMessage> 
    // Note: you can use standard provider PollyQoSResiliencePipelineProvider

    // MyErrorMapping is a Dictionary<Type, Func<Exception, Error>>, eg:
    private static readonly Dictionary<Type, Func<Exception, Error>> MyErrorMapping = new()
    {
        {typeof(TaskCanceledException), CreateError},
        {typeof(TimeoutRejectedException), CreateError},
        {typeof(BrokenCircuitException), CreateError},
        {typeof(BrokenCircuitException<HttpResponseMessage>), CreateError},
    };
    private static Error CreateError(Exception e) => new RequestTimedOutError(e);

""""

.. [#f1] :ref:`di-the-addocelot-method` adds default ASP.NET services to DI container. You could call another extended :ref:`di-addocelotusingbuilder-method` while configuring services to develop your own :ref:`di-custom-builder`. See more instructions in the ":ref:`di-addocelotusingbuilder-method`" section of :doc:`../features/dependencyinjection` feature.
.. [#f2] If something doesn't work or you get stuck, please review current `QoS issues <https://github.com/search?q=repo%3AThreeMammals%2FOcelot+QoS&type=issues>`_ filtering by |QoS_label| label.
.. [#f3] We upgraded `Polly`_ version from v7.x to v8.x! The :ref:`qos-extensibility` feature was requested in issue `1875`_ and delivered by PR `1914`_ as a part of version `23.2`_.

.. _Polly: https://www.thepollyproject.org
.. _1875: https://github.com/ThreeMammals/Ocelot/issues/1875
.. _1914: https://github.com/ThreeMammals/Ocelot/pull/1914
.. _23.2: https://github.com/ThreeMammals/Ocelot/releases/tag/23.2.0
.. |QoS_label| image:: https://img.shields.io/badge/-QoS-D3ADAF.svg
   :target: https://github.com/ThreeMammals/Ocelot/labels/QoS
