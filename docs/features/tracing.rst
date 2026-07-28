Tracing
=======

  Feature of: :doc:`../features/logging`

  * `.NET logging and tracing | .NET | Microsoft Learn <https://learn.microsoft.com/en-us/dotnet/core/diagnostics/logging-tracing>`_
  * `.NET distributed tracing | .NET | Microsoft Learn <https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing>`_

This chapter explains how to perform distributed tracing using Ocelot.

.. |opentracing-csharp Logo| image:: https://avatars.githubusercontent.com/u/15482765
  :alt: opentracing-csharp Logo
  :width: 30

|opentracing-csharp Logo| OpenTracing
-------------------------------------

.. _OpenTracing: https://opentracing.io
.. _Ocelot.Tracing.OpenTracing: https://www.nuget.org/packages/Ocelot.Tracing.OpenTracing
.. _ThreeMammals/Ocelot.Tracing.OpenTracing: https://github.com/ThreeMammals/Ocelot.Tracing.OpenTracing

  | Package: `Ocelot.Tracing.OpenTracing`_
  | Namespace: ``Ocelot.Tracing.OpenTracing``
  | Repository: `ThreeMammals/Ocelot.Tracing.OpenTracing`_

Ocelot provides tracing functionality through the project from `opentracing-csharp <https://github.com/opentracing/opentracing-csharp>`_ repository.

.. code-block:: shell

  dotnet add package Ocelot.Tracing.OpenTracing

The example below uses the `C# Client for Jaeger <https://github.com/jaegertracing/jaeger-client-csharp>`_ to provide the tracer used in Ocelot.
To add `OpenTracing`_ services, you must call the ``AddOpenTracing()`` extension method on the ``OcelotBuilder`` returned by ``AddOcelot()`` [#f1]_, as shown below:

.. code-block:: csharp
  :emphasize-lines: 11

  builder.Services
      .AddSingleton(serviceProvider =>
      {
          var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
          var config = new Jaeger.Configuration(builder.Environment.ApplicationName, loggerFactory);
          var tracer = config.GetTracer();
          GlobalTracer.Register(tracer);
          return tracer;
      })
      .AddOcelot(builder.Configuration)
      .AddOpenTracing();

Then, in your `ocelot.json <https://github.com/ThreeMammals/Ocelot/blob/main/samples/OpenTracing/ocelot.json>`__, add the following to the route you want to trace:

.. code-block:: json

  "HttpHandlerOptions": {
    "UseTracing": true
  }

Ocelot will now send tracing information to `Jaeger <https://www.jaegertracing.io/>`_ whenever this route is called.

.. note::
  1. A clean yet functional sample can be found here: `Ocelot.Samples.OpenTracing <https://github.com/ThreeMammals/Ocelot/tree/main/samples/OpenTracing>`_.
  2. The `OpenTracing`_ project was archived on January 31, 2022 (see `the article <https://www.cncf.io/blog/2022/01/31/cncf-archives-the-opentracing-project/>`_).
     The Ocelot team is planning to decide on a migration to `OpenTelemetry <https://opentelemetry.io>`_, which is highly desirable.

.. warning::
  Starting with version `25.0`_, the `Ocelot.Tracing.OpenTracing`_ package has been extracted from the Ocelot mono-repo into its own dedicated repository.
  The package ID and namespace remain unchanged, but the source code, issues, and releases are now hosted at `ThreeMammals/Ocelot.Tracing.OpenTracing`_.

.. _tr-butterfly:

Butterfly
---------

.. _Butterfly: https://github.com/liuhaoyang/butterfly
.. _Ocelot.Tracing.Butterfly: https://www.nuget.org/packages/Ocelot.Tracing.Butterfly
.. _ThreeMammals/Ocelot.Tracing.Butterfly: https://github.com/ThreeMammals/Ocelot.Tracing.Butterfly

  | Package: `Ocelot.Tracing.Butterfly`_
  | Namespace: ``Ocelot.Tracing.Butterfly``
  | Repository: `ThreeMammals/Ocelot.Tracing.Butterfly`_

Ocelot provides tracing functionality through the excellent `Butterfly`_ project.
To use the tracing functionality, please refer to the `Butterfly`_ documentation.

In Ocelot, you need to add the NuGet package if you wish to trace a route:

.. code-block:: shell

  dotnet add package Ocelot.Tracing.Butterfly

In your `Program`_, to add `Butterfly`_ services, you must call the ``AddButterfly()`` extension method on the ``OcelotBuilder`` returned by ``AddOcelot()``, as shown below:

.. code-block:: csharp
  :emphasize-lines: 5

  using Ocelot.Tracing.Butterfly;

  builder.Services
      .AddOcelot(builder.Configuration)
      .AddButterfly(options =>
      {
          // This is the URL that the Butterfly collector server is running on...
          options.CollectorUrl = "http://localhost:9618";
          options.Service = "Ocelot";
      });

Then, in your `ocelot.json`_, add the following to the route you want to trace:

.. code-block:: json

  "HttpHandlerOptions": {
    "UseTracing": true
  }

Ocelot will now send tracing information to `Butterfly`_ whenever this route is called.

.. note::
  The `Butterfly`_ project has not been supported for more than seven years, as of 2026.
  The latest release of the `Butterfly.Client <https://www.nuget.org/packages/Butterfly.Client>`_ package (version `0.0.8 <https://www.nuget.org/packages/Butterfly.Client/0.0.8>`_) was made on February 22, 2018.
  As planned, the Ocelot team discontinued distribution of the `Ocelot.Tracing.Butterfly`_ package from the main Ocelot mono-repo in version `25.0`_, moving it to its own dedicated repository instead.

.. warning::
  Starting with version `25.0`_, the `Ocelot.Tracing.Butterfly`_ package has been extracted from the Ocelot mono-repo into its own dedicated repository.
  The package ID and namespace remain unchanged, but the source code, issues, and releases are now hosted at `ThreeMammals/Ocelot.Tracing.Butterfly`_.

""""

.. [#f1] The :ref:`di-services-addocelot-method` adds default ASP.NET services to the DI container. You can call another extended :ref:`di-addocelotusingbuilder-method` while configuring services to develop your own :ref:`di-custom-builder`. See more instructions in the ":ref:`di-addocelotusingbuilder-method`" section of the :doc:`../features/dependencyinjection` feature.

.. _Program: https://github.com/ThreeMammals/Ocelot/blob/main/samples/Basic/Program.cs
.. _ocelot.json: https://github.com/ThreeMammals/Ocelot/blob/main/samples/Basic/ocelot.json
.. _24.1: https://github.com/ThreeMammals/Ocelot/releases/tag/24.1.0
.. _25.0: https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0-beta.4
