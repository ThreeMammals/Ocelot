using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Ocelot.Routing.ServiceFabric.Models.ServiceFabric
{
    public class ServiceFabricManifestEndpoint
    {
        [XmlAttribute(AttributeName = "Protocol")]
        public string Protocol { get; set; }
    }
}
