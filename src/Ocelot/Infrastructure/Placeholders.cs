using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.Responses;
using System.Net.Sockets;

namespace Ocelot.Infrastructure;

public class Placeholders : IPlaceholders
{
    private readonly Dictionary<string, Func<Response<string>>> _placeholders;
    private readonly Dictionary<string, Func<DownstreamRequest, string>> _requestPlaceholders;
    private readonly IBaseUrlFinder _finder;
    private readonly IRequestScopedDataRepository _repo;
    private readonly IHttpContextAccessor _contextAccessor;

    public Placeholders(IBaseUrlFinder finder, IRequestScopedDataRepository repo, IHttpContextAccessor contextAccessor)
    {
        _repo = repo;
        _contextAccessor = contextAccessor;
        _finder = finder;
        _placeholders = new Dictionary<string, Func<Response<string>>>
        {
            { "{BaseUrl}", GetBaseUrl },
            { "{TraceId}", GetTraceId },
            { "{RemoteIpAddress}", GetRemoteIpAddress },
            { "{UpstreamHost}", GetUpstreamHost },
        };

        _requestPlaceholders = new Dictionary<string, Func<DownstreamRequest, string>>
        {
            { "{DownstreamBaseUrl}", GetDownstreamBaseUrl },
        };
    }

    public Response<string> Get(string key)
    {
        if (_placeholders.ContainsKey(key))
        {
            var response = _placeholders[key].Invoke();
            if (!response.IsError)
            {
                return new OkResponse<string>(response.Data);
            }
        }

        return new ErrorResponse<string>(new CouldNotFindPlaceholderError(key));
    }

    public Response<string> Get(string key, DownstreamRequest request)
    {
        return _requestPlaceholders.TryGetValue(key, out var func)
            ? new OkResponse<string>(func.Invoke(request))
            : new ErrorResponse<string>(new CouldNotFindPlaceholderError(key));
    }

    public Response Add(string key, Func<Response<string>> func)
    {
        return _placeholders.TryAdd(key, func)
            ? new OkResponse()
            : new ErrorResponse(new CannotAddPlaceholderError($"Unable to add placeholder: {key}, placeholder already exists"));
    }

    public Response Remove(string key)
    {
        if (!_placeholders.ContainsKey(key))
        {
            return new ErrorResponse(new CannotRemovePlaceholderError($"Unable to remove placeholder: {key}, placeholder does not exists"));
        }

        _placeholders.Remove(key);
        return new OkResponse();
    }

    private Response<string> GetRemoteIpAddress()
    {
        // this can blow up so adding try catch and return error
        try
        {
            var con = _contextAccessor.HttpContext.Connection;
            var req = _contextAccessor.HttpContext.Request;
            var ip = con?.RemoteIpAddress ?? con?.LocalIpAddress;

            // Reading from Host header: direct client-to-gateway connection scenario
            ip ??= IPAddress.TryParse(req.Host.ToString(), out var parsedIp) ? parsedIp
                : Dns.GetHostAddresses(req.Host.ToString()).First(a => a.AddressFamily != AddressFamily.InterNetworkV6);

            // Reading from X-Forwarded-For header: forwarded by a "gateway in the middle" connection scenario
            var forwarded = req.Headers.TryGetValue("X-Forwarded-For", out var values) ? values.ToString() : string.Empty;
            ip ??= IPAddress.TryParse(forwarded, out parsedIp) ? parsedIp : IPAddress.Loopback;

            var remoteIdAddress = ip?.ToString() ?? "localhost";
            return new OkResponse<string>(remoteIdAddress);
        }
        catch
        {
            return new ErrorResponse<string>(new CouldNotFindPlaceholderError("{RemoteIpAddress}"));
        }
    }

    private static string GetDownstreamBaseUrl(DownstreamRequest x)
    {
        var downstreamUrl = $"{x.Scheme}://{x.Host}";
        if (x.Port != 80 && x.Port != 443)
        {
            downstreamUrl = $"{downstreamUrl}:{x.Port}";
        }

        return $"{downstreamUrl}/";
    }

    private Response<string> GetTraceId()
    {
        var traceId = _repo.Get<string>("TraceId");
        return traceId.IsError
            ? new ErrorResponse<string>(traceId.Errors)
            : new OkResponse<string>(traceId.Data);
    }

    private Response<string> GetBaseUrl() => new OkResponse<string>(_finder.Find());

    private Response<string> GetUpstreamHost()
    {
        try
        {
            return _contextAccessor.HttpContext.Request.Headers.TryGetValue("Host", out var upstreamHost)
                ? new OkResponse<string>(upstreamHost.First())
                : new ErrorResponse<string>(new CouldNotFindPlaceholderError("{UpstreamHost}"));
        }
        catch
        {
            return new ErrorResponse<string>(new CouldNotFindPlaceholderError("{UpstreamHost}"));
        }
    }
}
