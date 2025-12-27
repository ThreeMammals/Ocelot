.. _GraphQL: https://graphql.org/
.. _Ocelot.Samples.GraphQL: https://github.com/ThreeMammals/Ocelot/tree/main/samples/GraphQL
.. _graphql-dotnet: https://github.com/graphql-dotnet/graphql-dotnet
.. |GraphQL Logo| image:: https://avatars.githubusercontent.com/u/13958777
  :alt: GraphQL Logo
  :width: 40

|GraphQL Logo| GraphQL
======================

Ocelot does not directly support `GraphQL`_, but many people have asked about it.
We wanted to show how easy it is to integrate the `GraphQL for .NET <https://github.com/graphql-dotnet/graphql-dotnet>`_ library.

Sample
------

  **Sample**: `Ocelot.Samples.GraphQL`_

Please see the sample project `Ocelot.Samples.GraphQL`_.
Using a combination of the `graphql-dotnet`_ project and Ocelot :doc:`../features/delegatinghandlers` feature, this is pretty easy to do.
However, we do not intend to integrate more closely with `GraphQL`_ at the moment.
Check out the sample's `README.md <https://github.com/ThreeMammals/Ocelot/blob/main/samples/GraphQL/README.md>`_ for detailed instructions on how to do this.

Future
------

If you have sufficient experience with `GraphQL`_ and the mentioned .NET `graphql-dotnet`_ package, we would welcome your contribution to the sample. |octocat|

.. |octocat| image:: https://github.githubassets.com/images/icons/emoji/octocat.png
  :alt: octocat
  :height: 25
  :class: img-valign-middle

Who knows, maybe you will get inspired by the sample development and come up with a design solution in the form of a rough draft of a *GraphQL* feature to implement in Ocelot.
Good luck!
And welcome to the `Discussions <https://github.com/ThreeMammals/Ocelot/discussions>`_ space of the repository!
