using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Ocelot.Routing.ServiceFabric.Models.ServiceFabric
{
    public class ServiceFabricClusterManifestFabricSettings
    {
        [XmlArray(ElementName = "Section")]
        public ServiceFabricClusterManifestFabricSettingsSection[] Sections { get; set; }
    }
}
