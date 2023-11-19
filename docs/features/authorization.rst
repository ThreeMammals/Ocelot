Authorization
=============

Ocelot supports claims based authorization which is run post authentication.
This means if you have a route you want to authorize, you can add the following to your Route configuration:

.. code-block:: json

    "RouteClaimsRequirement": {
        "UserType": "registered"
    }

In this example, when the ``AuthorizationMiddleware`` is called, Ocelot will check to see if the user has the claim type **UserType** and if the value of that claim is ``"registered"``.
If it isn't then the user will not be authorized and the response will be `403 Forbidden <https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/403>`_.

Authorization Middleware
------------------------

The `AuthorizationMiddleware <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20AuthorizationMiddleware&type=code>`_ is built-in into Ocelot pipeline.

  | Previous private: ``ClaimsToClaimsMiddleware``
  | Previous public: ``PreAuthorizationMiddleware``
  | **This**: ``AuthorizationMiddleware``
  | Next private: ``ClaimsToHeadersMiddleware``
  | Next public: ``PreQueryStringBuilderMiddleware``

.. role::  htm(raw)
    :format: html

So, the closest middlewares are in order of calling:

``ClaimsToClaimsMiddleware`` :htm:`&rarr;` ``PreAuthorizationMiddleware`` :htm:`&rarr;` **AuthorizationMiddleware** :htm:`&rarr;` ``ClaimsToHeadersMiddleware`` :htm:`&rarr;` ``PreQueryStringBuilderMiddleware``

As you may know from the :doc:`../features/middlewareinjection` section, the Authorization middleware can be overridden like this:

.. code-block:: csharp

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        var configuration = new OcelotPipelineConfiguration
        {
            AuthorizationMiddleware = async (context, next) =>
            {
                await next.Invoke();
            }
        };
        app.UseOcelot(configuration);
    }

Do this in very rare cases, because overriding Authorization middleware means you will lose claims & scopes authorizer through the **RouteClaimsRequirement** property of the route. 
Another option is preparing before the actual authorization in ``PreAuthorizationMiddleware`` which is public and open to overriding.
