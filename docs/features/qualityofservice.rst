Quality of Service
==================

    Label: `QoS <https://github.com/ThreeMammals/Ocelot/labels/QoS>`_

Ocelot supports one QoS capability at the current time. You can set on a per Route basis if you want to use a circuit breaker when making requests to a downstream service.
This uses an awesome .NET library called `Polly <https://www.thepollyproject.org/>`_, check them out `in official repo <https://github.com/App-vNext/Polly>`_.

The first thing you need to do if you want to use the :doc:`../features/administration` API is bring in the relevant NuGet `package <https://www.nuget.org/packages/Ocelot.Provider.Polly>`_:

.. code-block:: powershell

    Install-Package Ocelot.Provider.Polly

Then in your ``ConfigureServices`` method to add Polly services we must call the ``AddPolly()`` extension of the ``OcelotBuilder`` being returned by ``AddOcelot()`` [#f1]_ like below:

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

- You must set a number greater than ``0`` against **ExceptionsAllowedBeforeBreaking** for this rule to be implemented. [#f2]_
- **DurationOfBreak** means the circuit breaker will stay open for 1 second after it is tripped.
- **TimeoutValue** means if a request takes more than 5 seconds, it will automatically be timed out. 

You can set the **TimeoutValue** in isolation of the **ExceptionsAllowedBeforeBreaking** and **DurationOfBreak** options:

.. code-block:: json

  "QoSOptions": {
    "TimeoutValue": 5000
  }

There is no point setting the other two in isolation as they affect each other!

If you do not add a QoS section, QoS will not be used, however Ocelot will default to a **90** seconds timeout on all downstream requests.
If someone needs this to be configurable, open an issue. [#f2]_

""""

.. [#f1] The ``AddOcelot`` method adds default ASP.NET services to DI-container. You could call another more extended ``AddOcelotUsingBuilder`` method while configuring services to build and use custom builder via an ``IMvcCoreBuilder`` interface object. See more instructions in :doc:`../features/dependencyinjection`, "**The AddOcelotUsingBuilder method**" section.
.. [#f2] If something doesn't work or you get stuck, please review current `QoS <https://github.com/search?q=repo%3AThreeMammals%2FOcelot+QoS&type=issues>`_ issues filtering by |QoS| label.

.. |QoS| image:: https://img.shields.io/badge/-QoS-D3ADAF.svg
   :target: https://github.com/ThreeMammals/Ocelot/labels/QoS
