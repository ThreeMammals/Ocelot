using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Ocelot.Routing.ServiceFabric.Models.ServiceFabric
{
    public class ServiceFabricServiceManifestResourceCollection
    {
        [XmlArray(ElementName = "Endpoints")]
        [XmlArrayItem("Endpoint", IsNullable = false)]
        public ServiceFabricServiceManifestEndpoint[] Endpoints { get; set; }
    }
}
