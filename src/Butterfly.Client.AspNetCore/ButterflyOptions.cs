using Microsoft.Extensions.Options;

namespace Butterfly.Client.AspNetCore
{
    public class ButterflyOptions : ButterflyConfig , IOptions<ButterflyOptions>
    {
        public ButterflyOptions Value => this;
    }
}