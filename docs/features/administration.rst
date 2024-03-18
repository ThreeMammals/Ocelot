Administration
==============

Ocelot supports changing configuration during runtime via an authenticated HTTP API. This can be authenticated in two ways either using Ocelot's internal IdentityServer (for authenticating requests to the administration API only) or hooking the administration API authentication into your own IdentityServer.

The first thing you need to do if you want to use the administration API is bring in the relavent `NuGet package <https://www.nuget.org/packages/Ocelot.Administration>`_:

.. code-block:: powershell

    Install-Package Ocelot.Administration

This will bring down everything needed by the administration API.

Providing your own IdentityServer
---------------------------------

All you need to do to hook into your own IdentityServer is add the following configuration options with authentication to your ``ConfigureServices`` method.
After that we must pass these options to ``AddAdministration()`` extension of the ``OcelotBuilder`` being returned by ``AddOcelot()`` [#f1]_ like below:

.. code-block:: csharp

    public virtual void ConfigureServices(IServiceCollection services)
    {
        Action<JwtBearerOptions> options = o =>
        {
            o.Authority = identityServerRootUrl;
            o.RequireHttpsMetadata = false;
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
            };
            // etc...
        };

        services
            .AddOcelot()
            .AddAdministration("/administration", options);
    }

You now need to get a token from your IdentityServer and use in subsequent requests to Ocelot's administration API.

This feature was implemented for `Issue 228 <https://github.com/ThreeMammals/Ocelot/issues/228>`_.
It is useful because the IdentityServer authentication middleware needs the URL of the IdentityServer. 
If you are using the internal IdentityServer, it might not always be possible to have the Ocelot URL.  

Internal IdentityServer
-----------------------

The API is authenticated using Bearer tokens that you request from Ocelot itself.
This is provided by the amazing `Identity Server <https://github.com/IdentityServer/IdentityServer4>`_ project that the .NET community has been using for several years.
Check them out.

In order to enable the administration section, you need to do a few things. First of all, add this to your initial **Startup.cs**. 

The path can be anything you want and it is obviously recommended don't use a URL you would like to route through with Ocelot as this will not work.
The administration uses the ``MapWhen`` functionality of ASP.NET Core and all requests to "**{root}/administration**" will be sent there not to the Ocelot middleware.

The secret is the client secret that Ocelot's internal IdentityServer will use to authenticate requests to the administration API. This can be whatever you want it to be!
In order to pass this secret string as parameter, we must call the ``AddAdministration()`` extension of the ``OcelotBuilder`` being returned by ``AddOcelot()`` [#f1]_ like below:

.. code-block:: csharp

    public virtual void ConfigureServices(IServiceCollection services)
    {
        services
            .AddOcelot()
            .AddAdministration("/administration", "secret");
    }

In order for the administration API to work, Ocelot / IdentityServer must be able to call itself for validation. 
This means that you need to add the base URL of Ocelot to global configuration if it is not default ``http://localhost:5000``. 
Please note, if you are using something like Docker to host Ocelot it might not be able to call back to **localhost** etc, and you need to know what you are doing with Docker networking in this scenario. 
Anyway, this can be done as follows.

If you want to run on a different host and port locally:

.. code-block:: json

 "GlobalConfiguration": {
    "BaseUrl": "http://localhost:55580"
  }

or if Ocelot is exposed via DNS:

.. code-block:: json

 "GlobalConfiguration": {
    "BaseUrl": "http://mydns.com"
  }

Now, if you went with the configuration options above and want to access the API, you can use the Postman scripts called **ocelot.postman_collection.json** in the solution to change the Ocelot configuration. 
Obviously these will need to be changed if you are running Ocelot on a different URL to ``http://localhost:5000``.

The scripts show you how to request a Bearer token from Ocelot and then use it to GET the existing configuration and POST a configuration.

If you are running multiple Ocelot instances in a cluster then you need to use a certificate to sign the Bearer tokens used to access the administration API.

In order to do this, you need to add two more environmental variables for each Ocelot in the cluster:

1. ``OCELOT_CERTIFICATE`` The path to a certificate that can be used to sign the tokens. The certificate needs to be of the type X509 and obviously Ocelot needs to be able to access it.
2. ``OCELOT_CERTIFICATE_PASSWORD`` The password for the certificate.

Normally Ocelot just uses temporary signing credentials but if you set these environmental variables then it will use the certificate. 
If all the other Ocelot instances in the cluster have the same certificate then you are good!

Administration API
------------------

POST {adminPath}/connect/token
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

This gets a token for use with the admin area using the client credentials we talk about setting above.
Under the hood this calls into an IdentityServer hosted within Ocelot.

The body of the request is form-data as follows:

* ``client_id`` set as admin
* ``client_secret`` set as whatever you used when setting up the administration services.
* ``scope`` set as admin
* ``grant_type`` set as client_credentials

GET {adminPath}/configuration
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

This gets the current Ocelot configuration. It is exactly the same JSON we use to set Ocelot up with in the first place.

POST {adminPath}/configuration
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

This overwrites the existing configuration (should probably be a PUT!).
We recommend getting your config from the GET endpoint, making any changes and posting it back... simples.

The body of the request is JSON and it is the same format as the `FileConfiguration <https://github.com/ThreeMammals/Ocelot/blob/main/src/Ocelot/Configuration/File/FileConfiguration.cs>`_
that we use to set up Ocelot on a file system. 

Please note, if you want to use this API then the process running Ocelot must have permission to write to the disk where your **ocelot.json** or **ocelot.{environment}.json** is located.
This is because Ocelot will overwrite them on save. 

DELETE {adminPath}/outputcache/{region}
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

This clears a region of the cache. If you are using a backplane, it will clear all instances of the cache!
Giving your the ability to run a cluster of Ocelots and cache over all of them in memory and clear them all at the same time, so just use a distributed cache.

The region is whatever you set against the **Region** field in the `FileCacheOptions <https://github.com/search?q=repo%3AThreeMammals%2FOcelot%20FileCacheOptions&type=code>`_ section of the Ocelot configuration.

""""

.. [#f1] :ref:`di-the-addocelot-method` adds default ASP.NET services to DI container. You could call another extended :ref:`di-addocelotusingbuilder-method` while configuring services to develop your own :ref:`di-custom-builder`. See more instructions in the ":ref:`di-addocelotusingbuilder-method`" section of :doc:`../features/dependencyinjection` feature.
