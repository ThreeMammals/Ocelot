Quality of Service
==================

    Label: `QoS <https://github.com/ThreeMammals/Ocelot/labels/QoS>`_

Ocelot supports one QoS capability at the current time. You can set on a per Route basis if you want to use a circuit breaker when making requests to a downstream service.
This uses an awesome .NET library called `Polly`_, check them out `in official repository <https://github.com/App-vNext/Polly>`_.

The first thing you need to do if you want to use the :doc:`../features/administration` API is bring in the relevant NuGet `package <https://www.nuget.org/packages/Ocelot.Provider.Polly>`_:

.. code-block:: powershell

    Install-Package Ocelot.Provider.Polly

Then in your ``ConfigureServices`` method to add `Polly`_ services we must call the ``AddPolly()`` extension of the ``OcelotBuilder`` being returned by ``AddOcelot()`` [#f1]_ like below:

.. code-block:: csharp

    services.AddOcelot()
        .AddPolly();

Then add the following section to a Route configuration: 

.. code-block:: json

  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 1000,
    "TimeoutValue": 5000
  }

- You must set a number equal or greater than ``2`` against **ExceptionsAllowedBeforeBreaking** for this rule to be implemented. [#f2]_
- **DurationOfBreak** means the circuit breaker will stay open for 1 second after it is tripped.
- **TimeoutValue** means if a request takes more than 5 seconds, it will automatically be timed out. 

You can set the **TimeoutValue** in isolation of the **ExceptionsAllowedBeforeBreaking** and **DurationOfBreak** options:

.. code-block:: json

  "QoSOptions": {
    "TimeoutValue": 5000
  }

There is no point setting the other two in isolation as they affect each other!

Defaults
--------

If you do not add a QoS section, QoS will not be used, however Ocelot will default to a **90** seconds timeout on all downstream requests.
If someone needs this to be configurable, open an issue. [#f2]_

.. _qos-polly-v7-vs-v8:

`Polly`_ v7 vs v8
-----------------

Important changes in version `23.2`_: [#f3]_

  - With `Polly`_ version 8+, the ``ExceptionsAllowedBeforeBreaking`` value must be equal to or greater than **2**!
  - The ``AddPolly`` method has been migrated from v7 policy wrappers to v8 resilience pipelines. Consequently, it now exhibits different behavior based on v8 pipelines.

If you prefer not to modify your settings, you can continue using `Polly`_ v7 as follows:

.. code-block:: csharp

    services.AddOcelot()
        .AddPollyV7();

**Note**: Support for `Polly`_ v7 will be removed in a future version. We recommend avoiding this method (which is tagged as ``Obsolete``) unless absolutely necessary.

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
