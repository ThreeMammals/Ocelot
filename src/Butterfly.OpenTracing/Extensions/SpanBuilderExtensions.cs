using System;
using System.Collections.Generic;
using System.Text;

namespace Butterfly.OpenTracing
{
    public static class SpanBuilderExtensions
    {
        public static ISpanBuilder AsChildOf(this ISpanBuilder spanBuilder, ISpanContext spanContext)
        {
            if (spanBuilder == null)
            {
                throw new ArgumentNullException(nameof(spanBuilder));
            }
            spanBuilder.References.Add(new SpanReference(SpanReferenceOptions.ChildOf, spanContext));
            return spanBuilder;
        }

        public static ISpanBuilder FollowsFrom(this ISpanBuilder spanBuilder, ISpanContext spanContext)
        {
            if (spanBuilder == null)
            {
                throw new ArgumentNullException(nameof(spanBuilder));
            }
            spanBuilder.References.Add(new SpanReference(SpanReferenceOptions.FollowsFrom, spanContext));
            return spanBuilder;
        }
    }
}
