using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Ocelot.Routing.ServiceFabric.Models.ServiceFabric
{
    [XmlRoot(ElementName = "ClusterManifest", Namespace = "http://schemas.microsoft.com/2011/01/fabric")]
    public class ServiceFabricClusterManifest
    {
        [XmlElement(ElementName = "FabricSettings")]
        public ServiceFabricClusterManifestFabricSettings FabricSettings { get; set; }
    }
}
