using System;

namespace Ocelot.Middleware.Pipeline
{
    public class MapWhenOptions
    {
        private Func<DownstreamContext, bool> _predicate;

        public Func<DownstreamContext, bool> Predicate
        {
            get
            {
                return _predicate;
            }
            set
            {
                _predicate = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        public OcelotRequestDelegate Branch { get; set; }
    }
}
