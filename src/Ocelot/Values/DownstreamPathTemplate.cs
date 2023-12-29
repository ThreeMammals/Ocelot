﻿namespace Ocelot.Values
{
    public class DownstreamPathTemplate
    {
        public DownstreamPathTemplate(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public override string ToString() => Value ?? string.Empty;
    }
}
