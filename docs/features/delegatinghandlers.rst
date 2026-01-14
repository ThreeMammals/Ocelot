.. _ocelot.json: https://github.com/ThreeMammals/Ocelot/blob/main/samples/Basic/ocelot.json
.. _Program: https://github.com/ThreeMammals/Ocelot/blob/main/samples/Basic/Program.cs

Delegating Handlers
===================

    **MS Learn Documentation:**

    * `DelegatingHandler Class <https://learn.microsoft.com/en-us/dotnet/api/system.net.http.delegatinghandler>`_
    * `HTTP Message Handlers in ASP.NET Web API <https://learn.microsoft.com/en-us/aspnet/web-api/overview/advanced/http-message-handlers>`_
    * `HttpClient Message Handlers in ASP.NET Web API <https://learn.microsoft.com/en-us/aspnet/web-api/overview/advanced/httpclient-message-handlers>`_

Ocelot allows the user to add `delegating handlers <https://learn.microsoft.com/en-us/dotnet/api/system.net.http.delegatinghandler>`_ to the ``HttpClient`` transport. [#f1]_

Configuration
-------------

In order to utilize the :doc:`../features/delegatinghandlers` feature, you need to do the following three steps of configuration.

1. Create a class that can be used as a *delegating handler*: it must inherit from the ``DelegatingHandler`` class.
   We are going to register these handlers in the ASP.NET Core DI container, so you can inject any other services you have registered into the constructor of your handler.

   .. code-block:: csharp

    public class MyHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            // Do stuff before sending request, and optionally call the base handler...
            var response = await base.SendAsync(request, token);
            // Do post-processing of the response...
            return response;
        }
    }

2. You must add the handlers to the DI container in your `Program`_, as shown below:

   .. code-block:: csharp

    builder.Services
        .AddOcelot(builder.Configuration)
        .AddDelegatingHandler<MyHandler>()
        .AddDelegatingHandler<MyHandlerTwo>();

   Both of these ``AddDelegatingHandler{T}`` methods have an optional parameter called ``global``, which is set to ``false``.
   If it is ``false``, then the intent of the *delegating handler* is to be applied to specific routes via `ocelot.json`_ (see step 3).
   If it is set to ``true``, then it becomes a global handler and will be applied to all routes, as shown below:

   .. code-block:: csharp

    builder.Services
        .AddOcelot(builder.Configuration)
        .AddDelegatingHandler<MyGlobalHandler>(true);  // it's global!

.. _break: http://break.do

    **Note 1**: The generic ``AddDelegatingHandler<T>(bool)`` method has another overloaded non-generic one with the ``Type`` parameter: ``AddDelegatingHandler(Type, bool)``.
    Thus, here is an alternative to set it up:

    .. code-block:: csharp

        builder.Services
            .AddOcelot(builder.Configuration)
            .AddDelegatingHandler(typeof(MyHandler)) // for selected routes only
            .AddDelegatingHandler(typeof(MyGlobalHandler), true); // it's global!

    **Note 2**: Both versions of the methods add transient services to the DI container. It is recommended to utilize the generic version.

3. If you want route-specific *delegating handlers* or to order your specific and/or global *delegating handlers* (more on this in the :ref:`dh-execution-order` section), then you must add the following to the specific route in `ocelot.json`_.
   The names in the array must match the class names of your *delegating handlers* for Ocelot to match them together:

   .. code-block:: json

     "DelegatingHandlers": [ "MyHandlerTwo", "MyHandler" ]

.. _dh-execution-order:

Execution Order
---------------

You can have as many *delegating handlers* as you want, and they are run in the following order:

1. Any globals that are left in the order they were added to services and are not in the ``DelegatingHandlers`` option array from `ocelot.json`_.
2. Any non-global *delegating handlers* plus any globals that were in the ``DelegatingHandlers`` option array from `ocelot.json`_, ordered as they are in the ``DelegatingHandlers`` array.
3. Tracing *delegating handler*, if enabled (refer to the :doc:`../features/tracing` chapter).
4. Quality of Service *delegating handler*, if enabled (refer to the :doc:`../features/qualityofservice` chapter).
5. The ``HttpClient`` sends the ``HttpRequestMessage``.

Hopefully, other people will find this feature useful!

""""

.. [#f1] This feature was requested in issue `208`_, and the team decided that it would be useful in various ways, releasing it in version `3.0.3`_. Since then, we extended it in issue `264`_ and released it in version `5.0.0`_.

.. _208: https://github.com/ThreeMammals/Ocelot/issues/208
.. _264: https://github.com/ThreeMammals/Ocelot/issues/264

.. _3.0.3: https://github.com/ThreeMammals/Ocelot/releases/tag/3.0.3
.. _5.0.0: https://github.com/ThreeMammals/Ocelot/releases/tag/5.0.0
