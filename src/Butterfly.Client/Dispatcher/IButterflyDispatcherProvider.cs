using System;
using System.Collections.Generic;
using System.Text;

namespace Butterfly.Client
{
    public interface IButterflyDispatcherProvider
    {
        IButterflyDispatcher GetDispatcher();
    }
}
