namespace Ocelot.Library.Middleware
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Repository;

    public class ClaimsParserMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;

        public ClaimsParserMiddleware(RequestDelegate next, IScopedRequestDataRepository scopedRequestDataRepository) 
            : base(scopedRequestDataRepository)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            
            await _next.Invoke(context);
        }
    }
}
