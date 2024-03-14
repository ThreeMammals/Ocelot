Tracing
=======

This page details how to perform distributed tracing with Ocelot. 

.. |opentracing-csharp Logo| image:: https://avatars.githubusercontent.com/u/15482765
  :alt: opentracing-csharp Logo
  :width: 30

|opentracing-csharp Logo| OpenTracing
-------------------------------------

Ocelot providers tracing functionality from the excellent `OpenTracing API for .NET <https://github.com/opentracing/opentracing-csharp>`_ project. 
The code for the Ocelot integration can be found in `Ocelot.Tracing.OpenTracing <https://github.com/ThreeMammals/Ocelot/tree/main/src/Ocelot.Tracing.OpenTracing>`_ project.

The example below uses `C# Client for Jaeger <https://github.com/jaegertracing/jaeger-client-csharp>`_ client to provide the tracer used in Ocelot.
In order to add `OpenTracing <https://opentracing.io/>`_ services we must call the ``AddOpenTracing()`` extension of the ``OcelotBuilder`` being returned by ``AddOcelot()`` [#f1]_ like below:

.. code-block:: csharp

    services.AddSingleton<ITracer>(sp =>
    {
        var loggerFactory = sp.GetService<ILoggerFactory>();
        Configuration config = new Configuration(context.HostingEnvironment.ApplicationName, loggerFactory);

        var tracer = config.GetTracer();
        GlobalTracer.Register(tracer);
        return tracer;
    });

    services
        .AddOcelot()
        .AddOpenTracing();

Then in your **ocelot.json** add the following to the Route you want to trace:

.. code-block:: json

  "HttpHandlerOptions": {
    "UseTracing": true
  }

Ocelot will now send tracing information to `Jaeger <https://www.jaegertracing.io/>`_ when this Route is called.

OpenTracing Status
^^^^^^^^^^^^^^^^^^

The `OpenTracing <https://opentracing.io/>`_ project was archived on January 31, 2022 (see `the article <https://www.cncf.io/blog/2022/01/31/cncf-archives-the-opentracing-project/>`_).
The Ocelot team will decide on a migration to `OpenTelemetry <https://opentelemetry.io/>`_ which is highly desired.

Butterfly
---------

Ocelot providers tracing functionality from the excellent `Butterfly <https://github.com/liuhaoyang/butterfly>`_ project.
The code for the Ocelot integration can be found in `Ocelot.Tracing.Butterfly <https://github.com/ThreeMammals/Ocelot/tree/main/src/Ocelot.Tracing.Butterfly>`_ project.

In order to use the tracing please read the `Butterfly <https://github.com/liuhaoyang/butterfly>`_ documentation.

In Ocelot you need to add the `NuGet package <https://www.nuget.org/packages/Ocelot.Tracing.Butterfly>`_ if you wish to trace a Route:

.. code-block:: powershell

    Install-Package Ocelot.Tracing.Butterfly

In your ``ConfigureServices`` method to add Butterfly services: we must call the ``AddButterfly()`` extension of the ``OcelotBuilder`` being returned by ``AddOcelot()`` [#f1]_ like below:

.. code-block:: csharp

    services
        .AddOcelot()
        // This comes from Ocelot.Tracing.Butterfly package
        .AddButterfly(option =>
        {
            // This is the URL that the Butterfly collector server is running on...
            option.CollectorUrl = "http://localhost:9618";
            option.Service = "Ocelot";
        });

Then in your **ocelot.json** add the following to the Route you want to trace:

.. code-block:: json

  "HttpHandlerOptions": {
    "UseTracing": true
  }

Ocelot will now send tracing information to Butterfly when this Route is called.

""""

.. [#f1] :ref:`di-the-addocelot-method` adds default ASP.NET services to DI container. You could call another extended :ref:`di-addocelotusingbuilder-method` while configuring services to develop your own :ref:`di-custom-builder`. See more instructions in the ":ref:`di-addocelotusingbuilder-method`" section of :doc:`../features/dependencyinjection` feature.
