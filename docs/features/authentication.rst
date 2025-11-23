.. _scheme: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/#authentication-scheme
.. _Program: https://github.com/ThreeMammals/Ocelot/blob/main/samples/Basic/Program.cs

Authentication
==============

In order to authenticate routes and subsequently use any of Ocelot's claims based features such as authorization or modifying the request with values from the token,
users must register authentication services in their `Program`_ as usual but they provide a `scheme`_ 
(authentication provider key) with each registration e.g.

.. code-block:: csharp

    const string AuthenticationProviderKey = "MyKey"; // aka scheme
    builder.Services
        .AddAuthentication()
        .AddJwtBearer(AuthenticationProviderKey, options =>
        {
            // authentication setup via options initialization
        });

In this example, ``MyKey`` is the `scheme`_ with which this provider has been registered, but for JWT bearer authentication, the scheme is usually ``Bearer``.
We then map this to a route in the configuration using the following :ref:`authentication-options-schema` options:

* ``AuthenticationProviderKey`` is a string, the legacy definition of :ref:`Single Authentication Scheme <authentication-scheme>`.
* ``AuthenticationProviderKeys`` is an array of strings, the recommended definition of :ref:`Multiple Authentication Schemes <authentication-multiple>` feature.

.. _authentication-options-schema:

``AuthenticationOptions`` Schema
--------------------------------

.. _FileAuthenticationOptions: https://github.com/ThreeMammals/Ocelot/blob/main/src/Ocelot/Configuration/File/FileAuthenticationOptions.cs

  Class: `FileAuthenticationOptions`_

The following is the full *authentication* configuration, used in both the :ref:`config-route-schema` and the :ref:`config-dynamic-route-schema`.
Not all of these options need to be configured; however, the ``AuthenticationProviderKeys`` option is mandatory when ``AuthenticationProviderKey`` is absent.

.. code-block:: json

  "AuthenticationOptions": {
    "AllowAnonymous": false, // nullable boolean
    "AllowedScopes": [], // array of strings
    "AuthenticationProviderKey": "", // deprecated! -> use AuthenticationProviderKeys
    "AuthenticationProviderKeys": [] // array of strings
  }

.. list-table::
  :widths: 25 75
  :header-rows: 1

  * - *Option*
    - *Description*
  * - ``AllowAnonymous``
    - Excludes a route from global *authentication options* by setting it to ``true``.
      If the global option disables authentication by forcibly having a ``true`` value, then at the route level the option can include a route to be authenticated by setting it to ``false``.
      For more details, refer to the ":ref:`Configuration and AllowAnonymous <authentication-configuration>`" section.
  * - ``AllowedScopes``
    - If specified, enables authorization based on the ``scope`` claim after successful authentication by a configured authentication provider.
      For more details, refer to the ":ref:`authentication-allowed-scopes`" section.
  * - ``AuthenticationProviderKey``
    - Maps a configured authentication provider, identified by a key (scheme), to a route that requires authentication.
      *Note: This option is deprecated—see the warning below.*
      For more details, refer to the ":ref:`Single Authentication Scheme <authentication-scheme>`" section.
  * - ``AuthenticationProviderKeys``
    - Maps all configured authentication providers, identified by their schemes, to a route that requires authentication.
      For more details, refer to the ":ref:`Multiple Authentication Schemes <authentication-multiple>`" section.

.. warning::
  The ``AuthenticationProviderKey`` option is deprecated in version `24.1`_! Use the ``AuthenticationProviderKeys`` array option instead.
  Note that ``AuthenticationProviderKey`` will be removed in version `25.0`_.
  For backward compatibility in version `24.1`_, the ``AuthenticationProviderKey`` option takes precedence over the schemes in the ``AuthenticationProviderKeys`` array.
  If the ``AuthenticationProviderKey`` scheme provider fails, the remaining schemes in the ``AuthenticationProviderKeys`` array will enforce the appropriate authentication providers in the specified order.

.. _authentication-scheme:

Single Authentication Scheme [#f1]_
-----------------------------------

  Option: ``AuthenticationProviderKey``

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
.. _multiple authentication schemes: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/limitingidentitybyscheme#use-multiple-authentication-schemes

  Option: ``AuthenticationProviderKeys``

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
The order of the keys in an array definition does matter! We use a "First One Wins" authentication strategy.

.. _authentication-configuration:

Configuration and ``AllowAnonymous`` [#f3]_
-------------------------------------------

To configure *authentication options* uniformly across all static routes, define them in ``GlobalConfiguration`` section using the :ref:`authentication-options-schema`.
If *authentication options* are specified in both ``GlobalConfiguration`` and a route (i.e., ``AuthenticationProviderKey`` or ``AuthenticationProviderKeys`` are set), the route-level configuration takes precedence.

Excluding a route from global *authentication options* is possible by setting ``AllowAnonymous`` option to ``true``.
This prevents the route from requiring authentication, keeping it open and anonymous.

In the following example:

* The first route is authenticated using the ``MyGlobalKey`` provider's scheme.
* The second route uses the ``MyKey`` provider's scheme.
* The third route is not authenticated.

.. code-block:: json
  :emphasize-lines: 4, 8-11, 15-17, 22-26

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
      "RouteKeys": [], // empty -> no grouping, thus opts will apply to all routes
      "AuthenticationProviderKeys": [ "MyGlobalKey" ],
      "AllowedScopes": [ "Admin" ]
    }
  }

.. _break: http://break.do

  **Note**: Ocelot performs a per-option merging algorithm to combine route and global ``AuthenticationOptions``.
  If global ``AuthenticationProviderKeys`` are defined together with global ``AllowedScopes``, then route options should be specified as a pair of scheme and scopes; otherwise, a scope should not belong to the global authentication provider.
  Moreover, the route scopes array entirely overrides the global scopes array, so the two collections are not merged but rather interchangeable.

.. _authentication-global-configuration:

Global Configuration [#f4]_
---------------------------

Since the global configuration for static routes has already been described above, here are additional details regarding dynamic routes, whose configuration was not supported in versions prior to `24.1`_.
Starting with version `24.1`_, global and route *authentication options* for :ref:`Dynamic Routing <routing-dynamic>` were introduced.
These global options may also be overridden in the ``DynamicRoutes`` configuration section, as defined by the :ref:`config-dynamic-route-schema`.

.. code-block:: json
  :emphasize-lines: 6-9, 18-22

  {
    "DynamicRoutes": [
      {
        "Key": "R1", // optional
        "ServiceName": "my-service",
        "AuthenticationOptions": {
          "AuthenticationProviderKeys": ["MyKey"], // custom authentication provider
          "AllowedScopes": ["my-service"] // require authorization with a 'scope' claim set to the value 'my-service'
        }
      }
    ],
    "GlobalConfiguration": {
      "BaseUrl": "https://ocelot.net",
      "DownstreamScheme": "http",
      "ServiceDiscoveryProvider": {
        // required section for dynamic routing
      },
      "AuthenticationOptions": {
        "RouteKeys": [], // or null, no grouping, thus opts apply to all dynamic routes
        "AuthenticationProviderKeys": ["Bearer"], // use a global JWT bearer auth provider for all discovered services
        "AllowedScopes": ["oc-admin"] // require the global 'scope' claim to gain access to all discovered services
      }
    }
  }

In this configuration, an ``oc-admin`` scope authorization is applied to all implicit dynamic routes by the global ``Bearer`` JWT signing service.
However, for the “my-service” service, authorization with the ``my-service`` scope is applied, and authentication is provided by another source of tokens named ``MyKey``.

.. note::

  1. If the ``RouteKeys`` option is not defined or the array is empty in the global ``AuthenticationOptions``, the global options will apply to all routes.
  If the array contains route keys, it defines a single group of routes to which the global options apply.
  Routes excluded from this group must specify their own route-level ``AuthenticationOptions``.

  2. Prior to version `24.1`_, global and dynamic route ``AuthenticationOptions`` were not available.
  Starting with version `24.1`_, global configuration is supported for both static and dynamic routes.

.. _authentication-allowed-scopes:

Allowed Scopes
--------------

  Option: ``AllowedScopes``

To set up authorization by scopes from the ``AllowedScopes`` collection, after successful authentication by the middleware and after claims have been transformed,
the authorization middleware in Ocelot retrieves all user claims (from the token) of the '``scope``' type and ensures that the user has at least one of the scopes in the list.
This provides a way to restrict access to a route on a per-scope basis.

  **Note**: Since version `24.1`_, specifying global *allowed scopes* is exclusively supported.
  Therefore, only a route-level scheme (i.e., the ``AuthenticationProviderKeys`` array) combined with a route-level ``AllowedScopes`` array can override the global ``AllowedScopes``.
  Sure, to enable authentication, the ``AllowAnonymous`` option must be set to ``false`` or left undefined.
  For more details, refer to the ":ref:`Configuration and AllowAnonymous <authentication-configuration>`" and ":ref:`Global Configuration <authentication-global-configuration>`" sections.

JWT Tokens
----------

If you want to authenticate using JWT tokens maybe from a provider like `Auth0 <https://auth0.com/>`_, you can register your authentication middleware as normal e.g.

.. code-block:: csharp

    builder.Services
        .AddAuthentication()
        .AddJwtBearer("Auth0", options =>
        {
            options.Authority = "test";
            options.Audience = "test";
        });
    builder.Services
        .AddOcelot(builder.Configuration);

Then map the authentication provider key to a route in your configuration e.g.

.. code-block:: json

  "AuthenticationOptions": {
    "AuthenticationProviderKeys": ["Auth0"],
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

    Action<JwtBearerOptions> options = o =>
    {
        o.Authority = "https://whereyouridentityserverlives.com";
        // ...
    };
    builder.Services
        .AddAuthentication()
        .AddJwtBearer("IS4", options);
    builder.Services
        .AddOcelot(builder.Configuration);

Then map the authentication provider key to a route in your configuration e.g.

.. code-block:: json

  "AuthenticationOptions": {
    "AuthenticationProviderKeys": ["IS4"],
  }

Auth0 by Okta
-------------

Yet another identity provider by `Okta <https://www.okta.com/>`_, see `Auth0 Developer Resources <https://developer.auth0.com/>`_.

Add the following, at minimum, to your startup `Program`_:

.. code-block:: csharp

    builder.Services
        .AddAuthentication()
        .AddJwtBearer("Okta", o =>
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
    3. It is highly advisable to read and understand the :ref:`authentication-warnings` related to the critical changes in authentication when utilizing .NET 8.

.. _authentication-warnings:

Warnings
--------

.. warning::
  .NET 8 introduced a breaking change where ``JwtSecurityToken`` was replaced with ``JsonWebToken`` to enhance performance and reliability.
  Consequently, their handlers were changed ``JwtSecurityTokenHandler`` to ``JsonWebTokenHandler``.
  For a complete understanding of .NET 8 breaking change related to JWT tokens, please refer to the Microsoft Learn documentation: "`Security token events return a JsonWebToken <https://learn.microsoft.com/en-us/dotnet/core/compatibility/aspnet-core/8.0/securitytoken-events>`__".

Links
-----
.. _Mapping, customizing, and transforming claims in ASP.NET Core: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/claims?view=aspnetcore-9.0

* Microsoft Learn: `Overview of ASP.NET Core authentication <https://learn.microsoft.com/en-us/aspnet/core/security/authentication/>`_
* Microsoft Learn: `Authorize with a specific scheme in ASP.NET Core <https://learn.microsoft.com/en-us/aspnet/core/security/authorization/limitingidentitybyscheme>`_
* Microsoft Learn: `Policy schemes in ASP.NET Core <https://learn.microsoft.com/en-us/aspnet/core/security/authentication/policyschemes>`_
* Microsoft Learn: `Mapping, customizing, and transforming claims in ASP.NET Core`_
* Microsoft .NET Blog: `ASP.NET Core Authentication with IdentityServer4 <https://devblogs.microsoft.com/dotnet/asp-net-core-authentication-with-identityserver4/>`_

Roadmap
-------

Nothing is currently in the stack, but the Ocelot team is rethinking a new version of the ":doc:`../features/administration`" feature, which is closely dependent on authentication.

We invite you to add more examples if you have integrated with other identity providers and the integration solution is working.
Please open a "`Show and tell <https://github.com/ThreeMammals/Ocelot/discussions/categories/show-and-tell>`_" discussion in the repository.

""""

.. [#f1] The ":ref:`Single Authentication Scheme <authentication-scheme>`" feature has been an Ocelot artifact for ages. Use the ``AuthenticationProviderKeys`` property instead of ``AuthenticationProviderKey`` one. We support this ``[Obsolete]`` property for backward compatibility and migration reasons. In future releases, the property may be removed as a breaking change.
.. [#f2] The ":ref:`Multiple Authentication Schemes <authentication-multiple>`" feature was requested in issues `740`_, `1580`_ and delivered as a part of `23.0`_ release.
.. [#f3] The global ":ref:`Configuration and AllowAnonymous <authentication-configuration>`" feature for static routes was requested in issues `842`_ and `1414`_, implemented in pull request `2114`_, and officially released in version `24.1`_.
.. [#f4] The ":ref:`Global Configuration <authentication-global-configuration>`" feature for dynamic routes was requested in issues `585`_ and `2316`_, implemented in pull request `2336`_, and released in version `24.1`_.

.. _446: https://github.com/ThreeMammals/Ocelot/issues/446
.. _585: https://github.com/ThreeMammals/Ocelot/issues/585
.. _740: https://github.com/ThreeMammals/Ocelot/issues/740
.. _842: https://github.com/ThreeMammals/Ocelot/issues/842
.. _1414: https://github.com/ThreeMammals/Ocelot/issues/1414
.. _1580: https://github.com/ThreeMammals/Ocelot/issues/1580
.. _2114: https://github.com/ThreeMammals/Ocelot/pull/2114
.. _2316: https://github.com/ThreeMammals/Ocelot/issues/2316
.. _2336: https://github.com/ThreeMammals/Ocelot/pull/2336
.. _23.0: https://github.com/ThreeMammals/Ocelot/releases/tag/23.0.0
.. _24.1: https://github.com/ThreeMammals/Ocelot/releases/tag/24.1.0
.. _25.0: https://github.com/ThreeMammals/Ocelot/milestone/13
