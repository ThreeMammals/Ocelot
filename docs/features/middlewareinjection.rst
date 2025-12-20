.. _Program: https://github.com/ThreeMammals/Ocelot/blob/main/samples/Metadata/Program.cs

Middleware Injection
====================


When setting up Ocelot in your `Program`_, you can provide additional middleware and override it with your custom middlewares. This is done as follows:

.. code-block:: csharp

    // Set it up: configuration, services, etc.
    // Middleware setup is only possible during the final stage of app configuration and execution
    var app = builder.Build();
    var pipeline = new OcelotPipelineConfiguration
    {
        PreErrorResponderMiddleware = async (context, next) =>
        {
            await next.Invoke();
        }
    };
    await app.UseOcelot(pipeline);
    await app.RunAsync();

In the example above, the provided function will run before the first piece of Ocelot middleware.
This allows users to supply any behavior they want before and after the Ocelot pipeline has run.

.. warning::
    Be cautious, as this means you can break everything — use at your own risk or pleasure!
    If you notice any exceptions or strange behavior in your middleware pipeline and are using any of the following, remove your custom middlewares and try again.

.. _mi-ocelotpipelineconfiguration-class:

``OcelotPipelineConfiguration`` Class
-------------------------------------

.. _OcelotPipelineConfiguration: https://github.com/ThreeMammals/Ocelot/blob/main/src/Ocelot/Middleware/OcelotPipelineConfiguration.cs

  Class: `OcelotPipelineConfiguration`_

The user can set middleware-functions aka custom user's middleware against the following:

.. list-table::
    :widths: 50 50
    :header-rows: 1

    * - *Middleware*
      - *Description*
    * - | ``PreErrorResponderMiddleware``
        | Prev: ``ExceptionHandlerMiddleware``
        | Next: ``ResponderMiddleware``
      - This is called after the global error-handling middleware, so any code before calling ``next.Invoke`` is the next action executed in the Ocelot pipeline.
        Any code after ``next.Invoke`` is the final action executed in the Ocelot pipeline before reaching the global error handler.
    * - | ``ResponderMiddleware``
        | Prev: ``PreErrorResponderMiddleware``
        | Next: ``DownstreamRouteFinderMiddleware``
      - This allows the user to completely override Ocelot's `ResponderMiddleware <https://github.com/ThreeMammals/Ocelot/blob/develop/src/Ocelot/Responder/Middleware/ResponderMiddleware.cs>`_. :sup:`1`
    * - | ``PreAuthenticationMiddleware``
        | Prev: ``RequestIdMiddleware``
        | Next: ``AuthenticationMiddleware``
      - This allows the user to run any extra authentication before the Ocelot authentication kicks in.
    * - | ``AuthenticationMiddleware``
        | Prev: ``PreAuthenticationMiddleware``
        | Next: ``ClaimsToClaimsMiddleware``
      - This allows the user to completely override Ocelot's `AuthenticationMiddleware <https://github.com/ThreeMammals/Ocelot/blob/develop/src/Ocelot/Authentication/Middleware/AuthenticationMiddleware.cs>`_. :sup:`1`
    * - | ``PreAuthorizationMiddleware``
        | Prev: ``ClaimsToClaimsMiddleware``
        | Next: ``AuthorizationMiddleware``
      - This allows the user to run any extra authorization before the Ocelot authorization kicks in.
    * - | ``AuthorizationMiddleware``
        | Prev: ``PreAuthorizationMiddleware``
        | Next: ``ClaimsToHeadersMiddleware``
      - This allows the user to completely override Ocelot's `AuthorizationMiddleware <https://github.com/ThreeMammals/Ocelot/blob/develop/src/Ocelot/Authorization/Middleware/AuthorizationMiddleware.cs>`_. :sup:`1`
    * - | ``ClaimsToHeadersMiddleware``
        | Prev: ``AuthorizationMiddleware``
        | Next: ``PreQueryStringBuilderMiddleware``
      - This allows the user to completely override Ocelot's `ClaimsToHeadersMiddleware <https://github.com/ThreeMammals/Ocelot/blob/develop/src/Ocelot/Headers/Middleware/ClaimsToHeadersMiddleware.cs>`_. :sup:`1`
    * - | ``PreQueryStringBuilderMiddleware``
        | Prev: ``ClaimsToHeadersMiddleware``
        | Next: ``ClaimsToQueryStringMiddleware``
      - This allows the user to implement own query string manipulation logic.

Obviously, you can add the mentioned Ocelot middleware overrides as normal before the call to ``app.UseOcelot``.
They cannot be added afterward because Ocelot does not invoke subsequent middleware overrides based on the specified middleware configuration.
As a result, the next-called middleware **will not** affect the Ocelot configuration.

.. warning::
  :sup:`1` Use the mentioned middleware overrides with caution! Overridden middleware removes the default implementation.
  If you encounter any exceptions or strange behavior in your middleware pipeline, remove the overridden middleware and try again.

.. _mi-ocelot-pipeline-builder:

Ocelot Pipeline Builder
-----------------------

  | Class: ``Ocelot.Middleware.OcelotPipelineExtensions``
  | Method: ``BuildOcelotPipeline(IApplicationBuilder, OcelotPipelineConfiguration)``

The Ocelot pipeline is part of the entire `ASP.NET Core Middleware <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/>`_ conveyor, also known as the app pipeline.
The `BuildOcelotPipeline <https://github.com/search?q=repo%3AThreeMammals%2FOcelot+BuildOcelotPipeline+path%3A%2F%5Esrc%5C%2FOcelot%5C%2FMiddleware%5C%2F%2F&type=code>`_ method encapsulates the Ocelot pipeline.
The last middleware in the ``BuildOcelotPipeline`` method is ``HttpRequesterMiddleware``, which calls the next middleware if it is added to the pipeline.

The internal `HttpRequesterMiddleware <https://github.com/search?q=repo%3AThreeMammals%2FOcelot+HttpRequesterMiddleware+path%3A%2F%5Esrc%5C%2FOcelot%5C%2F%2F&type=code>`_ is part of the pipeline,
but it is private and cannot be overridden since this middleware is not included in the list of `user-accessible public middlewares <https://github.com/ThreeMammals/Ocelot/blob/main/src/Ocelot/Middleware/OcelotPipelineConfiguration.cs>`_ that can be overridden.
Therefore, it is the `final middleware <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20app.UseMiddleware%3CHttpRequesterMiddleware%3E()&type=code>`_ in both the Ocelot and ASP.NET pipelines, and it handles non-user operations.
The last user (public) middleware that can be overridden is `PreQueryStringBuilderMiddleware <https://github.com/search?q=repo%3AThreeMammals%2FOcelot+PreQueryStringBuilderMiddleware+language%3AC%23&type=code&l=C%23>`_, which is read from the pipeline configuration object.
For more details, see the previous :ref:`mi-ocelotpipelineconfiguration-class` section.

To understand the actual order of middleware execution, here is a quick list of them, with an asterisk (*) marking the ones that can be overridden:

1. ``ConfigurationMiddleware``
2. ``ExceptionHandlerMiddleware``
3. ``PreErrorResponderMiddleware``\*
4. ``ResponderMiddleware``\*
5. ``DownstreamRouteFinderMiddleware``
6. ``MultiplexingMiddleware``
7. ``SecurityMiddleware``
8. ``HttpHeadersTransformationMiddleware``
9. ``DownstreamRequestInitialiserMiddleware``
10. ``RateLimitingMiddleware``
11. ``RequestIdMiddleware``
12. ``PreAuthenticationMiddleware``\*
13. ``AuthenticationMiddleware``\*
14. ``ClaimsToClaimsMiddleware``
15. ``PreAuthorizationMiddleware``\*
16. ``AuthorizationMiddleware``\*
17. ``ClaimsToHeadersMiddleware``\*
18. ``PreQueryStringBuilderMiddleware``\*
19. ``ClaimsToQueryStringMiddleware``
20. ``ClaimsToDownstreamPathMiddleware``
21. ``LoadBalancingMiddleware``
22. ``DownstreamUrlCreatorMiddleware``
23. ``OutputCacheMiddleware``
24. ``HttpRequesterMiddleware``

Considering that ``PreQueryStringBuilderMiddleware`` and ``HttpRequesterMiddleware`` are the final user and system middleware, there are no other middleware components in the pipeline.
However, you can still extend the ASP.NET pipeline, as demonstrated in the following code:

.. code-block:: csharp

    await app.UseOcelot();
    app.UseMiddleware<MyCustomMiddleware>();

However, we do not recommend adding custom middleware before or after calling ``UseOcelot()`` because it affects the stability of the entire pipeline and has not been tested.
This type of custom pipeline building falls outside the Ocelot pipeline model, and the quality of the solution is your responsibility.

Finally, do not confuse the distinction between system (private, non-overridden) and user (public, overridden) middleware.
Private middleware is hidden and cannot be overridden, but the entire ASP.NET pipeline can still be extended.
The public middleware of the :ref:`mi-ocelotpipelineconfiguration-class` is fully customizable and can be overridden.

Roadmap
-------

The community has shown interest in adding more overridden middleware.
One such request is pull request `1497 <https://github.com/ThreeMammals/Ocelot/pull/1497>`_, which may possibly be included in an upcoming release.

In any case, if the current overridden middleware does not provide enough pipeline flexibility, you can open a new topic in the `Discussions <https://github.com/ThreeMammals/Ocelot/discussions>`_ of the repository. |octocat|

.. |octocat| image:: https://github.githubassets.com/images/icons/emoji/octocat.png
  :alt: octocat
  :height: 25
  :class: img-valign-middle
