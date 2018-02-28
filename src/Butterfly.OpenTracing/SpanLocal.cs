#if NET45
using System.Runtime.Remoting.Messaging;
#else
using System.Threading;
#endif


namespace Butterfly.OpenTracing
{
    internal static class SpanLocal
    {
#if NET45 || NET451 || NET452
        private const string SpanKey = "butterfly-span";
#else
        private static readonly AsyncLocal<ISpan> AsyncLocal = new AsyncLocal<ISpan>();
#endif

        public static ISpan Current
        {
#if NET45 || NET451 || NET452
            get
            {
                return CallContext.LogicalGetData(SpanKey) as ISpan;
            }
            set
            {
                CallContext.LogicalSetData(SpanKey, value);
            }
#else
            get
            {
                return AsyncLocal.Value;
            }
            set
            {
                AsyncLocal.Value = value;
            }
#endif
        }
    }
}