using Microsoft.AspNetCore.Http;
using Ocelot.Responses;
using Ocelot.Values;

namespace Ocelot.LoadBalancer.LoadBalancers;

public class RoundRobin : ILoadBalancer
{
    private readonly Func<Task<List<Service>>> _servicesDelegate;
    private readonly string _serviceName;
    private readonly List<Lease> _leasing;

    public RoundRobin(Func<Task<List<Service>>> services, string serviceName)
    {
        _servicesDelegate = services;
        _serviceName = serviceName;
        _leasing = new();
    }

    //private int _last;
    private static readonly Dictionary<string, int> LastIndices = new();
    protected static readonly object SyncRoot = new();

    public event EventHandler<LeaseEventArgs> Leased;
    protected virtual void OnLeased(LeaseEventArgs e) => Leased?.Invoke(this, e);

    public virtual async Task<Response<ServiceHostAndPort>> Lease(HttpContext httpContext)
    {
        var services = await _servicesDelegate?.Invoke() ?? new List<Service>();
        if (services.Count == 0)
        {
            return new ErrorResponse<ServiceHostAndPort>(new ServicesAreEmptyError($"There were no services in {nameof(RoundRobin)} for '{_serviceName}' during {nameof(Lease)} operation!"));
        }

        lock (SyncRoot)
        {
            // Capture the count value because another thread might modify the list
            int count = services.Count;
            var readMe = new Service[count];
            services.CopyTo(readMe);

            LastIndices.TryGetValue(_serviceName, out int last);
            if (last >= count)
            {
                last = 0;
            }

            Service next = null;
            int index = last, stop = count;

            // Scan for the next online service instance which must be healthy
            while (next?.HostAndPort == null && stop-- > 0) // TODO Check real health status
            {
                index = last;
                next = readMe[last];
                LastIndices[_serviceName] = (++last < count) ? last : 0;
            }

            if (next == null)
            {
                return new ErrorResponse<ServiceHostAndPort>(new ServicesAreNullError($"The service at index {index} was null in {nameof(RoundRobin)} for {_serviceName} during the {nameof(Lease)} operation. Total services count: {count}."));
            }

            // Happy path: Lease now
            UpdateLeasing(readMe);
            Lease wanted = GetLease(next);
            int leaseIndex = Update(ref wanted, true); // perform counting based on Connections
            OnLeased(new(wanted, next, index, leaseIndex));
            return new OkResponse<ServiceHostAndPort>(next.HostAndPort); // but it should be actually new(wanted.HostAndPort)
        }
    }

    public virtual void Release(ServiceHostAndPort hostAndPort)
    { }

    private int Update(ref Lease item, bool increase)
    {
        var index = _leasing.IndexOf(item);
        _ = increase ? item.Connections++ : item.Connections--;
        _leasing[index] = item; // write the value back to the position
        return index;
    }

    private Lease GetLease(Service @for) => _leasing.Find(l => l == @for.HostAndPort);

    private void UpdateLeasing(IList<Service> services)
    {
        // Don't remove leasing data of old services, so keep data during life time of the load balancer
        // _leasing.RemoveAll(l => services.All(s => s?.HostAndPort != l));
        var newLeases = services
            .Where(s => s != null && !_leasing.Exists(l => l == s.HostAndPort))
            .Select(s => new Lease(s.HostAndPort))
            .ToArray(); // capture leasing state and produce new collection
        _leasing.AddRange(newLeases);
    }
}

public class LeaseEventArgs : EventArgs
{
    public LeaseEventArgs(Lease lease, Service service, int serviceIndex, int leaseIndex)
    {
        Lease = lease;
        Service = service;
        ServiceIndex = serviceIndex;
        LeaseIndex = leaseIndex;
    }

    public Lease Lease { get; }
    public Service Service { get; }

    public int ServiceIndex { get; }
    public int LeaseIndex { get; }
}
