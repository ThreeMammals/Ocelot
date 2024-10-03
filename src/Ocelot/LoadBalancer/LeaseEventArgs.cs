using Ocelot.Values;

namespace Ocelot.LoadBalancer;

public class LeaseEventArgs : EventArgs
{
    public LeaseEventArgs(Lease lease, Service service, int serviceIndex)
    {
        Lease = lease;
        Service = service;
        ServiceIndex = serviceIndex;
    }

    public Lease Lease { get; }
    public Service Service { get; }
    public int ServiceIndex { get; }
}
