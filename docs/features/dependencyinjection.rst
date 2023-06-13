Dependency Injection
====================

    | **Namespace**: `Ocelot.DependencyInjection <https://github.com/search?q=repo%3AThreeMammals%2FOcelot+namespace+Ocelot.DependencyInjection&type=code>`_
    | **Source code**: `DependencyInjection <https://github.com/ThreeMammals/Ocelot/tree/develop/src/Ocelot/DependencyInjection>`_

Overview
--------

Dependency Injection feature in Ocelot is designed to extend and/or control building of Ocelot core as ASP.NET MVC pipeline services.
The main methods are `AddOcelot <#the-addocelot-method>`_ and `AddOcelotUsingBuilder <#the-addocelotusingbuilder-method>`_ of the ``ServiceCollectionExtensions`` class.
Use them in **Program.cs** and **Startup.cs** of your ASP.NET MVC gateway app (minimal web app) to enable and build the core of Ocelot.

And of course, the `OcelotBuilder <#the-ocelotbuilder-class>`_ class is the core of Ocelot.

IServiceCollection extensions
-----------------------------

    **Class**: `Ocelot.DependencyInjection.ServiceCollectionExtensions <https://github.com/ThreeMammals/Ocelot/blob/develop/src/Ocelot/DependencyInjection/ServiceCollectionExtensions.cs>`_

Based on the current implementations for the ``OcelotBuilder`` class, the ``AddOcelot`` method adds default ASP.NET services to DI-container.
You could call another more extended ``AddOcelotUsingBuilder`` method while configuring services to build and use custom builder via an ``IMvcCoreBuilder`` interface object.

The AddOcelot method
^^^^^^^^^^^^^^^^^^^^

    | **Signatures**:
    | ``IOcelotBuilder AddOcelot(this IServiceCollection services)``
    | ``IOcelotBuilder AddOcelot(this IServiceCollection services, IConfiguration configuration)``

This ``IServiceCollection`` extension method adds default ASP.NET services and Ocelot application services with configuration injected implicitly or explicitly.
Note! The method adds default ASP.NET services required for Ocelot core in the `AddDefaultAspNetServices <#the-adddefaultaspnetservices-method>`_ method which plays the role of default builder.

In this scenario, you do nothing except calling the ``AddOcelot`` method which has been mentioned in feature chapters, if additional startup settings are required.
In this case you just reuse default settings to build Ocelot core. The alternative is ``AddOcelotUsingBuilder`` method, see the next paragraph.

The AddOcelotUsingBuilder method
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

    | **Signatures**:
    | ``IOcelotBuilder AddOcelotUsingBuilder(this IServiceCollection services, Func<IMvcCoreBuilder, Assembly, IMvcCoreBuilder> customBuilder)``
    | ``IOcelotBuilder AddOcelotUsingBuilder(this IServiceCollection services, IConfiguration configuration, Func<IMvcCoreBuilder, Assembly, IMvcCoreBuilder> customBuilder)``

This ``IServiceCollection`` extension method adds Ocelot application services, and it *adds custom ASP.NET services* with configuration injected implicitly or explicitly.
Note! The method adds **custom** ASP.NET services required for Ocelot core using custom builder (``customBuilder`` parameter).
It is highly recommended to read docs of the `AddDefaultAspNetServices <#the-adddefaultaspnetservices-method>`_ method, 
or even to review implementation to understand default ASP.NET services which are the minimal part of the gateway core. 

In this custom scenario, you control everything during ASP.NET MVC pipeline building, and you provide custom settings to build Ocelot core.

The OcelotBuilder class
-----------------------

    **Source code**: `Ocelot.DependencyInjection.OcelotBuilder <https://github.com/ThreeMammals/Ocelot/blob/develop/src/Ocelot/DependencyInjection/OcelotBuilder.cs>`_

The ``OcelotBuilder`` class is the core of Ocelot which does the following:

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

The AddDefaultAspNetServices method
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

    **Class**: `Ocelot.DependencyInjection.OcelotBuilder <https://github.com/ThreeMammals/Ocelot/blob/develop/src/Ocelot/DependencyInjection/OcelotBuilder.cs>`_

Currently the method is protected and overriding is forbidden. The role of the method is to inject required services via both ``IServiceCollection`` and ``IMvcCoreBuilder`` interfaces objects
for the minimal part of the gateway core.

Current implementation is the folowing:

.. code-block:: csharp

        protected IMvcCoreBuilder AddDefaultAspNetServices(IMvcCoreBuilder builder, Assembly assembly)
        {
            Services
                .AddLogging()
                .AddMiddlewareAnalysis()
                .AddWebEncoders();

            return builder
                .AddApplicationPart(assembly)
                .AddControllersAsServices()
                .AddAuthorization()
                .AddNewtonsoftJson();
        }

The method cannot be overridden. It is not virtual, and there is no way to override current behavior by inheritance.
And, the method is default builder of Ocelot core while calling the  `AddOcelot <#the-addocelot-method>`_ method.
As alternative, to "override" this default builder, you can design and reuse custom builder as a ``Func<IMvcCoreBuilder, Assembly, IMvcCoreBuilder>`` delegate object 
and pass it as parameter to the `AddOcelotUsingBuilder <#the-addocelotusingbuilder-method>`_ extension method.
It gives you full control on design and buiding of Ocelot core, but be careful while designing your custom Ocelot core as customizable ASP.NET MVC pipeline.

Warning! Most of services from minimal part of the core should be reused, but only a few of services could be removed.
The next paragraph shows you an example of designing custom Ocelot core by custom builder which removes default 
`Newtonsoft JSON <https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.newtonsoftjsonmvccorebuilderextensions.addnewtonsoftjson?view=aspnetcore-7.0>`_ services 
and adds modern `JSON services <https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.mvccoremvccorebuilderextensions.addjsonoptions?view=aspnetcore-7.0>`_ 
from `the box <https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.mvccoremvccorebuilderextensions?view=aspnetcore-7.0>`_.

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
