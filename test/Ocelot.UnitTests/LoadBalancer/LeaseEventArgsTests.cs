using Ocelot.LoadBalancer;
using Ocelot.Values;

namespace Ocelot.UnitTests.LoadBalancer;

public class LeaseEventArgsTests
{
    [Fact]
    public void Ctor()
    {
        // Arrange
        ServiceHostAndPort host = new("host", 123);
        Lease lease = new(host, 3);
        Service service = new("s", new("h", 123), "", "", []);

        // Act
        LeaseEventArgs args = new(lease, service, 3);

        // Assert
        Assert.Equal(lease, args.Lease);
        Assert.Equal(service, args.Service);
        Assert.Equal(3, args.ServiceIndex);
    }
}
