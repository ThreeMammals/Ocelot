Claims Transformation
=====================

Ocelot allows the user to access claims and transform them into headers, query string 
parameters and other claims. This is only available once a user has been authenticated.

After the user is authenticated we run the claims to claims transformation middleware.
This allows the user to transform claims before the authorisation middleware is called.
After the user is authorised first we call the claims to headers middleware and Finally
the claims to query strig parameters middleware.

The syntax for performing the transforms is the same for each proces. In the ReRoute
configuration a json dictionary is added with a specific name either AddClaimsToRequest,
AddHeadersToRequest, AddQueriesToRequest. 

Note I'm not a hotshot programmer so have no idea if this syntax is good..

Within this dictionary the entries specify how Ocelot should transform things! 
The key to the dictionary is going to become the key of either a claim, header 
or query parameter.

The value of the entry is parsed to logic that will perform the transform. First of
all a dictionary accessor is specified e.g. Claims[CustomerId]. This means we want
to access the claims and get the CustomerId claim type. Next is a greater than (>)
symbol which is just used to split the string. The next entry is either value or value with
and indexer. If value is specifed Ocelot will just take the value and add it to the 
transform. If the value has an indexer Ocelot will look for a delimiter which is provided
after another greater than symbol. Ocelot will then split the value on the delimiter 
and add whatever was at the index requested to the transform.

Claims to Claims Tranformation
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Below is an example configuration that will transforms claims to claims

.. code-block:: json

    "AddClaimsToRequest": {
        "UserType": "Claims[sub] > value[0] > |",
        "UserId": "Claims[sub] > value[1] > |"
    }

This shows a transforms where Ocelot looks at the users sub claim and transforms it into
UserType and UserId claims. Assuming the sub looks like this "usertypevalue|useridvalue".

Claims to Headers Tranformation
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Below is an example configuration that will transforms claims to headers

.. code-block:: json

    "AddHeadersToRequest": {
        "CustomerId": "Claims[sub] > value[1] > |"
    }

This shows a transform where Ocelot looks at the users sub claim and trasnforms it into a 
CustomerId header. Assuming the sub looks like this "usertypevalue|useridvalue".

Claims to Query String Parameters Tranformation
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Below is an example configuration that will transforms claims to query string parameters

.. code-block:: json

    "AddQueriesToRequest": {
        "LocationId": "Claims[LocationId] > value",
    }

This shows a transform where Ocelot looks at the users LocationId claim and add its as
a query string parameter to be forwarded onto the downstream service.