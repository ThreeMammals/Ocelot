using Ocelot.Middleware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ocelot.Infrastructure
{
    public interface IOcelotPipeLineInfoRegistry
    {
        public void Add<T>(Type ocelotmiddleware) 
            where T : IOcelotMiddleware;
        public bool Exist<T>() 
            where T : IOcelotMiddleware;
    }
}
