using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Ocelot.Routing.ServiceFabric.Models.ServiceFabric
{
    public class ServiceFabricManifestResourceCollection
    {
        [XmlArray(ElementName = "Endpoints")]
        [XmlArrayItem("Endpoint", IsNullable = false)]
        public ServiceFabricManifestEndpoint[] Endpoints { get; set; }
    }
}
