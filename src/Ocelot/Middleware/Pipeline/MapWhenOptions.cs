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
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _predicate = value;
            }
        }

        public OcelotRequestDelegate Branch { get; set; }
    }
}
