Authorization
=============
.. contents:: Table of Contents
   :depth: 2
   :local:

Ocelot supports claims based authorization which is run post authentication.
This means if you have a route you want to authorize, you can add the following to your route configuration:

.. code-block:: json

  "RouteClaimsRequirement": {
    "UserType": "registered"
  }

In this example, when the :ref:`authorization-middleware` is called, Ocelot will check to see if the user has the claim type ``UserType`` and if the value of that claim is ``"registered"``.
If it isn't then the user will not be authorized and the response will be `403 Forbidden <https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/403>`_.

.. _authorization-middleware:

Authorization Middleware
------------------------

The `AuthorizationMiddleware <https://github.com/search?q=repo%3AThreeMammals%2FOcelot+AuthorizationMiddleware+language%3AC%23&type=code&l=C%23>`_ is built-in into Ocelot pipeline.

  | Previous private: ``ClaimsToClaimsMiddleware``
  | Previous public: ``PreAuthorizationMiddleware``
  | **This**: ``AuthorizationMiddleware``
  | Next private: ``ClaimsToHeadersMiddleware``
  | Next public: ``PreQueryStringBuilderMiddleware``

.. role::  htm(raw)
    :format: html

So, the closest middlewares are in order of calling:

``ClaimsToClaimsMiddleware`` :htm:`&rarr;` ``PreAuthorizationMiddleware`` :htm:`&rarr;` **AuthorizationMiddleware** :htm:`&rarr;` ``ClaimsToHeadersMiddleware`` :htm:`&rarr;` ``PreQueryStringBuilderMiddleware``

As you may know from the :doc:`../features/middlewareinjection` chapter, the Authorization middleware can be overridden like this:

.. code-block:: csharp

    var app = builder.Build();
    await app.UseOcelot(new OcelotPipelineConfiguration
    {
        AuthorizationMiddleware = async (context, next) =>
        {
            await next.Invoke();
        }
    });
    await app.RunAsync();

**Note!** Do this in very rare cases, because overriding the Authorization middleware means you will lose claims and scopes authorizer through the ``RouteClaimsRequirement`` property of the route.
Another option is preparing before the actual authorization in ``PreAuthorizationMiddleware``, which is public and open to overriding.

.. code-block:: csharp

    await app.UseOcelot(new OcelotPipelineConfiguration
    {
        PreAuthorizationMiddleware = async (context, next) =>
        {
            // Do whatever you want here
            await next.Invoke(); // next is AuthorizationMiddleware
        }
    });

Pass Through Authorization
--------------------------

Ocelot supports "pass-through" authorization for downstream requests by allowing the downstream HTTP handler to use the host/process credentials.
This is controlled by the ``UseCredentials`` option inside :ref:`config-http-handler-options`.

When to use pass-through authorization
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

- Useful when Ocelot and downstream services are deployed in the same trusted network (for example, an intranet or Windows domain environment), and downstream services expect Integrated/Windows authentication (Negotiate/NTLM).
- Useful for scenarios where you don't want to re-issue tokens from the gateway but instead want the gateway to let the underlying transport layer authenticate using the gateway host identity.

Strong warning - security implications
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Pass-through authorization effectively allows the gateway's process identity to authenticate to downstream services. This is safe only when:

* Ocelot and downstream services are in a trusted network under your control (e.g., a private corporate network, a Docker network).
* You understand the credential flow (Kerberos / NTLM) and have hard requirements for audience and scope.
* You **do not** expose downstream services directly to untrusted networks while using this mode.

Never enable pass-through credentials for arbitrary public downstream services.
Doing so can inadvertently leak host credentials or allow downstream services to authenticate on behalf of the gateway in ways you may not intend.

How it is applied in Ocelot
^^^^^^^^^^^^^^^^^^^^^^^^^^^

The option is applied when Ocelot constructs the ``SocketsHttpHandler`` for downstream requests.
The handler is created by the :class:`MessageInvokerPool` implementation (``MessageInvokerPool.CreateHandler`` overridable method).
If ``UseCredentials`` is enabled for a route (or for a global group of routes), Ocelot will configure ``SocketsHttpHandler.UseCredentials`` (or set ``handler.Credentials``) for the handler used to talk to that downstream service.

Overriding credentials creation
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

To customize credential creation (for example, to create credentials per-route based on route metadata or to implement a different credential strategy), override the ``CreateHandler`` method of :class:`MessageInvokerPool`.

Example - set ``UseCredentials`` or custom ``NetworkCredential``:

.. code-block:: csharp

    using System.Net;
    using System.Net.Http;
    using Ocelot.Configuration;
    using Ocelot.Logging;
    using Ocelot.Requester;

    public class WindowsAuthMessageInvokerPool : MessageInvokerPool
    {
        public WindowsAuthMessageInvokerPool(IDelegatingHandlerFactory handlerFactory, IOcelotLoggerFactory loggerFactory)
            : base(handlerFactory, loggerFactory)
        {
        }

        protected override SocketsHttpHandler CreateHandler(DownstreamRoute route)
        {
            var handler = base.CreateHandler(route);

            // Enable pass-through (host) credentials when configured
            if (route.HttpHandlerOptions?.UseCredentials == true)
            {
                handler.UseCredentials = true;
            }

            // Alternatively, set explicit credentials (example retrieving from metadata)
            // var domain = route.GetMetadata<string>("auth/domain");
            // var user = route.GetMetadata<string>("auth/user");
            // var pwd = route.GetMetadata<string>("auth/password");
            // if (!string.IsNullOrEmpty(user))
            // {
            //     handler.Credentials = new NetworkCredential(user, pwd, domain);
            // }

            return handler;
        }
    }

See also
^^^^^^^^

- Example tests and acceptance coverage for Windows auth are included in PR `#1521 <https://github.com/ThreeMammals/Ocelot/pull/1521>`_
  (see `WindowsAuthTests <https://github.com/search?q=repo%3AThreeMammals%2FOcelot+WindowsAuthTests&type=code>`_ in that PR for a usage pattern and tests).
- Additional security guidance and when to use route-level overrides are in :doc:`../features/authentication` (Pass Through Authentication).
