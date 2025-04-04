using Ocelot.Configuration;
using Ocelot.Headers;
using Ocelot.Metadata;
using Ocelot.Middleware;
using Ocelot.Responder;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ZstdNet;

namespace Ocelot.Samples.Metadata;

public class MetadataResponder : HttpContextResponder
{
    public MetadataResponder(IRemoveOutputHeaders removeOutputHeaders)
        : base(removeOutputHeaders) { }

    protected override async Task WriteToUpstreamAsync(HttpContext context, DownstreamResponse downstream)
    {
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
            // Don't process json requested by scripts aka XHR
            if (route.GetMetadata("disableMetadataJson", false))
            {
                AddMetadataHeader(context, metadata!); // but return in the header
                await base.WriteToUpstreamAsync(context, downstream);
                return;
            }

            //var json = await response.Content.ReadAsStringAsync(context.RequestAborted);
            var json = await ReadCompressedJsonAsync(context.Response, response.Content, context.RequestAborted);
            if (string.IsNullOrEmpty(json))
            {
                // Impossible to decompress content and write to body
                // Write metadata to the contentEncoding and write original content
                AddMetadataHeader(context, metadata!);
                await base.WriteToUpstreamAsync(context, downstream);
                return;
            }

            //var json1 = await JsonNode.ParseAsync(jsonStream, cancellationToken: context.RequestAborted);
            var json1 = JsonObject.Parse(json);
            var json2 = JsonSerializer.SerializeToNode(metadata);
            var aggregated = new JsonObject
            {
                [nameof(HttpContext.Response)] = json1,
                [nameof(MetadataOptions.Metadata)] = json2,
            };
            AddMetadataHeader(context, metadata!);
            await WriteJsonAsync(context.Response, response.Content, aggregated, context.RequestAborted);
        }
        else
        {
            AddMetadataHeader(context, metadata!);
            await base.WriteToUpstreamAsync(context, downstream);
        }
    }

    private static void AddMetadataHeader(HttpContext context, IDictionary<string, string> metadata)
    {
        var node = JsonSerializer.SerializeToNode(metadata);
        var header = node?.ToJsonString(JsonSerializerOptions.Default/*Web*/) ?? string.Empty;
        context.Response.Headers.Append("OC-Route-Metadata", new(header));
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

    private static async Task<string> ReadCompressedJsonAsync(HttpResponse response, HttpContent content, CancellationToken token)
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
        else if (content.Headers.ContentEncoding.Contains("deflate")) // Deflate algorithm compression
        {
            // Actually it doesn't work: only MS compressed DeflateStream are supported
            // Decompressor will generate System.IO.InvalidDataException: The archive entry was compressed using an unsupported compression method.
            //var compressed = await content.ReadAsStreamAsync(token);
            //var decompressed = await DecompressDeflateAsync(compressed, token);
            //return encoding.GetString(decompressed);
            return string.Empty;
        }
        else if (content.Headers.ContentEncoding.Contains("gzip")) // GZip compression
        {
            var compressed = await content.ReadAsStreamAsync(token);
            var decompressed = DecompressGZip(compressed);
            return encoding.GetString(decompressed);
        }
        else // no compression
        {
            var buffer = await content.ReadAsByteArrayAsync(token);
            return encoding.GetString(buffer);
        }
    }

    private static Task WriteJsonAsync(HttpResponse to, HttpContent content, JsonObject json, CancellationToken token)
    {
        // We will not use original downstrean encoding, so defaults always to UTF8 for upstream
        var encoding = Encoding.UTF8; // DetectEncoding(content);
        var serialized = json.ToJsonString(JsonSerializerOptions.Default/*Web*/); // will not require compression
        var buffer = encoding.GetBytes(serialized);
        to.ContentLength = buffer.Length; // don't use chunked

        var ct = content.Headers.ContentType ?? new("application/json", encoding.HeaderName);
        ct.CharSet = encoding.HeaderName;
        to.ContentType = ct.ToString(); // always -> application/json; charset=utf-8

        var contentEncoding = content.Headers.ContentEncoding;
        if (contentEncoding.Contains("br")) // Brotli compression
        {
            //var compressed = await CompressBrotliAsync(buffer, token);
            //var data = encoding.GetString(compressed);
            //await to.WriteAsync(data, encoding, token); // client app error: Decompression failed
            to.Headers.ContentEncoding = new("identity"); // don't compress with Brotli algo
        }
        else if (contentEncoding.Contains("zstd")) // Zstandard compression (Facebook)
        {
            to.Headers.ContentEncoding = new("identity"); // don't compress with Zstandard algo
        }
        else if (contentEncoding.Contains("deflate")) // Deflate compression
        {
            // Do nothing, because of impossibility to decompress third-party streams
        }
        else if (contentEncoding.Contains("gzip")) // GZip compression
        {
            to.Headers.ContentEncoding = new("identity"); // don't compress with GZip algo
        }
        return to.Body.WriteAsync(buffer, 0, buffer.Length, token);
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
        using var decompressor = new BrotliStream(input, CompressionMode.Decompress);
        await decompressor.CopyToAsync(output, token);
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

    // https://learn.microsoft.com/en-us/dotnet/api/system.io.compression.deflatestream?view=net-9.0
    private static async Task<byte[]> DecompressDeflateAsync(Stream input, CancellationToken token)
    {
        using var output = new MemoryStream();
        using var decompressor = new DeflateStream(input, CompressionMode.Decompress);
        await decompressor.CopyToAsync(output, token);
        return output.ToArray();
    }

    // https://learn.microsoft.com/en-us/dotnet/api/system.io.compression.gzipstream?view=net-9.0
    public static byte[] DecompressGZip(Stream input)
    {
        using var output = new MemoryStream();
        using var decompressor = new GZipStream(input, CompressionMode.Decompress);
        decompressor.CopyTo(output);
        return output.ToArray();
    }
}
