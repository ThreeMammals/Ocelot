using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Ocelot.Configuration.File;
using System.Runtime.InteropServices;
using System.Text;

namespace Ocelot.AcceptanceTests.Requester;

public sealed class PayloadTooLargeTests : Steps
{
    private IHost _host;
    private const string Payload =
        "[{\"_id\":\"6540f8ee7beff536c1304e3a\",\"index\":0,\"guid\":\"349307e2-5b1b-4ea9-8e42-d0d26b35059e\",\"isActive\":true,\"balance\":\"$2,458.86\",\"picture\":\"http://placehold.it/32x32\",\"age\":36,\"eyeColor\":\"blue\",\"name\":\"WalshSloan\",\"gender\":\"male\",\"company\":\"ENOMEN\",\"email\":\"walshsloan@enomen.com\",\"phone\":\"+1(818)463-2479\",\"address\":\"863StoneAvenue,Islandia,NewHampshire,7062\",\"about\":\"Exvelitelitutsintlaborisofficialaborisreprehenderittemporsitminim.Exveniamexetesse.Reprehenderitirurealiquipsuntnostrudcillumaliquipsuntvoluptateessenisivoluptatetemporexercitationsint.Laborumexestipsumincididuntvelit.Idnisiproidenttemporelitnonconsequatestnostrudmollit.\\r\\n\",\"registered\":\"2014-11-13T01:53:09-01:00\",\"latitude\":-1.01137,\"longitude\":160.133312,\"tags\":[\"nisi\",\"eu\",\"anim\",\"ipsum\",\"fugiat\",\"excepteur\",\"culpa\"],\"friends\":[{\"id\":0,\"name\":\"MayNoel\"},{\"id\":1,\"name\":\"RichardsDiaz\"},{\"id\":2,\"name\":\"JannieHarvey\"}],\"greeting\":\"Hello,WalshSloan!Youhave6unreadmessages.\",\"favoriteFruit\":\"banana\"},{\"_id\":\"6540f8ee39e04d0ac854b05d\",\"index\":1,\"guid\":\"0f210e11-94a1-45c7-84a4-c2bfcbe0bbfb\",\"isActive\":false,\"balance\":\"$3,371.91\",\"picture\":\"http://placehold.it/32x32\",\"age\":25,\"eyeColor\":\"green\",\"name\":\"FergusonIngram\",\"gender\":\"male\",\"company\":\"DOGSPA\",\"email\":\"fergusoningram@dogspa.com\",\"phone\":\"+1(804)599-2376\",\"address\":\"130RiverStreet,Bellamy,DistrictOfColumbia,9522\",\"about\":\"Duisvoluptatemollitullamcomollitessedolorvelit.Nonpariaturadipisicingsintdoloranimveniammollitdolorlaborumquisnulla.Ametametametnonlaborevoluptate.Eiusmoddocupidatatveniamirureessequiullamcoincididuntea.\\r\\n\",\"registered\":\"2014-11-01T03:51:36-01:00\",\"latitude\":-57.122954,\"longitude\":-91.22665,\"tags\":[\"nostrud\",\"ipsum\",\"id\",\"cupidatat\",\"consectetur\",\"labore\",\"ullamco\"],\"friends\":[{\"id\":0,\"name\":\"TabithaHuffman\"},{\"id\":1,\"name\":\"LydiaStark\"},{\"id\":2,\"name\":\"FaithStuart\"}],\"greeting\":\"Hello,FergusonIngram!Youhave3unreadmessages.\",\"favoriteFruit\":\"banana\"}]";

    public PayloadTooLargeTests()
    {
    }

    public override void Dispose()
    {
        _host?.Dispose();
        base.Dispose();
    }

    [Fact]
    public void Should_throw_payload_too_large_exception_using_kestrel()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, HttpMethods.Post);
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningOnKestrelWithCustomBodyMaxSize(1024))
            .When(x => WhenIPostUrlOnTheApiGateway("/", new ByteArrayContent(Encoding.UTF8.GetBytes(Payload))))
            .Then(x => ThenTheStatusCodeShouldBe((int)HttpStatusCode.RequestEntityTooLarge))
            .BDDfy();
    }

    [SkippableFact]
    public void Should_throw_payload_too_large_exception_using_http_sys()
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, HttpMethods.Post);
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningOnHttpSysWithCustomBodyMaxSize(1024))
            .When(x => WhenIPostUrlOnTheApiGateway("/", new ByteArrayContent(Encoding.UTF8.GetBytes(Payload))))
            .Then(x => ThenTheStatusCodeShouldBe((int)HttpStatusCode.RequestEntityTooLarge))
            .BDDfy();
    }

    private static FileRoute GivenRoute(int port, string method = null) => new()
    {
        DownstreamPathTemplate = "/",
        DownstreamHostAndPorts = new()
        {
            new("localhost", port),
        },
        DownstreamScheme = Uri.UriSchemeHttp,
        UpstreamPathTemplate = "/",
        UpstreamHttpMethod = new() {method ?? HttpMethods.Get },
    };

    private void GivenThereIsAServiceRunningOn(int port)
        => handler.GivenThereIsAServiceRunningOn(port, MapOK);
    private static Task MapOK(HttpContext context)
    {
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        return context.Response.WriteAsync(string.Empty);
    }

    private async Task GivenOcelotIsRunningOnKestrelWithCustomBodyMaxSize(long customBodyMaxSize)
    {
        var ocelotUrl = DownstreamUrl(PortFinder.GetRandomPort());
        _host = await GivenOcelotHostIsRunning(null, null, null, builder => builder
                .UseUrls(ocelotUrl)
                .UseKestrel()
                .ConfigureKestrel((_, options) => options.Limits.MaxRequestBodySize = customBodyMaxSize));
        ocelotClient = new HttpClient
        {
            BaseAddress = new Uri(ocelotUrl),
        };
    }

    private async Task GivenOcelotIsRunningOnHttpSysWithCustomBodyMaxSize(long customBodyMaxSize)
    {
        var ocelotUrl = DownstreamUrl(PortFinder.GetRandomPort());
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1416 // Validate platform compatibility
        _host = await GivenOcelotHostIsRunning(null, null, null, builder => builder
                .UseUrls(ocelotUrl)
                .UseHttpSys(options => options.MaxRequestBodySize = customBodyMaxSize));
#pragma warning restore CA1416
#pragma warning restore IDE0079
        ocelotClient = new HttpClient
        {
            BaseAddress = new Uri(ocelotUrl),
        };
    }
}
