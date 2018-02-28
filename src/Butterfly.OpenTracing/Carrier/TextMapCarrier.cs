using System.Collections;
using System.Collections.Generic;

namespace Butterfly.OpenTracing
{
    public class TextMapCarrier : ITextMapCarrier
    {
        private readonly Dictionary<string, string> _carrier = new Dictionary<string, string>();

        public string this[string key] { get => _carrier[key]; set => _carrier[key] = value; }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _carrier.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _carrier.GetEnumerator();
        }
    }
}