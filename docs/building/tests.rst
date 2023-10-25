Tests
=====

The tests should all just run and work as part of the build process. You can of course also run them in Visual Studio.

Create SSL Cert for Testing
---------------------------

You can do this via `OpenSSL <https://www.openssl.org/>`_:

* Install `openssl package <https://github.com/openssl/openssl>`_ (if you are using Windows, download binaries `here <https://www.openssl.org/source/>`_).
* Generate private key: ``openssl genrsa 2048 > private.pem``
* Generate the self-signed certificate: ``openssl req -x509 -days 1000 -new -key private.pem -out public.pem``
* If needed, create PFX: ``openssl pkcs12 -export -in public.pem -inkey private.pem -out mycert.pfx``
