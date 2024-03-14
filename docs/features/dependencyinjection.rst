Dependency Injection
====================

    | **Namespace**: `Ocelot.DependencyInjection <https://github.com/search?q=repo%3AThreeMammals%2FOcelot+namespace+Ocelot.DependencyInjection&type=code>`_
    | **Source code**: `DependencyInjection <https://github.com/ThreeMammals/Ocelot/tree/develop/src/Ocelot/DependencyInjection>`_

Overview
--------

Dependency Injection feature in Ocelot is designed to extend and/or control building of Ocelot core as ASP.NET MVC pipeline services.
The main methods are `AddOcelot <#the-addocelot-method>`_ and `AddOcelotUsingBuilder <#the-addocelotusingbuilder-method>`_ of the ``ServiceCollectionExtensions`` class.
Use them in **Program.cs** and **Startup.cs** of your ASP.NET MVC gateway app (minimal web app) to enable and build Ocelot pipeline.

And of course, the `OcelotBuilder <#the-ocelotbuilder-class>`_ class is the core of Ocelot.

IServiceCollection extensions
-----------------------------

    | **Namespace**: ``Ocelot.DependencyInjection``
    | **Class**: `ServiceCollectionExtensions <https://github.com/ThreeMammals/Ocelot/blob/develop/src/Ocelot/DependencyInjection/ServiceCollectionExtensions.cs>`_

Based on the current implementations for the ``OcelotBuilder`` class, the ``AddOcelot`` method adds default ASP.NET services to DI container.
You could call another more extended ``AddOcelotUsingBuilder`` method while configuring services to build and use custom builder via an ``IMvcCoreBuilder`` interface object.

The AddOcelot method
^^^^^^^^^^^^^^^^^^^^

**Signatures**:

* ``IOcelotBuilder AddOcelot(this IServiceCollection services)``
* ``IOcelotBuilder AddOcelot(this IServiceCollection services, IConfiguration configuration)``

This ``IServiceCollection`` extension method adds default ASP.NET services and Ocelot application services with configuration injected implicitly or explicitly.
Note! The method adds **default** ASP.NET services required for Ocelot core in the `AddDefaultAspNetServices <#the-adddefaultaspnetservices-method>`_ method which plays the role of default builder.

In this scenario, you do nothing except calling the ``AddOcelot`` method which has been mentioned in feature chapters, if additional startup settings are required.
In this case you just reuse default settings to build Ocelot core. The alternative is ``AddOcelotUsingBuilder`` method, see the next section.

The AddOcelotUsingBuilder method
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

**Signatures**:

* ``IOcelotBuilder AddOcelotUsingBuilder(this IServiceCollection services, Func<IMvcCoreBuilder, Assembly, IMvcCoreBuilder> customBuilder)``
* ``IOcelotBuilder AddOcelotUsingBuilder(this IServiceCollection services, IConfiguration configuration, Func<IMvcCoreBuilder, Assembly, IMvcCoreBuilder> customBuilder)``

This ``IServiceCollection`` extension method adds Ocelot application services, and it *adds custom ASP.NET services* with configuration injected implicitly or explicitly.
Note! The method adds **custom** ASP.NET services required for Ocelot pipeline using custom builder (``customBuilder`` parameter).
It is highly recommended to read docs of the `AddDefaultAspNetServices <#the-adddefaultaspnetservices-method>`_ method, 
or even to review implementation to understand default ASP.NET services which are the minimal part of the gateway pipeline. 

In this custom scenario, you control everything during ASP.NET MVC pipeline building, and you provide custom settings to build Ocelot core.

The OcelotBuilder class
-----------------------

    **Source code**: `Ocelot.DependencyInjection.OcelotBuilder <https://github.com/ThreeMammals/Ocelot/blob/develop/src/Ocelot/DependencyInjection/OcelotBuilder.cs>`_

The ``OcelotBuilder`` class is the core of Ocelot which does the following:

- Contructs itself by single public constructor:
  ``public OcelotBuilder(IServiceCollection services, IConfiguration configurationRoot, Func<IMvcCoreBuilder, Assembly, IMvcCoreBuilder> customBuilder = null)``
- Initializes and stores public properties: **Services** (``IServiceCollection`` object), **Configuration** (``IConfiguration`` object) and **MvcCoreBuilder** (``IMvcCoreBuilder`` object)
- Adds **all application services** during construction phase over the ``Services`` property
- Adds ASP.NET services by builder using ``Func<IMvcCoreBuilder, Assembly, IMvcCoreBuilder>`` object in these 2 development scenarios:

  * by default builder (``AddDefaultAspNetServices`` method) if there is no ``customBuilder`` parameter provided
  * by custom builder with provided delegate object as the ``customBuilder`` parameter

- Adds (switches on/off) Ocelot features by:

  * ``AddSingletonDefinedAggregator`` and ``AddTransientDefinedAggregator`` methods
  * ``AddCustomLoadBalancer`` method
  * ``AddDelegatingHandler`` method
  * ``AddConfigPlaceholders`` method

The AddDefaultAspNetServices method
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

    **Class**: `Ocelot.DependencyInjection.OcelotBuilder <https://github.com/ThreeMammals/Ocelot/blob/develop/src/Ocelot/DependencyInjection/OcelotBuilder.cs>`_

Currently the method is protected and overriding is forbidden.
The role of the method is to inject required services via both ``IServiceCollection`` and ``IMvcCoreBuilder`` interfaces objects for the minimal part of the gateway pipeline.

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
And, the method is default builder of Ocelot pipeline while calling the `AddOcelot <#the-addocelot-method>`_ method.
As alternative, to "override" this default builder, you can design and reuse custom builder as a ``Func<IMvcCoreBuilder, Assembly, IMvcCoreBuilder>`` delegate object 
and pass it as parameter to the `AddOcelotUsingBuilder <#the-addocelotusingbuilder-method>`_ extension method.
It gives you full control on design and buiding of Ocelot pipeline, but be careful while designing your custom Ocelot pipeline as customizable ASP.NET MVC pipeline.

Warning! Most of services from minimal part of the pipeline should be reused, but only a few of services could be removed.

Warning!! The method above is called after adding required services of ASP.NET MVC pipeline building by 
`AddMvcCore <https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.mvccoreservicecollectionextensions.addmvccore>`_ method 
over the ``Services`` property in upper calling context. These services are absolute minimum core services for ASP.NET MVC pipeline.
They must be added to DI container always, and they are added implicitly before calling of the method by caller in upper context.
So, ``AddMvcCore`` creates an ``IMvcCoreBuilder`` object with its assignment to the ``MvcCoreBuilder`` property.
Finally, as a default builder, the method above receives ``IMvcCoreBuilder`` object being ready for further extensions.

The next section shows you an example of designing custom Ocelot pipeline by custom builder.

Custom Builder
--------------

**Goal**: Replace ``Newtonsoft.Json`` services with ``System.Text.Json`` services.

Problem
^^^^^^^

The default `AddOcelot <#the-addocelot-method>`_ method adds 
`Newtonsoft JSON <https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.newtonsoftjsonmvccorebuilderextensions.addnewtonsoftjson>`_ services 
by the ``AddNewtonsoftJson`` extension method in default builder (the `AddDefaultAspNetServices <#the-adddefaultaspnetservices-method>`_ method). 
The ``AddNewtonsoftJson`` method calling was introduced in old .NET and Ocelot releases which was necessary when Microsoft did not launch the ``System.Text.Json`` library, 
but now it affects normal use, so we have an intention to solve the problem.

Modern `JSON services <https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.mvccoremvccorebuilderextensions.addjsonoptions>`_ 
out of `the box <https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.mvccoremvccorebuilderextensions>`_
will help to configure JSON settings by the ``JsonSerializerOptions`` property for JSON formatters during (de)serialization.

Solution
^^^^^^^^

We have the following methods in ``Ocelot.DependencyInjection.ServiceCollectionExtensions`` class:

- ``IOcelotBuilder AddOcelotUsingBuilder(this IServiceCollection services, Func<IMvcCoreBuilder, Assembly, IMvcCoreBuilder> customBuilder)``
- ``IOcelotBuilder AddOcelotUsingBuilder(this IServiceCollection services, IConfiguration configuration, Func<IMvcCoreBuilder, Assembly, IMvcCoreBuilder> customBuilder)``

These method with custom builder allows you to use your any desired JSON library for (de)serialization.
But we are going to create custom ``MvcCoreBuilder`` with support of JSON services, such as ``System.Text.Json``.
To do that we need to call ``AddJsonOptions`` extension of the ``MvcCoreMvcCoreBuilderExtensions`` class 
(NuGet package: `Microsoft.AspNetCore.Mvc.Core <https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Core/>`_) in **Startup.cs**:

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
                // Add your custom builder
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

The sample code provides settings to render JSON as indented text rather than compressed plain JSON text without spaces.
This is just one common use case, and you can add additional services to the builder.

------------------------------------------------------------------

.. _di-configuration-overview:

Configuration Overview
----------------------

*Dependency Injection* for the :doc:`../features/configuration` feature in Ocelot is designed to extend and/or control **configuring** of Ocelot core before the stage of building ASP.NET MVC pipeline services.

Use :ref:`di-configuration-extensions` in the following ``ConfigureAppConfiguration`` method (**Program.cs** and **Startup.cs**) of your app (minimal web app) to configure Ocelot pipeline and services:

.. code-block:: csharp

    namespace Microsoft.AspNetCore.Hosting;

    public interface IWebHostBuilder
    {
        IWebHostBuilder ConfigureAppConfiguration(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate);
    }

.. _di-configuration-extensions:

IConfigurationBuilder extensions
--------------------------------

    | **Namespace**: ``Ocelot.DependencyInjection``
    | **Class**: `ConfigurationBuilderExtensions`_

The main one is the :ref:`di-configuration-addocelot` of the `ConfigurationBuilderExtensions`_ class, which has a list of overloaded versions with corresponding signatures.

The purpose of the method is to prepare everything before actually configuring with native extensions:
merge partial JSON files, select a merge type to save the merged JSON configuration data ``ToFile`` or ``ToMemory``,
and finally call the following native ``IConfigurationBuilder`` framework extensions:

* ``AddJsonFile`` finally adds primary configuration file (aka `ocelot.json`_) after the merge stage writing the file back **to the file system** using the ``ToFile`` merge type option, which is implicitly the default.
* ``AddJsonStream`` finally adds the JSON data of the primary configuration file as a UTF-8 stream after the merge stage **into memory** using the ``ToMemory`` merge type option.

.. _di-configuration-addocelot:

AddOcelot method
^^^^^^^^^^^^^^^^

**Signatures** of the most common versions:

.. code-block:: csharp

    IConfigurationBuilder AddOcelot(this IConfigurationBuilder builder, IWebHostEnvironment env);
    IConfigurationBuilder AddOcelot(this IConfigurationBuilder builder, string folder, IWebHostEnvironment env);

**Note!** These versions use implicit ``ToFile`` merge type to write `ocelot.json`_ back to a disk and finally they call the ``AddJsonFile`` extension.

**Signatures** of the versions to specify a ``MergeOcelotJson`` option:

.. code-block:: csharp

    IConfigurationBuilder AddOcelot(this IConfigurationBuilder builder, IWebHostEnvironment env, MergeOcelotJson mergeTo,
        string primaryConfigFile = null, string globalConfigFile = null, string environmentConfigFile = null, bool? optional = null, bool? reloadOnChange = null);
    IConfigurationBuilder AddOcelot(this IConfigurationBuilder builder, string folder, IWebHostEnvironment env, MergeOcelotJson mergeTo,
        string primaryConfigFile = null, string globalConfigFile = null, string environmentConfigFile = null, bool? optional = null, bool? reloadOnChange = null);

**Note!** These versions have optional arguments to specify the location of 3 main files that are involved in the merge operation.
In theory, these files can be located anywhere, but in practice it is better to keep the files in one folder.

**Signatures** of the versions to indicate the ``FileConfiguration`` object of a self-created out-of-the-box configuration: [#f1]_

.. code-block:: csharp

    IConfigurationBuilder AddOcelot(this IConfigurationBuilder builder, FileConfiguration fileConfiguration,
        string primaryConfigFile = null, bool? optional = null, bool? reloadOnChange = null);
    IConfigurationBuilder AddOcelot(this IConfigurationBuilder builder, FileConfiguration fileConfiguration, IWebHostEnvironment env, MergeOcelotJson mergeTo,
        string primaryConfigFile = null, string globalConfigFile = null, string environmentConfigFile = null, bool? optional = null, bool? reloadOnChange = null);

| **Note 1**. These versions have optional arguments to specify the location of 3 main files that are involved in the merge operation.
| **Note 2**. Your ``FileConfiguration`` object can be serialized/deserialized to/from anywhere: local or remote storages, Consul KV storage and even a database.
  Please, read more about this super useful feature in PR `1569`_ [#f1]_.

""""

.. [#f1] Dynamic :doc:`../features/configuration` feature was requested in issues `1228`_, `1235`_, and delivered by PR `1569`_ as a part of the version `20.0`_. Since then we extended it in PR `1227`_ and released it as a part of the version `23.2`_.

.. _ConfigurationBuilderExtensions: https://github.com/ThreeMammals/Ocelot/blob/develop/src/Ocelot/DependencyInjection/ConfigurationBuilderExtensions.cs
.. _ocelot.json: https://github.com/ThreeMammals/Ocelot/blob/main/test/Ocelot.ManualTest/ocelot.json
.. _1227: https://github.com/ThreeMammals/Ocelot/pull/1227
.. _1228: https://github.com/ThreeMammals/Ocelot/issues/1228
.. _1235: https://github.com/ThreeMammals/Ocelot/issues/1235
.. _1569: https://github.com/ThreeMammals/Ocelot/pull/1569
.. _20.0: https://github.com/ThreeMammals/Ocelot/releases/tag/20.0.0
.. _23.2: https://github.com/ThreeMammals/Ocelot/releases/tag/23.2.0
