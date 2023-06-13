Dependency Injection
====================

    | **Namespace**: `Ocelot.DependencyInjection <https://github.com/search?q=repo%3AThreeMammals%2FOcelot+namespace+Ocelot.DependencyInjection&type=code>`_
    | **Source code**: `DependencyInjection <https://github.com/ThreeMammals/Ocelot/tree/develop/src/Ocelot/DependencyInjection>`_

Overview
--------

Dependency Injection feature in Ocelot is designed to extend and/or control building of Ocelot core as ASP.NET MVC pipeline services.
The main methods are `AddOcelot <#the-addocelot-method>`_ and `AddOcelotUsingBuilder <#the-addocelotusingbuilder-method>`_ of the ``ServiceCollectionExtensions`` class.
Use them in **Program.cs** and **Startup.cs** of your ASP.NET MVC gateway app (minimal web app) to enable and build the core of Ocelot.

And of course, the ``OcelotBuilder`` class is the core of Ocelot which does the following:

- Contructs itself by single public constructor:
    ``public OcelotBuilder(IServiceCollection services, IConfiguration configurationRoot, Func<IMvcCoreBuilder, Assembly, IMvcCoreBuilder> customBuilder = null)``
- Initializes and stores public properties:
    **Services** (``IServiceCollection`` object), **Configuration** (``IConfiguration`` object) and **MvcCoreBuilder** (``IMvcCoreBuilder`` object)
- Adds **all application services** during construction phase over the ``Services`` property
- Adds ASP.NET services by builder using ``Func<IMvcCoreBuilder, Assembly, IMvcCoreBuilder>`` object in these 2 user scenarios:
    - by default builder (``AddDefaultAspNetServices`` method) if there is no ``customBuilder`` parameter provided
    - by custom builder with provided delegate object as ``customBuilder`` parameter
- Adds (switches on/off) Ocelot features by:
    - ``AddSingletonDefinedAggregator`` and ``AddTransientDefinedAggregator`` methods
    - ``AddCustomLoadBalancer`` method
    - ``AddDelegatingHandler`` method
    - ``AddConfigPlaceholders`` method

The AddOcelot method
--------------------

Based on the current dependency injection implementations for the ``OcelotBuilder`` class, the ``AddOcelot`` method adds default ASP.NET services to DI-container.
You could call another more extended ``AddOcelotUsingBuilder`` method while configuring services to build and use custom builder via an ``IMvcCoreBuilder`` interface object.

The AddOcelotUsingBuilder method
--------------------------------

C# method signature:
``public static IOcelotBuilder AddOcelotUsingBuilder(this IServiceCollection services, Func<IMvcCoreBuilder, Assembly, IMvcCoreBuilder> customBuilder)``

Newtonsoft.Json vs System.Text.Json
-----------------------------------

The default ``AddOcelot`` method adds ``.AddNewtonsoftJson()``, which was necessary when Microsoft did not launch the ``System.Text.Json`` library, 
but now it affects normal use, so this PR is mainly to solve the problem problem.

Added the following methods in ``Ocelot.DependencyInjection.ServiceCollectionExtensions``
- ``AddOcelotWithCustomMvcCoreBuilder(this IServiceCollection services, Func<IMvcCoreBuilder, Assembly, IMvcCoreBuilder> customMvcCoreBuilder)``
- ``AddOcelotWithCustomMvcCoreBuilder(this IServiceCollection services, IConfiguration configuration, Func<IMvcCoreBuilder, Assembly, IMvcCoreBuilder> customMvcCoreBuilder)``


Proposed Changes
^^^^^^^^^^^^^^^^

Support custom ``MvcCoreBuilder`` to adapt to more changes in the future, this change is mainly to support ``System.Text.Json``
This allows users to use their desired JSON library for serialization, such as ``System.Text.Json``.

For example:

.. code-block:: csharp

    service.AddOcelotUsingBuilder((builder, assembly) =>
    {
        return builder
            .AddApplicationPart(assembly)
            .AddControllersAsServices()
            .AddAuthorization()
            .AddJsonOptions(); // use System.Text.Json
    });

This is just one of the common usages, users can add more modules they need in the builder.
