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

* ``AuthenticationProviderKey`` is a string object, obsolete [#f2]_. This is legacy definition when you define :ref:`Single Authentication Scheme <authentication-scheme>`.
* ``AuthenticationProviderKeys`` is an array of strings, the recommended definition of :ref:`Multiple Authentication Schemes <authentication-multiple>` feature.

.. _authentication-configuration:

Configuration and ``AllowAnonymous`` [#f1]_
-------------------------------------------

If you want to configure *authentication options* uniformly across all routes, define them in ``GlobalConfiguration`` section using the ``AuthenticationOptions`` schema.
If *authentication options* are specified in both ``GlobalConfiguration`` and a route (i.e., ``AuthenticationProviderKey`` or ``AuthenticationProviderKeys`` are set), the route-level configuration takes precedence.

Excluding a route from global *authentication options* is possible by setting ``AllowAnonymous`` option to ``true`` in the route's ``AuthenticationOptions``.
This will prevent the route from being authenticated.

In the following example:

* The first route is authenticated using the ``MyGlobalKey`` provider's scheme.
* The second route uses the ``MyKey`` provider's scheme.
* The third route is not authenticated.

.. code-block:: json

  "Routes": [
    {
      // route #1 props...
      "AuthenticationOptions": {}
    },
    {
      // route #2 props...
      "AuthenticationOptions": {
        "AuthenticationProviderKeys": [ "MyKey" ],
        "AllowedScopes": [ "Bob" ]
      }
    },
    {
      // route #3 props...
      "AuthenticationOptions": {
        "AllowAnonymous": true
      }
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

  **Note**: If global ``AuthenticationProviderKeys`` are defined (i.e., the route does not explicitly configure its own ``AuthenticationProviderKeys``),
  then global ``AllowedScopes`` will also be applied, even if the route specifies its own ``AllowedScopes``.

.. _authentication-scheme:

Single Authentication Scheme [#f2]_
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

Multiple Authentication Schemes [#f3]_
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

Afterward, Ocelot applies all steps that are specified for ``AuthenticationProviderKey`` as :ref:`Single Authentication Scheme <authentication-scheme>`.

    **Note** that the order of the keys in an array definition does matter! We use a "First One Wins" authentication strategy.

Finally, we would say that registering providers, initializing options, and forwarding authentication artifacts can be a "real" coding challenge.
If you're stuck or don't know what to do, just find inspiration in our `acceptance tests <https://github.com/search?q=repo%3AThreeMammals%2FOcelot+MultipleAuthSchemesFeatureTests+language%3AC%23&type=code&l=C%23>`_ [#f4]_
(currently for `IdentityServer4 <https://identityserver4.readthedocs.io/>`_ only).

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
    3. It is highly advisable to read and understand the :ref:`authentication-warning` related to the critical changes in authentication when utilizing .NET 8. [#f5]_

Allowed Scopes
--------------

If you add scopes to ``AllowedScopes``, Ocelot will get all the user claims (from the token) of the '``scope``' type and make sure that the user has at least one of the scopes in the list.
This is a way to restrict access to a route on a per scope basis.

  **Note**: Since version `24.1`_, specifying global *allowed scopes* is exclusively supported.
  Therefore, only a route-level scheme (i.e., the ``AuthenticationProviderKeys`` array) combined with a route-level ``AllowedScopes`` array can override the global ``AllowedScopes``.
  Sure, to enable authentication, the ``AllowAnonymous`` option must be set to ``false`` or left undefined.

.. _authentication-warning:

Warning
-------

.NET 8 introduced a breaking change [#f5]_ where ``JwtSecurityToken`` was replaced with ``JsonWebToken`` to enhance performance and reliability.
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

.. [#f1] The global ":ref:`Configuration <authentication-configuration>`" feature was requested in issues `842`_ and `1414`_, implemented in pull request `2114`_, and officially released in version `24.1`_.
.. [#f2] The ":ref:`Single Authentication Scheme <authentication-scheme>`" feature has been an Ocelot artifact for ages. Use the ``AuthenticationProviderKeys`` property instead of ``AuthenticationProviderKey`` one. We support this ``[Obsolete]`` property for backward compatibility and migration reasons. In future releases, the property may be removed as a breaking change.
.. [#f3] The ":ref:`Multiple Authentication Schemes <authentication-multiple>`" feature was requested in issues `740`_, `1580`_ and delivered as a part of `23.0`_ release.
.. [#f4] We would appreciate any new pull requests to add extra acceptance tests for your custom scenarios with `multiple authentication schemes`_.
.. [#f5] For a complete understanding of .NET 8 breaking change related to JWT tokens, please refer to the Microsoft Learn documentation: "`Security token events return a JsonWebToken <https://learn.microsoft.com/en-us/dotnet/core/compatibility/aspnet-core/8.0/securitytoken-events>`__".

.. _446: https://github.com/ThreeMammals/Ocelot/issues/446
.. _740: https://github.com/ThreeMammals/Ocelot/issues/740
.. _842: https://github.com/ThreeMammals/Ocelot/issues/842
.. _1414: https://github.com/ThreeMammals/Ocelot/issues/1414
.. _1580: https://github.com/ThreeMammals/Ocelot/issues/1580
.. _2114: https://github.com/ThreeMammals/Ocelot/pull/2114
.. _23.0: https://github.com/ThreeMammals/Ocelot/releases/tag/23.0.0
.. _24.1: https://github.com/ThreeMammals/Ocelot/releases/tag/24.1.0
.. _Mapping, customizing, and transforming claims in ASP.NET Core: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/claims?view=aspnetcore-9.0
.. _multiple authentication schemes: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/limitingidentitybyscheme#use-multiple-authentication-schemes
