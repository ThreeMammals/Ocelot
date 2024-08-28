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
            var readMe = CaptureState(services, out int count);
            if (!TryScanNext(readMe, out Service next, out int index))
            {
                return new ErrorResponse<ServiceHostAndPort>(new ServicesAreNullError($"The service at index {index} was null in {nameof(RoundRobin)} for {_serviceName} during the {nameof(Lease)} operation. Total services count: {count}."));
            }

            ProcessLeasing(readMe, next, index); // Happy path: Lease now
            return new OkResponse<ServiceHostAndPort>(next.HostAndPort);
        }
    }

    public virtual void Release(ServiceHostAndPort hostAndPort) { }

    /// <summary>Capture the count value because another thread might modify the list.</summary>
    /// <param name="services">Mutable collection of services.</param>
    /// <param name="count">Captured count value.</param>
    /// <returns>Captured collection as a <see cref="Array"/> object.</returns>
    private static Service[] CaptureState(List<Service> services, out int count)
    {
        // Capture the count value because another thread might modify the list
        count = services.Count;
        var readMe = new Service[count];
        services.CopyTo(readMe);
        return readMe;
    }

    /// <summary>Scan for the next online service instance which must be healthy.</summary>
    /// <param name="readme">Read-only collection.</param>
    /// <param name="next">The next online service to return.</param>
    /// <param name="index">The index of the next service to return.</param>
    /// <returns><see langword="true"/> if found next online service; otherwise <see langword="false"/>.</returns>
    private bool TryScanNext(Service[] readme, out Service next, out int index)
    {
        int length = readme.Length, stop = length;
        LastIndices.TryGetValue(_serviceName, out int last);
        if (last >= length)
        {
            last = 0;
        }

        next = null;
        index = last;

        // Scan for the next service instance
        // TODO Check real health status
        while (next?.HostAndPort == null && stop-- > 0)
        {
            index = last;
            next = readme[last];
            LastIndices[_serviceName] = (++last < length) ? last : 0;
        }

        return next != null;
    }

    private void ProcessLeasing(Service[] readme, Service next, int index)
    {
        UpdateLeasing(readme);
        Lease wanted = GetLease(next);
        _ = Update(ref wanted, true); // perform counting based on Connections
        OnLeased(new(wanted, next, index));
    }

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
