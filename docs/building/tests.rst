Tests
=====

The tests should all just run and work as part of the build process. You can of course also run them in visual studio.


Create SSL Cert for Testing
^^^^^^^^^^^^^^^^^^^^^^^^^^^

You can do this via openssl:

Install openssl package (if you are using Windows, download binaries here).

Generate private key: `openssl genrsa 2048 > private.pem`

Generate the self signed certificate: `openssl req -x509 -days 1000 -new -key private.pem -out public.pem`

If needed, create PFX: `openssl pkcs12 -export -in public.pem -inkey private.pem -out mycert.pfx`