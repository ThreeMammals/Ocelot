.. _ocelot.json: https://github.com/ThreeMammals/Ocelot/blob/develop/samples/Basic/ocelot.json
.. _Program: https://github.com/ThreeMammals/Ocelot/blob/develop/samples/Basic/Program.cs

Not Supported
=============

Ocelot does not support...

.. _chunked-encoding:

Chunked Encoding
----------------

Ocelot will always get the body size and return `Content-Length <https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Length>`_ header.
Sorry, if this doesn't work for your use case! 
  
Forwarding ``Host`` header
--------------------------

The `Host <https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Host>`_ header that you send to Ocelot will not be forwarded to the downstream service.
Obviously this would break everything.

Swagger
-------

Contributors have looked multiple times at building ``swagger.json`` out of the Ocelot `ocelot.json`_ but it doesnt fit into the vision the team has for Ocelot.
If you would like to have *Swagger* in Ocelot then you must roll your own ``swagger.json`` and do the following in your `Program`_.
The code sample below registers a piece of middleware that loads your hand rolled ``swagger.json`` and returns it on ``/swagger/v1/swagger.json``.
It then registers the *Swagger* UI middleware from `Swashbuckle.AspNetCore <https://www.nuget.org/packages/Swashbuckle.AspNetCore>`_ package:

.. code-block:: shell

  dotnet add package Swashbuckle.AspNetCore --version 10.2.x

Include the following code in your `Program`_ file:

.. code-block:: csharp
  :emphasize-lines: 9

  var builder = WebApplication.CreateBuilder(args);
  // ...
  var app = builder.Build();
  app.Map("/swagger/v1/swagger.json", builder =>
      builder.Run(async context => {
          var json = await File.ReadAllTextAsync("swagger.json");
          await context.Response.WriteAsync(json);
      }));
  app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ocelot"));

  await app.UseOcelot();
  await app.RunAsync();

The main reasons why we don't think *Swagger* makes sense is we already hand roll our definition in `ocelot.json`_.
If we want people developing against Ocelot to be able to see what routes are available then either share the `ocelot.json`_ with them
(This should be as easy as granting access to a repo etc) or use the Ocelot :doc:`../features/administration` API so that they can query Ocelot for the configuration.

In addition to this, many people will configure Ocelot to proxy all traffic like ``/products/{everything}`` to their product service
and you would not be describing what is actually available if you parsed this and turned it into a *Swagger* path.
Also Ocelot has no concept of the models that the downstream services can return and linking to the above problem the same endpoint can return multiple models.
Ocelot does not know what models might be used in POST, PUT etc, so it all gets a bit messy, and finally, the Swashbuckle package **does not** reload ``swagger.json`` if it changes during runtime.
Ocelot's configuration can change during runtime so the *Swagger* and Ocelot information would not match.
Unless we rolled our own *Swagger* implementation.

Swagger Alternatives
^^^^^^^^^^^^^^^^^^^^
.. _Postman: https://www.postman.com
.. _@Burgyn: https://github.com/Burgyn
.. _MMLib.SwaggerForOcelot: https://github.com/Burgyn/MMLib.SwaggerForOcelot

For developers looking for a straightforward way to test the Ocelot API, we recommend using `Postman`_.
While generating a Postman collection from `ocelot.json`_ may be feasible, we do not currently plan to support this feature.

A robust alternative is the community package `MMLib.SwaggerForOcelot`_, maintained by Miňo Martiniak (`@Burgyn`_).
This package addresses many common scenarios for generating *Swagger* documentation when using the Ocelot API gateway.
