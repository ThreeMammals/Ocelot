using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Ocelot.Routing.ServiceFabric.Models.ServiceFabric
{
    [XmlRoot(ElementName = "ServiceManifest", Namespace = "http://schemas.microsoft.com/2011/01/fabric")]
    public class ServiceFabricServiceManifest
    {
        [XmlElement(ElementName = "ServiceTypes")]
        public ServiceFabricServiceManifestServiceTypes ServiceTypes { get; set; }

        [XmlElement(ElementName = "Resources")]
        public ServiceFabricServiceManifestResourceCollection Resources { get; set; }
    }
}
