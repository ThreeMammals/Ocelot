Tracing
=======

Ocelot providers tracing functionality from the excellent `Butterfly <https://github.com/ButterflyAPM>`_ project. 

In order to use the tracing please read the Butterfly documentation.

In ocelot you need to do the following if you wish to trace a ReRoute.

In your ConfigureServices method

.. code-block:: csharp

    services
        .AddOcelot()
        .AddOpenTracing(option =>
        {
            //this is the url that the butterfly collector server is running on...
            option.CollectorUrl = "http://localhost:9618";
            option.Service = "Ocelot";
        });

Then in your ocelot.json add the following to the ReRoute you want to trace..

.. code-block:: json

      "HttpHandlerOptions": {
            "UseTracing": true
        },

Ocelot will now send tracing information to Butterfly when this ReRoute is called.