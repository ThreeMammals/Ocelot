Delegating Handlers
===================

Ocelot allows the user to add delegating handlers to the ``HttpClient`` transport.
This feature was requested by `issue 208 <https://github.com/ThreeMammals/Ocelot/issues/208>`_ and the team decided that it was going to be useful in various ways.
Since then we extended it in `issue 264 <https://github.com/ThreeMammals/Ocelot/issues/264>`_.

How to Use
----------

In order to add delegating handlers to the ``HttpClient`` transport you need to do two main things.

**First**, in order to create a class that can be used a delegating handler it must look as follows.
We are going to register these handlers in the ASP.NET Core DI container, so you can inject any other services you have registered into the constructor of your handler.

.. code-block:: csharp

    public class FakeHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            // Do stuff and optionally call the base handler...
            return await base.SendAsync(request, token);
        }
    }

**Second**, you must add the handlers to DI container like below:

.. code-block:: csharp

    ConfigureServices(s => s
        .AddOcelot()
        .AddDelegatingHandler<FakeHandler>()
        .AddDelegatingHandler<FakeHandlerTwo>()
    )

Both of these ``AddDelegatingHandler`` methods have a default parameter called global which is set to ``false``.
If it is ``false`` then the intent of the *Delegating Handler* is to be applied to specific Routes via **ocelot.json** (more on that later).
If it is set to ``true`` then it becomes a global handler and will be applied to all Routes, as below:

.. code-block:: csharp

    services.AddOcelot()
        .AddDelegatingHandler<FakeHandler>(true)

**Finally**, if you want Route specific *Delegating Handlers* or to order your specific and (or) global (more on this later) *Delegating Handlers* then you must add the following to the specific Route in **ocelot.json**.
The names in the array must match the class names of your *Delegating Handlers* for Ocelot to match them together:

.. code-block:: json

  "DelegatingHandlers": [
    "FakeHandlerTwo",
    "FakeHandler"
  ]

Order of Execution
------------------

You can have as many *Delegating Handlers* as you want and they are run in the following order:

1. Any globals that are left in the order they were added to services and are not in the **DelegatingHandlers** array from **ocelot.json**.
2. Any non global *Delegating Handlers* plus any globals that were in the **DelegatingHandlers** array from **ocelot.json** ordered as they are in the **DelegatingHandlers** array.
3. Tracing *Delegating Handler*, if enabled (see :doc:`../features/tracing` docs).
4. Quality of Service *Delegating Handler*, if enabled (see :doc:`../features/qualityofservice` docs).
5. The ``HttpClient`` sends the ``HttpRequestMessage``.

Hopefully other people will find this feature useful!
