using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Ocelot.Routing.ServiceFabric.Models.ServiceFabric
{
    public class ServiceFabricManifestServiceTypes
    {
        [XmlElement(ElementName = "StatelessServiceType")]
        public ServiceFabricManifestStatelessServiceType StatelessServiceType { get; set; }
    }
}
