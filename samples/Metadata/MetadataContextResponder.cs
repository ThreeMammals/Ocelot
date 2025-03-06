using Microsoft.Extensions.Primitives;
using Ocelot.Configuration;
using Ocelot.Headers;
using Ocelot.Middleware;
using Ocelot.Responder;
using System;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Unicode;
using ZstdNet;

namespace Ocelot.Samples.Metadata;

public class MetadataContextResponder : HttpContextResponder
{
    public MetadataContextResponder(IRemoveOutputHeaders removeOutputHeaders)
        : base(removeOutputHeaders) { }

    protected override async Task WriteToUpstreamAsync(HttpContext context, DownstreamResponse downstream)
    {
        //await base.WriteToUpstreamAsync(context, downstream);
        //return;

        // Ensure the route has metadata at all
        var route = context.Items.DownstreamRoute();
        var metadata = route?.MetadataOptions.Metadata;
        if ((metadata?.Count ?? 0) == 0)
        {
            await base.WriteToUpstreamAsync(context, downstream);
            return;
        }

        // Content type is 'application/json', so embed route metadata JSON-node
        var response = context.Items.DownstreamResponse();
        if (response.Content.Headers.ContentType?.MediaType == "application/json")
        {
            var json = await response.Content.ReadAsStringAsync(context.RequestAborted);
            json = await ReadJsonAsync(response.Content, context.RequestAborted);

            //var json1 = await JsonNode.ParseAsync(jsonStream, cancellationToken: context.RequestAborted);
            var json1 = JsonObject.Parse(json);
            var json2 = JsonSerializer.SerializeToNode(metadata);
            var aggregated = new JsonObject
            {
                [nameof(HttpContext.Response)] = json1,
                [nameof(MetadataOptions.Metadata)] = json2,
            };
            await WriteJsonAsync(context.Response, response.Content, aggregated, context.RequestAborted);
        }
        else
        {
            await base.WriteToUpstreamAsync(context, downstream);
        }
    }

    private static Encoding DetectEncoding(HttpContent content)
    {
        if (!string.IsNullOrEmpty(content.Headers.ContentType?.CharSet))
        {
            try
            {
                return Encoding.GetEncoding(content.Headers.ContentType.CharSet);
            }
            catch (ArgumentException)
            {
                return Encoding.UTF8; // unknown encoding, fallback to UTF-8
            }
        }
        return Encoding.UTF8; // default to UTF-8
    }

    private static async Task<string> ReadJsonAsync(HttpContent content, CancellationToken token)
    {
        var encoding = DetectEncoding(content);
        if (content.Headers.ContentEncoding.Contains("br")) // Brotli compression: https://www.prowaretech.com/articles/current/dot-net/compression-brotli
        {
            using var compressed = await content.ReadAsStreamAsync(token);
            var decompressed = await DecompressBrotliAsync(compressed, token);
            return encoding.GetString(decompressed);
        }
        else if (content.Headers.ContentEncoding.Contains("zstd")) // Zstandard compression: https://github.com/facebook/zstd
        {
            using var compressed = await content.ReadAsStreamAsync(token);
            var decompressed = await DecompressZstandardAsync(compressed, token);
            return encoding.GetString(decompressed);
        }
        else // no compression
        {
            using Stream contentStream = await content.ReadAsStreamAsync(token);
            using MemoryStream memoryStream = new();
            await contentStream.CopyToAsync(memoryStream, token);
            byte[] contentBytes = memoryStream.ToArray();
            return encoding.GetString(contentBytes);
        }
    }

    private static async Task WriteJsonAsync(HttpResponse to, HttpContent content, JsonObject json, CancellationToken token)
    {
        // We will not use original downstrean encoding, so defaults always to UTF8 for upstream
        var encoding = Encoding.UTF8; // DetectEncoding(content);
        var serialized = json.ToJsonString(JsonSerializerOptions.Web); // will not require compression
        var buffer = encoding.GetBytes(serialized);
        to.ContentLength = buffer.Length; // don't use chunked

        var ct = content.Headers.ContentType ?? new("application/json", encoding.HeaderName);
        ct.CharSet = encoding.HeaderName;
        to.ContentType = ct.ToString(); // always -> application/json; charset=utf-8

        if (content.Headers.ContentEncoding.Contains("br")) // Brotli compression
        {
            //var compressed = await CompressBrotliAsync(buffer, token);
            //var data = encoding.GetString(compressed);
            //await to.WriteAsync(data, encoding, token); // client app error: Decompression failed
            to.Headers.ContentEncoding = new("identity"); // don't compress with Brotli algo
        }
        else if (content.Headers.ContentEncoding.Contains("zstd")) // Zstandard compression (Facebook)
        {
            to.Headers.ContentEncoding = new("identity"); // don't compress with Zstandard algo
        }
        await to.Body.WriteAsync(buffer, token);
    }

    // https://learn.microsoft.com/en-us/dotnet/api/system.io.compression.brotlistream?view=net-9.0
    private static async Task<byte[]> CompressBrotliAsync(byte[] input, CancellationToken token)
    {
        using var output = new MemoryStream(input.Length);
        using var brotli = new BrotliStream(output, CompressionLevel.SmallestSize);
        await brotli.WriteAsync(input, token);
        await brotli.FlushAsync(token);
        return output.ToArray();
    }

    // https://learn.microsoft.com/en-us/dotnet/api/system.io.compression.brotlistream?view=net-9.0
    private static async Task<byte[]> DecompressBrotliAsync(Stream input, CancellationToken token)
    {
        using var output = new MemoryStream();
        using var brotli = new BrotliStream(input, CompressionMode.Decompress);
        await brotli.CopyToAsync(output, token);
        return output.ToArray();
    }

    // https://github.com/skbkontur/ZstdNet
    private static async Task<byte[]> DecompressZstandardAsync(Stream input, CancellationToken token)
    {
        using var output = new MemoryStream();
        await using var decompression = new DecompressionStream(input);
        await decompression.CopyToAsync(output, token);
        return output.ToArray();
    }
}
