using Ocelot.Values;

namespace Ocelot.LoadBalancer;

public struct Lease : IEquatable<Lease>
{
    public Lease()
    {
        HostAndPort = null;
        Connections = 0;
    }

    public Lease(Lease from)
    {
        HostAndPort = from.HostAndPort;
        Connections = from.Connections;
    }

    public Lease(ServiceHostAndPort hostAndPort)
    {
        HostAndPort = hostAndPort;
        Connections = 0;
    }

    public Lease(ServiceHostAndPort hostAndPort, int connections)
    {
        HostAndPort = hostAndPort;
        Connections = connections;
    }

    public ServiceHostAndPort HostAndPort { get; }
    public int Connections { get; set; }

    public static Lease Null => new();

    public override readonly string ToString() => $"({HostAndPort}+{Connections})";
    public override readonly int GetHashCode() => HostAndPort.GetHashCode();
    public override readonly bool Equals(object obj) => obj is Lease l && this == l;
    public readonly bool Equals(Lease other) => this == other;

    /// <summary>Checks equality of two leases.</summary>
    /// <remarks>
    /// <para>Override default implementation of <see cref="ValueType.Equals(object)"/> because we want to ignore the <see cref="Connections"/> property.</para>
    /// Microsoft Learn | .NET | C# Docs:
    ///   <list type="bullet">
    ///   <item><seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/equality-operators">Equality operators</seealso></item>
    ///   <item><seealso href="https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-object-equals">System.Object.Equals method</seealso></item>
    ///   <item><seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1.equals?view=net-8.0">IEquatable&lt;T&gt;.Equals(T) Method</seealso></item>
    ///   <item><seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.valuetype.equals?view=net-8.0">ValueType.Equals(Object) Method</seealso></item>
    ///   </list>
    /// </remarks>
    /// <param name="x">First operand.</param>
    /// <param name="y">Second operand.</param>
    /// <returns><see langword="true"/> if both operands are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(Lease x, Lease y) => x.HostAndPort == y.HostAndPort; // ignore -> x.Connections == y.Connections;
    public static bool operator !=(Lease x, Lease y) => !(x == y);

    public static bool operator ==(ServiceHostAndPort h, Lease l) => h == l.HostAndPort;
    public static bool operator !=(ServiceHostAndPort h, Lease l) => !(h == l);

    public static bool operator ==(Lease l, ServiceHostAndPort h) => l.HostAndPort == h;
    public static bool operator !=(Lease l, ServiceHostAndPort h) => !(l == h);
}
