using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Ocelot.Routing.ServiceFabric.Models.ServiceFabric
{
    public class ServiceFabricClusterManifestFabricSettingsSection
    {
        [XmlAttribute(AttributeName = "Name")]
        public string Name { get; set; }

        [XmlArray(ElementName = "Parameter")]
        public ServiceFabricClusterManifestFabricSettingsSectionParameter[] Parameters { get; set; }
    }
}
