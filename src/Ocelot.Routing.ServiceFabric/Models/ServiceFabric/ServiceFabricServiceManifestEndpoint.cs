using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Ocelot.Routing.ServiceFabric.Models.ServiceFabric
{
    public class ServiceFabricServiceManifestEndpoint
    {
        [XmlAttribute(AttributeName = "Protocol")]
        public string Protocol { get; set; }
    }
}
