Quality of Service
==================

Ocelot supports one QoS capability at the current time. You can set on a per ReRoute basis if you 
want to use a circuit breaker when making requests to a downstream service. This uses the an awesome
.NET library called Polly check them out `here <https://github.com/App-vNext/Polly>`_.

Add the following section to a ReRoute configuration. 

.. code-block:: json

    "QoSOptions": {
        "ExceptionsAllowedBeforeBreaking":3,
        "DurationOfBreak":5,
        "TimeoutValue":5000
    }

You must set a number greater than 0 against ExceptionsAllowedBeforeBreaking for this rule to be 
implemented. Duration of break is how long the circuit breaker will stay open for after it is tripped.
TimeoutValue means ff a request takes more than 5 seconds it will automatically be timed out. 

If you do not add a QoS section QoS will not be used.