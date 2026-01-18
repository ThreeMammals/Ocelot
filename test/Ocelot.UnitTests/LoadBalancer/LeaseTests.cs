using Ocelot.LoadBalancer;
using Ocelot.Values;
using Steeltoe.Connector;

namespace Ocelot.UnitTests.LoadBalancer;

public class LeaseTests
{
    [Fact]
    public void Ctor()
    {
        // Arrange, Act
        Lease l = new();

        // Assert
        Assert.Null(l.HostAndPort);
        Assert.Equal(0, l.Connections);
    }

    [Fact]
    public void Ctor_Lease()
    {
        // Arrange
        ServiceHostAndPort host = new("host", 123);
        Lease from = new(host, 3);

        // Act
        Lease actual = new(from);

        // Assert
        Assert.Equivalent(from, actual);
        Assert.Equivalent(3, actual.Connections);
    }

    [Fact]
    public void Ctor_ServiceHostAndPort()
    {
        // Arrange
        ServiceHostAndPort hostAndPort = new("host", 123);

        // Act
        Lease actual = new(hostAndPort);

        // Assert
        Assert.Equivalent(hostAndPort, actual.HostAndPort);
        Assert.Equal(hostAndPort, actual.HostAndPort);
        Assert.Equal(0, actual.Connections);
    }

    [Fact]
    public void Ctor_Init()
    {
        // Arrange
        ServiceHostAndPort hostAndPort = new("host", 123);
        int connections = 3;

        // Act
        Lease actual = new(hostAndPort, connections);

        // Assert
        Assert.Equivalent(hostAndPort, actual.HostAndPort);
        Assert.Equal(hostAndPort, actual.HostAndPort);
        Assert.Equal(3, actual.Connections);
    }

    [Fact]
    public void Null()
    {
        // Arrange, Act
        Lease actual = Lease.Null;

        // Assert
        Assert.Null(actual.HostAndPort);
        Assert.Equal(0, actual.Connections);
    }

    [Fact]
    public void ToString_HostPlusConnections()
    {
        // Arrange
        Lease l = new(new("host", 333, "ws"), 4);

        // Act
        var actual = l.ToString();

        // Assert
        Assert.NotNull(actual);
        Assert.Equal("(ws:host:333+4)", actual);
    }

    [Fact]
    public void Equals_object()
    {
        // Arrange, Act, Assert
        Lease l = Lease.Null;
        var boxed = (object)l;
        bool equality = l.Equals(boxed);
        Assert.True(equality);

        // Arrange, Act, Assert 
        l = new(new("host", 333, "ws"), 4);
        boxed = (object)l;
        equality = Lease.Null.Equals(boxed);
        Assert.False(equality);

        // Arrange, Act, Assert
        string s = "not Lease";
        boxed = (object)s;
        equality = l.Equals(boxed);
        Assert.False(equality);
    }

    [Fact]
    public void Equals_Lease()
    {
        // Arrange, Act, Assert : false
        Lease l = new(new("host", 333, "ws"), 4);
        Lease other = Lease.Null;
        bool equality = l.Equals(other);
        Assert.False(equality);

        // Arrange, Act, Assert : true
        equality = Lease.Null.Equals(other);
        Assert.True(equality);
    }

    [Fact]
    public void Op_Inequality_Lease_Lease()
    {
        // Arrange, Act, Assert : true
        Lease x = new(new("host", 333, "ws"), 4);
        Lease y = Lease.Null;
        bool equality = x != y;
        Assert.True(equality);

        // Arrange, Act, Assert : false
        equality = Lease.Null != y;
        Assert.False(equality);
    }

    [Fact]
    public void Op_Inequality_ServiceHostAndPort_Lease()
    {
        // Arrange, Act, Assert : false
        ServiceHostAndPort h = new("host", 333, "ws");
        Lease l = new(h, 1);
        bool equality = h != l;
        Assert.False(equality);

        // Arrange, Act, Assert : true
        equality = h != Lease.Null;
        Assert.True(equality);
    }

    [Fact]
    public void Op_Inequality_Lease_ServiceHostAndPort()
    {
        // Arrange, Act, Assert : false
        ServiceHostAndPort h = new("host", 333, "ws");
        Lease l = new(h, 1);
        bool equality = l != h;
        Assert.False(equality);

        // Arrange, Act, Assert : true
        equality = Lease.Null != h;
        Assert.True(equality);
    }
}
