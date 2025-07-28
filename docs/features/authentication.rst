.. _scheme: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/#authentication-scheme
.. _Program: https://github.com/ThreeMammals/Ocelot/blob/main/samples/Basic/Program.cs

Authentication
==============

In order to authenticate routes and subsequently use any of Ocelot's claims based features such as authorization or modifying the request with values from the token,
users must register authentication services in their `Program`_ as usual but they provide a `scheme`_ 
(authentication provider key) with each registration e.g.

.. code-block:: csharp

    var AuthenticationProviderKey = "MyKey";
    builder.Services
        .AddAuthentication()
        .AddJwtBearer(AuthenticationProviderKey, options =>
        {
            // authentication setup via options initialization
        });

In this example ``MyKey`` is the `scheme`_ that this provider has been registered with.
We then map this to a route in the configuration using the following `AuthenticationOptions <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20AuthenticationOptions&type=code>`_ properties:

* ``AuthenticationProviderKey`` is a string object, obsolete [#f1]_. This is legacy definition when you define :ref:`authentication-scheme`.
* ``AuthenticationProviderKeys`` is an array of strings, the recommended definition of :ref:`authentication-multiple` feature.

Configuration
-------------

If you want to configure ``AuthenticationOptions`` the same for all Routes, do it in GlobalConfiguration the same way as for Route. If there are ``AuthenticationOptions`` configured both for GlobalConfiguration and Route (``AuthenticationProviderKey`` or ``AuthenticationProviderKeys`` is set), the Route section has priority.

If you want to exclude route from global ``AuthenticationOptions``, you can do that by setting ``AllowAnonymous`` to true in the route ``AuthenticationOptions`` - then this route will not be authenticated.

In the following example:

* the first route will be authenticated with MyGlobalKey provider key, 
* the second one - with MyKey provider key,
* the others will not be authenticated.

.. code-block:: json

  "Routes": [
    {
      "AuthenticationOptions": {},
      // ...
    },
    {
      "AuthenticationOptions": {
        "AuthenticationProviderKeys": [ "MyKey" ],
        "AllowedScopes": [ "Bob" ]
      },
      // ...
    },
    {
      "AuthenticationOptions": {
        "AllowAnonymous": true
      },
      // ...
    }
  ],
  "GlobalConfiguration": {
    "BaseUrl": "http://ocelot.net",
    "AuthenticationOptions": {
      "AuthenticationProviderKeys": [ "MyGlobalKey" ],
      "AllowedScopes": [ "Admin" ]
    }
  }

.. _break: http://break.do

  **Note** If there are global ``AuthenticationProviderKeys`` (when ``AuthenticationProviderKeys`` are not configured for route explicitly),
  it uses also global ``AllowedScopes``, even if ``AllowedScopes`` is configured for the route additionally.

.. _authentication-scheme:

Single Authentication Scheme [#f1]_
-----------------------------------

    | Property: ``AuthenticationOptions.AuthenticationProviderKey``

We map authentication provider to a Route in the configuration e.g.

.. code-block:: json

  "AuthenticationOptions": {
    "AuthenticationProviderKey": "MyKey",
    "AllowedScopes": []
  }

When Ocelot runs it will look at this routes ``AuthenticationProviderKey`` and check that there is an authentication provider registered with the given key.
If there isn't then Ocelot will not start up. If there is then the route will use that provider when it executes.

If a route is authenticated, Ocelot will invoke whatever scheme is associated with it while executing the authentication middleware.
If the request fails authentication, Ocelot returns a HTTP status code `401 Unauthorized <https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/401>`_.

.. _authentication-multiple:

Multiple Authentication Schemes [#f2]_
--------------------------------------

    | Property: ``AuthenticationOptions.AuthenticationProviderKeys``

In the real world of ASP.NET Core, apps may need to support multiple types of authentication by a single Ocelot app instance.
To register `multiple authentication schemes`_ (`authentication provider keys <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20AuthenticationProviderKey&type=code>`_) for each appropriate authentication provider,
use and develop this abstract configuration of two or more schemes:

.. code-block:: csharp

    var DefaultScheme = JwtBearerDefaults.AuthenticationScheme; // Bearer
    builder.Services
        .AddAuthentication()
        .AddJwtBearer(DefaultScheme, options => { /* JWT setup */ })
        // AddJwtBearer, AddCookie, AddIdentityServerAuthentication etc. 
        .AddMyProvider("MyKey", options => { /* Custom auth setup */ });

In this example, the ``MyKey`` and ``Bearer`` schemes represent the keys with which these providers were registered.
We then map these schemes to a route in the configuration as shown below.

.. code-block:: json

  "AuthenticationOptions": {
    "AuthenticationProviderKeys": [ "Bearer", "MyKey" ] // The order matters!
    "AllowedScopes": []
  }

Afterward, Ocelot applies all steps that are specified for ``AuthenticationProviderKey`` as :ref:`authentication-scheme`.

    **Note** that the order of the keys in an array definition does matter! We use a "First One Wins" authentication strategy.

Finally, we would say that registering providers, initializing options, and forwarding authentication artifacts can be a "real" coding challenge.
If you're stuck or don't know what to do, just find inspiration in our `acceptance tests <https://github.com/search?q=repo%3AThreeMammals%2FOcelot+MultipleAuthSchemesFeatureTests+language%3AC%23&type=code&l=C%23>`_
(currently for `IdentityServer4 <https://identityserver4.readthedocs.io/>`_ only) [#f3]_.

JWT Tokens
----------

If you want to authenticate using JWT tokens maybe from a provider like `Auth0 <https://auth0.com/>`_, you can register your authentication middleware as normal e.g.

.. code-block:: csharp

    var AuthenticationProviderKey = "MyKey";
    builder.Services
        .AddAuthentication()
        .AddJwtBearer(AuthenticationProviderKey, options =>
        {
            options.Authority = "test";
            options.Audience = "test";
        });
    builder.Services
        .AddOcelot(builder.Configuration);

Then map the authentication provider key to a route in your configuration e.g.

.. code-block:: json

  "AuthenticationOptions": {
    "AuthenticationProviderKeys": [ "MyKey" ],
    "AllowedScopes": []
  }

**JWT Tokens Docs**

    * Microsoft Learn: `Authentication and authorization in minimal APIs <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/security>`_
    * Andrew Lock | .NET Escapades: `A look behind the JWT bearer authentication middleware in ASP.NET Core <https://andrewlock.net/a-look-behind-the-jwt-bearer-authentication-middleware-in-asp-net-core/>`_

.. _authentication-identity-server:

Identity Server Bearer Tokens
-----------------------------

In order to use `IdentityServer <https://github.com/IdentityServer>`_ bearer tokens, register your IdentityServer services as usual in `Program`_ with a `scheme`_ (key).
If you don't understand how to do this, please consult the IdentityServer `documentation <https://identityserver4.readthedocs.io/>`_.

.. code-block:: csharp

    var AuthenticationProviderKey = "MyKey";
    Action<JwtBearerOptions> options = o =>
    {
        o.Authority = "https://whereyouridentityserverlives.com";
        // ...
    };
    builder.Services
        .AddAuthentication()
        .AddJwtBearer(AuthenticationProviderKey, options);
    builder.Services
        .AddOcelot(builder.Configuration);

Then map the authentication provider key to a route in your configuration e.g.

.. code-block:: json

  "AuthenticationOptions": {
    "AuthenticationProviderKeys": [ "MyKey" ],
    "AllowedScopes": []
  }

Auth0 by Okta
-------------

Yet another identity provider by `Okta <https://www.okta.com/>`_, see `Auth0 Developer Resources <https://developer.auth0.com/>`_.

Add the following, at minimum, to your startup `Program`_:

.. code-block:: csharp

    var OktaProviderKey = "MyKey";
    builder.Services
        .AddAuthentication()
        .AddJwtBearer(OktaProviderKey, o =>
        {
            var conf = builder.Configuration;
            o.Audience = conf["Authentication:Okta:Audience"]; // Okta Authorization server Audience
            o.Authority = conf["Authentication:Okta:Server"]; // Okta Authorization Issuer URI URL e.g. https://{subdomain}.okta.com/oauth2/{authidentifier}
        });
    builder.Services
        .AddOcelot(builder.Configuration);

    var app = builder.Build();
    await app
        .UseAuthentication()
        .UseOcelot();
    await app.RunAsync();

In order to get Ocelot to view the scope claim from Okta properly, you have to add the following to map the default Okta ``scp`` claim to ``scope``:

.. code-block:: csharp

    // Map Okta "scp" to "scope" claims instead of http://schemas.microsoft.com/identity/claims/scope to allow Ocelot to read/verify them
    JsonWebTokenHandler.DefaultInboundClaimTypeMap.Remove("scp");
    JsonWebTokenHandler.DefaultInboundClaimTypeMap.Add("scp", "scope");

**Okta Notes**

    1. Issue `446`_ contains some code and examples that might help with Okta integration.
    2. Here is documentation for better clarity on claims mapping: `Mapping, customizing, and transforming claims in ASP.NET Core`_.
    3. It is highly advisable to read and understand the :ref:`authentication-warning` related to the critical changes in authentication when utilizing .NET 8. [#f4]_

Allowed Scopes
--------------

If you add scopes to ``AllowedScopes``, Ocelot will get all the user claims (from the token) of the type scope and make sure that the user has at least one of the scopes in the list.

This is a way to restrict access to a route on a per scope basis.

.. _authentication-warning:

Warning
-------

.NET 8 introduced a breaking change [#f4]_ where ``JwtSecurityToken`` was replaced with ``JsonWebToken`` to enhance performance and reliability.
Consequently, their handlers were changed ``JwtSecurityTokenHandler`` to ``JsonWebTokenHandler``.
For versions prior to .NET 8, use the previous classes.

Links
-----

* Microsoft Learn: `Overview of ASP.NET Core authentication <https://learn.microsoft.com/en-us/aspnet/core/security/authentication/>`_
* Microsoft Learn: `Authorize with a specific scheme in ASP.NET Core <https://learn.microsoft.com/en-us/aspnet/core/security/authorization/limitingidentitybyscheme>`_
* Microsoft Learn: `Policy schemes in ASP.NET Core <https://learn.microsoft.com/en-us/aspnet/core/security/authentication/policyschemes>`_
* Microsoft Learn: `Mapping, customizing, and transforming claims in ASP.NET Core`_
* Microsoft .NET Blog: `ASP.NET Core Authentication with IdentityServer4 <https://devblogs.microsoft.com/dotnet/asp-net-core-authentication-with-identityserver4/>`_

Future
------

We invite you to add more examples if you have integrated with other identity providers and the integration solution is working.
Please open a "`Show and tell <https://github.com/ThreeMammals/Ocelot/discussions/categories/show-and-tell>`_" discussion in the repository.

""""

.. [#f1] ":ref:`authentication-scheme`" feature has been an Ocelot artifact for ages. Use the ``AuthenticationProviderKeys`` property instead of ``AuthenticationProviderKey`` one. We support this ``[Obsolete]`` property for backward compatibility and migration reasons. In future releases, the property may be removed as a breaking change.
.. [#f2] ":ref:`authentication-multiple`" feature was requested in issues `740`_, `1580`_ and delivered as a part of `23.0`_ release.
.. [#f3] We would appreciate any new pull requests to add extra acceptance tests for your custom scenarios with `multiple authentication schemes`_.
.. [#f4] For a complete understanding of .NET 8 breaking change related to JWT tokens, please refer to the Microsoft Learn documentation: "`Security token events return a JsonWebToken <https://learn.microsoft.com/en-us/dotnet/core/compatibility/aspnet-core/8.0/securitytoken-events>`__".

.. _446: https://github.com/ThreeMammals/Ocelot/issues/446
.. _740: https://github.com/ThreeMammals/Ocelot/issues/740
.. _1580: https://github.com/ThreeMammals/Ocelot/issues/1580
.. _23.0: https://github.com/ThreeMammals/Ocelot/releases/tag/23.0.0
.. _Mapping, customizing, and transforming claims in ASP.NET Core: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/claims?view=aspnetcore-9.0
.. _multiple authentication schemes: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/limitingidentitybyscheme#use-multiple-authentication-schemes
