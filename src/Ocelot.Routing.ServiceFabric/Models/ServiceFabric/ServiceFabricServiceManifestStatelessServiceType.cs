using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Ocelot.Routing.ServiceFabric.Models.ServiceFabric
{
    public class ServiceFabricServiceManifestStatelessServiceType
    {
        [XmlAttribute(AttributeName = "ServiceTypeName")]
        public string ServiceName { get; set; }

        [XmlArray(ElementName = "Extensions")]
        [XmlArrayItem("Extension", IsNullable = false)]
        public ServiceFabricServiceManifestExtension[] Extensions { get; set; }
    }
}
