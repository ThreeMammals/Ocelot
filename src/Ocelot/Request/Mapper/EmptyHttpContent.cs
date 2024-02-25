namespace Ocelot.Request.Mapper;

public class EmptyHttpContent : HttpContent
{
    protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
    {
        return Task.CompletedTask;
    }

    protected override bool TryComputeLength(out long length)
    {
        length = 0;
        return true;
    }
}
