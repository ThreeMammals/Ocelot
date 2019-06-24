using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Ocelot.Routing.ServiceFabric.Models.ServiceFabric
{
    public class ServiceFabricServiceManifestExtension
    {
        [XmlArray(ElementName = "Labels", Namespace = "http://schemas.microsoft.com/2015/03/fabact-no-schema")]
        [XmlArrayItem("Label", IsNullable = false)]
        public ServiceFabricServiceManifestExtensionLabel[] Labels { get; set; }

        [XmlAttribute(AttributeName = "Name")]
        public string Name { get; set; }
    }
}
