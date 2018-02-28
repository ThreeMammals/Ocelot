using System.Collections.ObjectModel;

namespace Butterfly.OpenTracing
{
    public class SpanReferenceCollection : Collection<SpanReference>
    {
        public static readonly SpanReferenceCollection Empty = new SpanReferenceCollection();
    }
}