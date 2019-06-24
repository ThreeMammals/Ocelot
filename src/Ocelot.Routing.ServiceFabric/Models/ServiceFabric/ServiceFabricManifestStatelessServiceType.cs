using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Ocelot.Routing.ServiceFabric.Models.ServiceFabric
{
    public class ServiceFabricManifestStatelessServiceType
    {
        [XmlAttribute(AttributeName = "ServiceTypeName")]
        public string ServiceName { get; set; }

        [XmlArray(ElementName = "Extensions")]
        [XmlArrayItem("Extension", IsNullable = false)]
        public ServiceFabricManifestExtension[] Extensions { get; set; }
    }
}
