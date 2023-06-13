Dependency Injection
====================

The default ``AddOcelot`` method adds ``.AddNewtonsoftJson()``, which was necessary when Microsoft did not launch the ``System.Text.Json`` library, 
but now it affects normal use, so this PR is mainly to solve the problem problem.

Added the following methods in ``Ocelot.DependencyInjection.ServiceCollectionExtensions``
- ``AddOcelotWithCustomMvcCoreBuilder(this IServiceCollection services, Func<IMvcCoreBuilder, Assembly, IMvcCoreBuilder> customMvcCoreBuilder)``
- ``AddOcelotWithCustomMvcCoreBuilder(this IServiceCollection services, IConfiguration configuration, Func<IMvcCoreBuilder, Assembly, IMvcCoreBuilder> customMvcCoreBuilder)``


Proposed Changes
----------------

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

The AddOcelot method
--------------------

Based on the current dependency injection implementations for the ``OcelotBuilder`` class, the ``AddOcelot`` method adds default ASP.NET services to DI-container.
You could call another more extended ``AddOcelotUsingBuilder`` method while configuring services to build and use custom builder via an ``IMvcCoreBuilder`` interface object.

The AddOcelotUsingBuilder method
--------------------------------

C# method signature:
``public static IOcelotBuilder AddOcelotUsingBuilder(this IServiceCollection services, Func<IMvcCoreBuilder, Assembly, IMvcCoreBuilder> customBuilder)``
