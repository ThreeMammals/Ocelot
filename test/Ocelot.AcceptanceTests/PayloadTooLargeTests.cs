using Ocelot.Configuration.File;
using System.Text;
using Microsoft.AspNetCore.Http;
using System.Runtime.InteropServices;

namespace Ocelot.AcceptanceTests;

public class PayloadTooLargeTests : IDisposable
{
    private readonly Steps _steps;
    private readonly ServiceHandler _serviceHandler;

    private const string Payload =
        "[{\"_id\":\"6540f8ee7beff536c1304e3a\",\"index\":0,\"guid\":\"349307e2-5b1b-4ea9-8e42-d0d26b35059e\",\"isActive\":true,\"balance\":\"$2,458.86\",\"picture\":\"http://placehold.it/32x32\",\"age\":36,\"eyeColor\":\"blue\",\"name\":\"WalshSloan\",\"gender\":\"male\",\"company\":\"ENOMEN\",\"email\":\"walshsloan@enomen.com\",\"phone\":\"+1(818)463-2479\",\"address\":\"863StoneAvenue,Islandia,NewHampshire,7062\",\"about\":\"Exvelitelitutsintlaborisofficialaborisreprehenderittemporsitminim.Exveniamexetesse.Reprehenderitirurealiquipsuntnostrudcillumaliquipsuntvoluptateessenisivoluptatetemporexercitationsint.Laborumexestipsumincididuntvelit.Idnisiproidenttemporelitnonconsequatestnostrudmollit.\\r\\n\",\"registered\":\"2014-11-13T01:53:09-01:00\",\"latitude\":-1.01137,\"longitude\":160.133312,\"tags\":[\"nisi\",\"eu\",\"anim\",\"ipsum\",\"fugiat\",\"excepteur\",\"culpa\"],\"friends\":[{\"id\":0,\"name\":\"MayNoel\"},{\"id\":1,\"name\":\"RichardsDiaz\"},{\"id\":2,\"name\":\"JannieHarvey\"}],\"greeting\":\"Hello,WalshSloan!Youhave6unreadmessages.\",\"favoriteFruit\":\"banana\"},{\"_id\":\"6540f8ee39e04d0ac854b05d\",\"index\":1,\"guid\":\"0f210e11-94a1-45c7-84a4-c2bfcbe0bbfb\",\"isActive\":false,\"balance\":\"$3,371.91\",\"picture\":\"http://placehold.it/32x32\",\"age\":25,\"eyeColor\":\"green\",\"name\":\"FergusonIngram\",\"gender\":\"male\",\"company\":\"DOGSPA\",\"email\":\"fergusoningram@dogspa.com\",\"phone\":\"+1(804)599-2376\",\"address\":\"130RiverStreet,Bellamy,DistrictOfColumbia,9522\",\"about\":\"Duisvoluptatemollitullamcomollitessedolorvelit.Nonpariaturadipisicingsintdoloranimveniammollitdolorlaborumquisnulla.Ametametametnonlaborevoluptate.Eiusmoddocupidatatveniamirureessequiullamcoincididuntea.\\r\\n\",\"registered\":\"2014-11-01T03:51:36-01:00\",\"latitude\":-57.122954,\"longitude\":-91.22665,\"tags\":[\"nostrud\",\"ipsum\",\"id\",\"cupidatat\",\"consectetur\",\"labore\",\"ullamco\"],\"friends\":[{\"id\":0,\"name\":\"TabithaHuffman\"},{\"id\":1,\"name\":\"LydiaStark\"},{\"id\":2,\"name\":\"FaithStuart\"}],\"greeting\":\"Hello,FergusonIngram!Youhave3unreadmessages.\",\"favoriteFruit\":\"banana\"}]";

    public PayloadTooLargeTests()
    {
        _serviceHandler = new ServiceHandler();
        _steps = new Steps();
    }

    [Fact]
    public void should_throw_payload_too_large_exception_using_kestrel()
    {
        var port = PortFinder.GetRandomPort();

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    DownstreamScheme = "http",
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = new List<string> { "POST" },
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}"))
            .And(x => _steps.GivenThereIsAConfiguration(configuration))
            .And(x => _steps.GivenOcelotIsRunningOnKestrelWithCustomBodyMaxSize(1024))
            .When(x => _steps.WhenIPostUrlOnTheApiGateway("/", new ByteArrayContent(Encoding.UTF8.GetBytes(Payload))))
            .Then(x => _steps.ThenTheStatusCodeShouldBe((int)HttpStatusCode.RequestEntityTooLarge))
            .BDDfy();
    }

    [SkippableFact]
    public void should_throw_payload_too_large_exception_using_http_sys()
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

        var port = PortFinder.GetRandomPort();

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    DownstreamScheme = "http",
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = new List<string> { "POST" },
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}"))
            .And(x => _steps.GivenThereIsAConfiguration(configuration))
            .And(x => _steps.GivenOcelotIsRunningOnHttpSysWithCustomBodyMaxSize(1024))
            .When(x => _steps.WhenIPostUrlOnTheApiGateway("/", new ByteArrayContent(Encoding.UTF8.GetBytes(Payload))))
            .Then(x => _steps.ThenTheStatusCodeShouldBe((int)HttpStatusCode.RequestEntityTooLarge))
            .BDDfy();
    }

    private void GivenThereIsAServiceRunningOn(string baseUrl)
    {
        _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, async context =>
        {
            context.Response.StatusCode = 200;
            await context.Response.WriteAsync(string.Empty);
        });
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        _serviceHandler?.Dispose();
        _steps?.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
