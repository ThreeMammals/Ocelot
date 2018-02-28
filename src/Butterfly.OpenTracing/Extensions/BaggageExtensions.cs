using System;
using System.Collections.Generic;

namespace Butterfly.OpenTracing
{
    public static class BaggageExtensions
    {
        public static Baggage Merge(this Baggage baggage, Baggage other)
        {
            return Merge(baggage, (IEnumerable<KeyValuePair<string, string>>)other);
        }

        public static Baggage Merge(this Baggage baggage, IEnumerable<KeyValuePair<string, string>> other)
        {
            if (baggage == null)
            {
                throw new ArgumentNullException(nameof(baggage));
            }
            if (other != null)
            {
                foreach (var item in other)
                {
                    baggage[item.Key] = item.Value;
                }
            }
            return baggage;
        }
    }
}