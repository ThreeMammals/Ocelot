using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Ocelot.Routing.ServiceFabric.Models.ServiceFabric
{
    public class ServiceFabricServiceManifestExtensionLabel
    {
        [XmlAttribute(AttributeName = "Key")]
        public string Key { get; set; }

        [XmlText]
        public string Value { get; set; }
    }
}
