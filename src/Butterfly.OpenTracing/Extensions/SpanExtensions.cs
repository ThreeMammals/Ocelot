﻿using System;

namespace Butterfly.OpenTracing
{
    public static class SpanExtensions
    {
        public static ISpan Tag(this ISpan span, string key, string value)
        {
            if (span == null)
            {
                throw new ArgumentNullException(nameof(span));
            }

            span.Tags[key] = value;
            return span;
        }

        public static ISpan Tag(this ISpan span, string key, bool value)
        {
            return Tag(span, key, value.ToString());
        }

        public static ISpan Tag(this ISpan span, string key, int value)
        {
            return Tag(span, key, value.ToString());
        }

        public static ISpan Tag(this ISpan span, string key, long value)
        {
            return Tag(span, key, value.ToString());
        }

        public static ISpan Tag(this ISpan span, string key, float value)
        {
            return Tag(span, key, value.ToString());
        }

        public static ISpan Tag(this ISpan span, string key, double value)
        {
            return Tag(span, key, value.ToString());
        }

        public static ISpan SetBaggage(this ISpan span, string key, string value)
        {
            if (span == null)
            {
                throw new ArgumentNullException(nameof(span));
            }

            span.SpanContext.SetBaggage(key, value);
            return span;
        }

        public static ISpan Log(this ISpan span, DateTime timestamp, LogField fields)
        {
            if (span == null)
            {
                throw new ArgumentNullException(nameof(span));
            }

            span.Logs.Add(new LogData(timestamp, fields));
            return span;
        }

        public static ISpan Log(this ISpan span, LogField fields)
        {
            return Log(span, DateTime.UtcNow, fields);
        }

        public static void Finish(this ISpan span)
        {
            span?.Finish(DateTimeOffset.UtcNow);
        }

        public static ISpan Exception(this ISpan span, Exception exception)
        {
            if (span == null)
            {
                return span;
            }

            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            span.Tags.Error(true);

            span.Log(LogField.CreateNew().EventError().ErrorKind(exception).Message(exception.Message));
            
            return span;
        }
    }
}