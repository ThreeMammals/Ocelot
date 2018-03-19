using System;
using System.Threading.Tasks;

namespace Ocelot.Middleware.Pipeline
{
    public class MapWhenMiddleware
    {
        private readonly OcelotRequestDelegate _next;
        private readonly MapWhenOptions _options;

        public MapWhenMiddleware(OcelotRequestDelegate next, MapWhenOptions options)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _next = next;
            _options = options;
        }

        public async Task Invoke(DownstreamContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (_options.Predicate(context))
            {
                await _options.Branch(context);
            }
            else
            {
                await _next(context);
            }
        }
    }
}
