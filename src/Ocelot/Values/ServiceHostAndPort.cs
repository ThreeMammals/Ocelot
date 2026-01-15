namespace Ocelot.Values;

public class ServiceHostAndPort : IEquatable<ServiceHostAndPort>
{
    public ServiceHostAndPort(ServiceHostAndPort from)
    {
        DownstreamHost = from.DownstreamHost;
        DownstreamPort = from.DownstreamPort;
        Scheme = from.Scheme;
    }

    public ServiceHostAndPort(string downstreamHost, int downstreamPort)
    {
        DownstreamHost = downstreamHost?.Trim('/');
        DownstreamPort = downstreamPort;
    }

    public ServiceHostAndPort(string downstreamHost, int downstreamPort, string scheme)
        : this(downstreamHost, downstreamPort) => Scheme = scheme;

    public string DownstreamHost { get; }
    public int DownstreamPort { get; }
    public string Scheme { get; }

    public override string ToString()
        => $"{Scheme}:{DownstreamHost}:{DownstreamPort}";
    public override int GetHashCode()
        => Tuple.Create(Scheme, DownstreamHost, DownstreamPort).GetHashCode();

    public bool Equals(ServiceHostAndPort other) => this == other;
    public override bool Equals(object obj)
        => obj != null && obj is ServiceHostAndPort o && this == o;

    /// <summary>Checks equality of two hosts.</summary>
    /// <remarks>Microsoft Learn | .NET | C# Docs:
    ///   <list type="bullet">
    ///   <item><seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/equality-operators">Equality operators</seealso></item>
    ///   <item><seealso href="https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-object-equals">System.Object.Equals method</seealso></item>
    ///   <item><seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1.equals?view=net-8.0">IEquatable&lt;T&gt;.Equals(T) Method</seealso></item>
    ///   </list>
    /// </remarks>
    /// <param name="l">Left operand.</param>
    /// <param name="r">Right operand.</param>
    /// <returns><see langword="true"/> if both operands are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(ServiceHostAndPort l, ServiceHostAndPort r)
        => (((object)l) == null || ((object)r) == null)
            ? Equals(l, r)
            : l.DownstreamHost == r.DownstreamHost && l.DownstreamPort == r.DownstreamPort && l.Scheme == r.Scheme;

    public static bool operator !=(ServiceHostAndPort l, ServiceHostAndPort r)
        => (((object)l) == null || ((object)r) == null)
            ? !Equals(l, r)
            : !(l == r);
}
