Not Supported
=============

Ocelot does not support...

.. _chunked-encoding:

Chunked Encoding
----------------

Ocelot will always get the body size and return `Content-Length <https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Length>`_ header.
Sorry, if this doesn't work for your use case! 
	
Forwarding a Host header
------------------------

The `Host <https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Host>`_ header that you send to Ocelot will not be forwarded to the downstream service.
Obviously this would break everything 😟

Swagger
-------

Contributors have looked multiple times at building **swagger.json** out of the Ocelot **ocelot.json** but it doesnt fit into the vision the team has for Ocelot.
If you would like to have Swagger in Ocelot then you must roll your own **swagger.json** and do the following in your **Startup.cs** or **Program.cs**.
The code sample below registers a piece of middleware that loads your hand rolled **swagger.json** and returns it on ``/swagger/v1/swagger.json``.
It then registers the SwaggerUI middleware from `Swashbuckle.AspNetCore <https://www.nuget.org/packages/Swashbuckle.AspNetCore>`_ package:

.. code-block:: csharp

    app.Map("/swagger/v1/swagger.json", b =>
    {
        b.Run(async x => {
            var json = File.ReadAllText("swagger.json");
            await x.Response.WriteAsync(json);
        });
    });   
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ocelot");
    });

    app.UseOcelot().Wait();

The main reasons why we don't think Swagger makes sense is we already hand roll our definition in **ocelot.json**.
If we want people developing against Ocelot to be able to see what routes are available then either share the **ocelot.json** with them
(This should be as easy as granting access to a repo etc) or use the Ocelot :doc:`../features/administration` API so that they can query Ocelot for the configuration.

In addition to this, many people will configure Ocelot to proxy all traffic like ``/products/{everything}`` to their product service
and you would not be describing what is actually available if you parsed this and turned it into a Swagger path.
Also Ocelot has no concept of the models that the downstream services can return and linking to the above problem the same endpoint can return multiple models.
Ocelot does not know what models might be used in POST, PUT etc, so it all gets a bit messy, and finally, the Swashbuckle package doesnt reload **swagger.json** if it changes during runtime.
Ocelot's configuration can change during runtime so the Swagger and Ocelot information would not match.
Unless we rolled our own Swagger implementation. 😋

If the developer wants something to easily test against the Ocelot API then we suggest using `Postman <https://www.postman.com/>`_ as a simple way to do this.
It might even be possible to write something that maps **ocelot.json** to the Postman JSON spec. However we don't intend to do this.
