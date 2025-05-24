using GraphQL;
using GraphQL.NewtonsoftJson;
using System.Net;
using System.Net.Http.Headers;

namespace Ocelot.Samples.GraphQL;

public class GraphQLDelegatingHandler : DelegatingHandler
{
    //private readonly ISchema _schema;
    private readonly IDocumentExecuter _executer;
    private readonly IGraphQLTextSerializer _serializer;

    public GraphQLDelegatingHandler(IDocumentExecuter executer, IGraphQLTextSerializer serializer)
    {
        _executer = executer;
        _serializer = serializer;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        //try get query from body, could check http method :)
        var query = await request.Content!.ReadAsStringAsync(cancellationToken);

        //if not body try query string, dont hack like this in real world..
        if (query.Length == 0)
        {
            var decoded = WebUtility.UrlDecode(request.RequestUri!.Query);
            query = decoded.Replace("?query=", string.Empty);
        }

        var result = await _executer.ExecuteAsync(_ =>
        {
            _.Query = query;
        });

        // IGraphQLSerializer & IGraphQLTextSerializer: https://github.com/graphql-dotnet/graphql-dotnet/blob/master/docs2/site/docs/getting-started/transport.md#igraphqlserializer--igraphqltextserializer
        var responseBody = _serializer.Serialize(result);
        var media = new MediaTypeHeaderValue("application/graphql-response+json");
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, media),
        };

        //ocelot will treat this like any other http request...
        return response;
    }
}
