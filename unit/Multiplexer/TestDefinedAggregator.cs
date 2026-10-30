using Microsoft.AspNetCore.Http;
using Ocelot.Middleware;
using Ocelot.Multiplexer;

namespace Ocelot.UnitTests.Multiplexer;

public class TestDefinedAggregator : IDefinedAggregator
{
    public async Task<DownstreamResponse> Aggregate(List<HttpContext> responses)
    {
        var tom = await responses[0].Items.DownstreamResponse().Content.ReadAsStringAsync();
        var laura = await responses[1].Items.DownstreamResponse().Content.ReadAsStringAsync();
        var content = $"{tom}, {laura}";
        var headers = responses.SelectMany(x => x.Items.DownstreamResponse().Headers).ToList();
        return new DownstreamResponse(new StringContent(content), HttpStatusCode.OK, headers, "some reason");
    }
}
