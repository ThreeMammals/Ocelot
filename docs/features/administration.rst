.. _IdentityServer: https://github.com/DuendeArchive/IdentityServer4
.. _IdentityServer4: https://www.nuget.org/packages/IdentityServer4
.. _Program: https://github.com/ThreeMammals/Ocelot.Administration.IdentityServer4/blob/main/sample/Program.cs
.. _Ocelot.Administration.IdentityServer4: https://www.nuget.org/packages/Ocelot.Administration.IdentityServer4
.. _24.0: https://github.com/ThreeMammals/Ocelot/releases/tag/24.0.0
.. _Ocelot.postman_collection.json: https://github.com/ThreeMammals/Ocelot.Administration.IdentityServer4/blob/main/sample/Ocelot.postman_collection.json

Administration
==============

  **Ocelot extension package**: `Ocelot.Administration.IdentityServer4`_ with integrated `IdentityServer4`_ package by `IdentityServer org <https://github.com/IdentityServer>`_ (archived on March 6, 2025)

Ocelot supports changing configuration during runtime via an authenticated HTTP API.
This can be authenticated in two ways either using Ocelot's internal `IdentityServer`_ (for authenticating requests to the :ref:`administration-api` only) or hooking the :ref:`administration-api` authentication into your own `IdentityServer`_.

The first thing you need to do if you want to use the :ref:`administration-api` is bring in the relevant `Ocelot.Administration.IdentityServer4`_ package:

.. code-block:: powershell

  NuGet\Install-Package Ocelot.Administration.IdentityServer4
  dotnet add package Ocelot.Administration.IdentityServer4

This will bring down everything needed by the :ref:`administration-api`.

  **Warning!** Currently, the *Administration* feature relies solely on the `IdentityServer4`_ package, whose `repository <https://github.com/DuendeArchive/IdentityServer4>`_ was archived by its owner on July 31, 2024 (for the first time) and again on March 6, 2025.
  In release `24.0`_, the Ocelot team deprecated the `Ocelot.Administration.IdentityServer4`_ extension package.
  However, `the repository <https://github.com/ThreeMammals/Ocelot.Administration.IdentityServer4>`_ remains available, allowing for potential patches.

.. _ad-your-own-identityserver:

Your Own IdentityServer [#f1]_
------------------------------

All you need to do to hook into your own `IdentityServer`_ is add the following configuration options with authentication to your `Program`_.
After that, we must pass these options to the ``AddAdministration()`` extension of the ``OcelotBuilder`` being returned by ``AddOcelot()`` [#f2]_, as shown below:

.. code-block:: csharp

    Action<JwtBearerOptions> options = o =>
    {
        o.Authority = "https://identity-server-host:3333";
        o.RequireHttpsMetadata = true; // false in development environment
        o.TokenValidationParameters = new()
        {
            ValidateAudience = false,
        };
        //...
    };
    builder.Services
        .AddOcelot(builder.Configuration)
        .AddAdministration("/administration", options);

You now need to get a token from your `IdentityServer`_ and use in subsequent requests to Ocelot's :ref:`administration-api`.

  **Note**: This feature is useful because the `IdentityServer`_ authentication middleware needs the URL of the server.
  If you are using the :ref:`ad-internal-identityserver`, it might not always be possible to have the Ocelot URL.

.. _ad-internal-identityserver:

Internal IdentityServer
-----------------------

The API is authenticated using Bearer tokens that you request from Ocelot itself.
This is provided by the amazing `IdentityServer`_ project that the .NET community has been using for several years.
Check it out.

In order to enable the administration section, you need to do a few things. First of all, add this to your initial `Program`_.

The path can be anything you want and it is obviously recommended don't use a URL you would like to route through with Ocelot as this will not work.
The administration uses the ``MapWhen`` functionality of ASP.NET Core and all requests to ``{root}/administration`` will be sent there not to the Ocelot middleware.

The secret is the client secret that Ocelot's internal `IdentityServer`_ will use to authenticate requests to the :ref:`administration-api`.
This can be whatever you want it to be!
In order to pass this secret string as parameter, we must call the ``AddAdministration()`` extension of the ``OcelotBuilder`` being returned by ``AddOcelot()`` [#f2]_, as shown below:

.. code-block:: csharp

    builder.Services
        .AddOcelot(builder.Configuration)
        .AddAdministration("/administration", "secret");

In order for the :ref:`administration-api` to work, Ocelot and `IdentityServer`_ must be able to call themselves for validation.
This means that you need to add the base URL of Ocelot to the global configuration if it is not the default ``http://localhost:5000``.

  **Note**: If you are using something like Docker to host Ocelot, it might not be able to call back to ``localhost``, etc., and you need to know what you are doing with Docker networking in this scenario.

Configuration can be done as follows:

* If you want to run on a different host and port locally:

  .. code-block:: json

      "GlobalConfiguration": {
        "BaseUrl": "http://localhost:5580"
      }

* or if Ocelot is exposed via DNS:

  .. code-block:: json

      "GlobalConfiguration": {
        "BaseUrl": "http://mydns.com"
      }

Now, if you went with the configuration options above and want to access the API, you can use the Postman scripts called `Ocelot.postman_collection.json`_ in the solution to change the Ocelot configuration. 
Obviously these will need to be changed if you are running Ocelot on a different URL to ``http://localhost:5000``.

The scripts show you how to request a Bearer token from Ocelot and then use it to GET the existing configuration and POST a configuration.

If you are running multiple Ocelot instances in a cluster then you need to use a certificate to sign the Bearer tokens used to access the :ref:`administration-api`.

In order to do this, you need to add two more environmental variables for each Ocelot in the cluster:

1. ``OCELOT_CERTIFICATE``: The path to a certificate that can be used to sign the tokens. The certificate needs to be of the type X509 and obviously Ocelot needs to be able to access it.
2. ``OCELOT_CERTIFICATE_PASSWORD``: The password for the certificate.

Normally Ocelot just uses temporary signing credentials but if you set these environmental variables then it will use the certificate. 
If all the other Ocelot instances in the cluster have the same certificate then you are good!

.. _administration-api:

Administration API
------------------

* **POST** ``{adminPath}/connect/token``

    This gets a token for use with the admin area using the client credentials we talk about setting above.
    Under the hood this calls into an `IdentityServer`_ hosted within Ocelot.

    The body of the request is form-data as follows:

    * ``client_id`` set as admin
    * ``client_secret`` set as whatever you used when setting up the administration services.
    * ``scope`` set as admin
    * ``grant_type`` set as client_credentials

* **GET** ``{adminPath}/configuration``

    This gets the current Ocelot configuration. It is exactly the same JSON we use to set Ocelot up with in the first place.

* **POST** ``{adminPath}/configuration``

    This overwrites the existing configuration (should probably be a PUT!).
    We recommend getting your config from the GET endpoint, making any changes and posting it back... simples.

    The body of the request is JSON and it is the same format as the `FileConfiguration <https://github.com/ThreeMammals/Ocelot/blob/main/src/Ocelot/Configuration/File/FileConfiguration.cs>`_
    that we use to set up Ocelot on a file system. 

    Please note, if you want to use this API then the process running Ocelot must have permission to write to the disk where your ``ocelot.json`` or ``ocelot.{environment}.json`` is located.
    This is because Ocelot will overwrite them on save. 

* **DELETE** ``{adminPath}/outputcache/{region}``

    This clears a region of the cache. If you are using a backplane, it will clear all instances of the cache!
    Giving your the ability to run a cluster of Ocelots and cache over all of them in memory and clear them all at the same time, so just use a distributed cache.

    The region is whatever you set against the ``Region`` field in the `FileCacheOptions <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20FileCacheOptions&type=code>`_ section of the Ocelot configuration.

""""

.. [#f1] The ":ref:`ad-your-own-identityserver`" feature was implemented for issue `228 <https://github.com/ThreeMammals/Ocelot/issues/228>`_.
.. [#f2] The :ref:`di-services-addocelot-method` adds default ASP.NET services to the DI container. You can call another extended :ref:`di-addocelotusingbuilder-method` while configuring services to develop your own :ref:`di-custom-builder`. See more instructions in the ":ref:`di-addocelotusingbuilder-method`" section of the :doc:`../features/dependencyinjection` feature.
