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
- Adds ASP.NET services by builder using ``Func<IMvcCoreBuilder, Assembly, IMvcCoreBuilder>`` object in these 2 development scenarios:
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
And, the method is default builder of Ocelot core while calling the `AddOcelot <#the-addocelot-method>`_ method.
As alternative, to "override" this default builder, you can design and reuse custom builder as a ``Func<IMvcCoreBuilder, Assembly, IMvcCoreBuilder>`` delegate object 
and pass it as parameter to the `AddOcelotUsingBuilder <#the-addocelotusingbuilder-method>`_ extension method.
It gives you full control on design and buiding of Ocelot core, but be careful while designing your custom Ocelot core as customizable ASP.NET MVC pipeline.

Warning! Most of services from minimal part of the core should be reused, but only a few of services could be removed.

Warning!! The method above is called after adding required services of ASP.NET MVC pipeline building by 
`AddMvcCore <https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.mvccoreservicecollectionextensions.addmvccore?view=aspnetcore-7.0>`_ method 
over the ``Services`` property in upper calling context. These services are absolute minimum core services for ASP.NET MVC pipeline. They must be added to DI-container always, 
and they are added implicitly before calling of the method by caller in upper context. So, ``AddMvcCore`` creates an ``IMvcCoreBuilder`` object with its assignment to the ``MvcCoreBuilder`` property.
Finally, as default builder the method above receives ``IMvcCoreBuilder`` object being ready for further extensions.

The next paragraph shows you an example of designing custom Ocelot core by custom builder.

Custom Builder
--------------
**Goal**: Replace ``Newtonsoft.Json`` services by ``System.Text.Json`` services.

The Problem
^^^^^^^^^^^

The default `AddOcelot <#the-addocelot-method>`_ method adds 
`Newtonsoft JSON <https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.newtonsoftjsonmvccorebuilderextensions.addnewtonsoftjson?view=aspnetcore-7.0>`_ services 
by the ``AddNewtonsoftJson`` extension method in default builder (the `AddDefaultAspNetServices <#the-adddefaultaspnetservices-method>`_ method). 
The ``AddNewtonsoftJson`` method calling was introduced in old .NET and Ocelot releases which was necessary when Microsoft did not launch the ``System.Text.Json`` library, 
but now it affects normal use, so we have an intention to solve the problem.

Modern `JSON services <https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.mvccoremvccorebuilderextensions.addjsonoptions?view=aspnetcore-7.0>`_ 
from `the box <https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.mvccoremvccorebuilderextensions?view=aspnetcore-7.0>`_
will help to configure JSON settings by the ``JsonSerializerOptions`` property for JSON formatters during (de)serialization.

Solution
^^^^^^^^

We have the following methods in ``Ocelot.DependencyInjection.ServiceCollectionExtensions`` class:

- ``IOcelotBuilder AddOcelotUsingBuilder(this IServiceCollection services, Func<IMvcCoreBuilder, Assembly, IMvcCoreBuilder> customBuilder)``
- ``IOcelotBuilder AddOcelotUsingBuilder(this IServiceCollection services, IConfiguration configuration, Func<IMvcCoreBuilder, Assembly, IMvcCoreBuilder> customBuilder)``

These method with custom builder allows you to use your any desired JSON library for (de)serialization.
But we are going to create custom ``MvcCoreBuilder`` with support of JSON services, such as ``System.Text.Json``.
To do that we need to call ``AddJsonOptions`` extension of the ``MvcCoreMvcCoreBuilderExtensions`` class 
(NuGet package: `Microsoft.AspNetCore.Mvc.Core <https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Core/>`_) in **Startup.cs** file:

.. code-block:: csharp

    using Microsoft.Extensions.DependencyInjection;
    using Ocelot.DependencyInjection;
    using System.Reflection;
    
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddLogging()
                .AddMiddlewareAnalysis()
                .AddWebEncoders()

                .AddOcelotUsingBuilder(MyCustomBuilder);
        }

        private static IMvcCoreBuilder MyCustomBuilder(IMvcCoreBuilder builder, Assembly assembly)
        {
            return builder
                .AddApplicationPart(assembly)
                .AddControllersAsServices()
                .AddAuthorization()

                // Replace AddNewtonsoftJson() by AddJsonOptions()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.WriteIndented = true; // use System.Text.Json
                });
        }
    }

This sample code provides settings to render JSON as indented text rather than zipped plain JSON text.
And, this is just one of the common usages, you can add more services you need in the builder.
