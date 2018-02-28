using System;
using System.Collections;
using System.Collections.Generic;

namespace Butterfly.OpenTracing
{
    public class DelegatingCarrier<T> : ITextMapCarrier where T : class, IEnumerable
    {
        private readonly Action<T, string, string> _injector;
        private readonly Func<T, string, string> _extractor;
        private readonly T _carrier;
        private readonly Func<T, IEnumerator<KeyValuePair<string, string>>> _enumerator;

        public DelegatingCarrier(T carrier, Action<T, string, string> injector)
        {
            _carrier = carrier ?? throw new ArgumentNullException(nameof(carrier));
            _injector = injector;
        }

        public DelegatingCarrier(T carrier, Func<T, string, string> extractor, Func<T, IEnumerator<KeyValuePair<string, string>>> enumerator = null)
        {
            _carrier = carrier ?? throw new ArgumentNullException(nameof(carrier));
            _extractor = extractor;
            _enumerator = enumerator;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            if (_enumerator != null)
            {
                return _enumerator(_carrier);
            }

            return (IEnumerator<KeyValuePair<string, string>>) _carrier.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public string this[string key]
        {
            get => _extractor?.Invoke(_carrier, key);
            set => _injector?.Invoke(_carrier, key, value);
        }
    }
}