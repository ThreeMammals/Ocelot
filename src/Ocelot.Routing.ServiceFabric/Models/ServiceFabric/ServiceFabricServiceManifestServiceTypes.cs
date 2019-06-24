using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Ocelot.Routing.ServiceFabric.Models.ServiceFabric
{
    public class ServiceFabricServiceManifestServiceTypes
    {
        [XmlElement(ElementName = "StatelessServiceType")]
        public ServiceFabricServiceManifestStatelessServiceType StatelessServiceType { get; set; }
    }
}
