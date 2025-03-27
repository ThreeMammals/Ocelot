Authorization
=============

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
