using Microsoft.AspNetCore.Hosting;
using System.Runtime.InteropServices;
using System.Text;

namespace Ocelot.AcceptanceTests.Requester;

[Trait("Bug", "749")] // https://github.com/ThreeMammals/Ocelot/issues/749
[Trait("PR", "1769")] // https://github.com/ThreeMammals/Ocelot/pull/1769
public sealed class PayloadTooLargeTests : Steps
{
    private const string Payload =
        "[{\"_id\":\"6540f8ee7beff536c1304e3a\",\"index\":0,\"guid\":\"349307e2-5b1b-4ea9-8e42-d0d26b35059e\",\"isActive\":true,\"balance\":\"$2,458.86\",\"picture\":\"http://placehold.it/32x32\",\"age\":36,\"eyeColor\":\"blue\",\"name\":\"WalshSloan\",\"gender\":\"male\",\"company\":\"ENOMEN\",\"email\":\"walshsloan@enomen.com\",\"phone\":\"+1(818)463-2479\",\"address\":\"863StoneAvenue,Islandia,NewHampshire,7062\",\"about\":\"Exvelitelitutsintlaborisofficialaborisreprehenderittemporsitminim.Exveniamexetesse.Reprehenderitirurealiquipsuntnostrudcillumaliquipsuntvoluptateessenisivoluptatetemporexercitationsint.Laborumexestipsumincididuntvelit.Idnisiproidenttemporelitnonconsequatestnostrudmollit.\\r\\n\",\"registered\":\"2014-11-13T01:53:09-01:00\",\"latitude\":-1.01137,\"longitude\":160.133312,\"tags\":[\"nisi\",\"eu\",\"anim\",\"ipsum\",\"fugiat\",\"excepteur\",\"culpa\"],\"friends\":[{\"id\":0,\"name\":\"MayNoel\"},{\"id\":1,\"name\":\"RichardsDiaz\"},{\"id\":2,\"name\":\"JannieHarvey\"}],\"greeting\":\"Hello,WalshSloan!Youhave6unreadmessages.\",\"favoriteFruit\":\"banana\"},{\"_id\":\"6540f8ee39e04d0ac854b05d\",\"index\":1,\"guid\":\"0f210e11-94a1-45c7-84a4-c2bfcbe0bbfb\",\"isActive\":false,\"balance\":\"$3,371.91\",\"picture\":\"http://placehold.it/32x32\",\"age\":25,\"eyeColor\":\"green\",\"name\":\"FergusonIngram\",\"gender\":\"male\",\"company\":\"DOGSPA\",\"email\":\"fergusoningram@dogspa.com\",\"phone\":\"+1(804)599-2376\",\"address\":\"130RiverStreet,Bellamy,DistrictOfColumbia,9522\",\"about\":\"Duisvoluptatemollitullamcomollitessedolorvelit.Nonpariaturadipisicingsintdoloranimveniammollitdolorlaborumquisnulla.Ametametametnonlaborevoluptate.Eiusmoddocupidatatveniamirureessequiullamcoincididuntea.\\r\\n\",\"registered\":\"2014-11-01T03:51:36-01:00\",\"latitude\":-57.122954,\"longitude\":-91.22665,\"tags\":[\"nostrud\",\"ipsum\",\"id\",\"cupidatat\",\"consectetur\",\"labore\",\"ullamco\"],\"friends\":[{\"id\":0,\"name\":\"TabithaHuffman\"},{\"id\":1,\"name\":\"LydiaStark\"},{\"id\":2,\"name\":\"FaithStuart\"}],\"greeting\":\"Hello,FergusonIngram!Youhave3unreadmessages.\",\"favoriteFruit\":\"banana\"}]";

    [Fact]
    public void Should_throw_payload_too_large_exception_using_kestrel()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, HttpMethod.Post);
        var configuration = GivenConfiguration(route);
        var body = Body();
        this
            .Given(x => GivenThereIsAServiceRunningOn(port, body))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningOnKestrelWithCustomBodyMaxSize(1024))
            .When(x => WhenIPostUrlOnTheApiGateway("/", new ByteArrayContent(Encoding.UTF8.GetBytes(Payload))))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.RequestEntityTooLarge))
            .And(x => ThenTheResponseBodyShouldBeEmpty())
        .BDDfy();
    }

    [Fact]
    public void Should_throw_payload_too_large_exception_using_http_sys()
    {
        Assert.SkipUnless(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Test is unstable for all platforms except Windows OS");

        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, HttpMethod.Post);
        var configuration = GivenConfiguration(route);
        var body = Body();
        this
            .Given(x => GivenThereIsAServiceRunningOn(port, body))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningOnHttpSysWithCustomBodyMaxSize(1024))
            .When(x => WhenIPostUrlOnTheApiGateway("/", new ByteArrayContent(Encoding.UTF8.GetBytes(Payload))))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.RequestEntityTooLarge))
            .And(x => ThenTheResponseBodyShouldBeEmpty())
        .BDDfy();
    }

    private Task<int> GivenOcelotIsRunningOnKestrelWithCustomBodyMaxSize(long customBodyMaxSize)
        => GivenOcelotHostIsRunning(
            null, null, null, null,
            host => host.UseKestrel(options => options.Limits.MaxRequestBodySize = customBodyMaxSize),
            null, null);

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1416 // Validate platform compatibility
    private Task<int> GivenOcelotIsRunningOnHttpSysWithCustomBodyMaxSize(long customBodyMaxSize)
    {
        int port = PortFinder.GetRandomPort();
        var ocelotUrl = DownstreamUrl(port);
        void ConfigureHttpSys(IWebHostBuilder builder) => builder
                .ConfigureAppConfiguration(WithBasicConfiguration)
                .ConfigureServices(WithAddOcelot)
                .Configure(WithUseOcelot)
                .UseUrls(ocelotUrl)
                .UseHttpSys(options => options.MaxRequestBodySize = customBodyMaxSize);
        return GivenOcelotHostIsRunning(null, null, null, ConfigureHttpSys, null, null,
            client => client.BaseAddress = new(ocelotUrl));
    }
#pragma warning restore CA1416
#pragma warning restore IDE0079
}
