Administration
==============

Ocelot supports changing configuration during runtime via an authenticated HTTP API. The API is authenticated 
using bearer tokens that you request from Ocelot iteself. This is provided by the amazing 
`Identity Server <https://github.com/IdentityServer/IdentityServer4>`_ project that I have been using for a few years now. Check them out.

In order to enable the administration section you need to do a few things. First of all add this to your
initial configuration.json. The value can be anything you want and it is obviously reccomended don't use
a url you would like to route through with Ocelot as this will not work. The administration uses the
MapWhen functionality of asp.net core and all requests to {root}/administration will be sent there not 
to the Ocelot middleware.

.. code-block:: json

    "GlobalConfiguration": {
        "AdministrationPath": "/administration"
    }

This will get the admin area set up but not the authentication. 
Please note that this is a very basic approach to 
this problem and if needed we can obviously improve on this!

You need to set 3 environmental variables. 

    ``OCELOT_USERNAME``

        This need to be the admin username you want to use with Ocelot.
    ``OCELOT_HASH``
    ``OCELOT_SALT``
        The hash and salt of the password you want to use given hashing algorythm. When requesting bearer tokens for use with the administration api you will need to supply username and password. In order to create a hash and salt of your password please check out HashCreationTests.should_create_hash_and_salt() this technique is based on [this](https://docs.microsoft.com/en-us/aspnet/core/security/data-protection/consumer-apis/password-hashing)
        using SHA256 rather than SHA1.



Now if you went with the configuration options above and want to access the API you can use the postman scripts
called ocelot.postman_collection.json in the solution to change the Ocelot configuration. Obviously these 
will need to be changed if you are running Ocelot on a different url to http://localhost:5000.

The scripts show you how to request a bearer token from ocelot and then use it to GET the existing configuration and POST 
a configuration.


Administration running multiple Ocelot's
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
If you are running multiple Ocelot's in a cluster then you need to use a certificate to sign the bearer tokens used to access the administration API.

In order to do this you need to add two more environmental variables for each Ocelot in the cluster.

``OCELOT_CERTIFICATE``
    The path to a certificate that can be used to sign the tokens. The certificate needs to be of the type X509 and obviously Ocelot needs to be able to access it.
``OCELOT_CERTIFICATE_PASSWORD``
    The password for the certificate.

Normally Ocelot just uses temporary signing credentials but if you set these environmental variables then it will use the certificate. If all the other Ocelots in the cluster have the same certificate then you are good!

Administration API
^^^^^^^^^^^^^^^^^^

**POST {adminPath}/connect/token**

This gets a token for use with the admin area using the username and password we talk about setting above. Under the hood this calls into an IdentityServer hosted within Ocelot.

The body of the request is form-data as follows

``client_id`` set as admin

``client_secret`` set as secret

``scope`` set as admin

``username`` set as whatever you used

``password`` set aswhatever you used

``grant_type`` set as password

**GET {adminPath}/configuration**


This gets the current Ocelot configuration. It is exactly the same JSON we use to set Ocelot up with in the first place.

**POST {adminPath}/configuration**


This overrwrites the existing configuration (should probably be a put!). I reccomend getting your config from the GET endpoint, making any changes and posting it back...simples.

The body of the request is JSON and it is the same format as the FileConfiguration.cs that we use to set up 
Ocelot on a file system.

**DELETE {adminPath}/outputcache/{region}**

This clears a region of the cache. If you are using a backplane it will clear all instances of the cache! Giving your the ability to run a cluster of Ocelots and cache over all of them in memory and clear them all at the same time / just use a distributed cache.

The region is whatever you set against the Region field in the FileCacheOptions section of the Ocelot configuration.
