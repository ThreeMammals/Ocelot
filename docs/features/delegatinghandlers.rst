Delegating Handers
==================

Ocelot allows the user to add delegating handlers to the HttpClient transport. This feature was requested `GitHub #208 <https://github.com/TomPallister/Ocelot/issues/208>`_ and I decided that it was going to be useful in various ways.

Usage
^^^^^

In order to add delegating handlers to the HttpClient transport you need to do the following.

This will register the Handlers as singletons. Because Ocelot caches the HttpClient for the downstream services to avoid
socket exhaustion (well known http client issue) you can only register singleton handlers.

.. code-block:: csharp

    services.AddOcelot()
            .AddDelegatingHandler<FakeHandler>()
            .AddDelegatingHandler<FakeHandlerTwo>()

You can have as many DelegatingHandlers as you want and they are run in a first in first out order. If you are using Ocelot's QoS functionality then that will always be run after your last delegating handler. If you are also registering handlers in DI these will be
run first.

In order to create a class that can be used a delegating handler it must look as follows

.. code-block:: csharp

    public class FakeHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            //do stuff and optionally call the base handler..
            return await base.SendAsync(request, cancellationToken);
        }
    }

Hopefully other people will find this feature useful!