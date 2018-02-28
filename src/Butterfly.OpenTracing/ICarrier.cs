using System;
using System.Collections.Generic;
using System.Text;

namespace Butterfly.OpenTracing
{
    public interface ICarrier : IEnumerable<KeyValuePair<string, string>>
    {
        string this[string key] { get; set; }
    }
}