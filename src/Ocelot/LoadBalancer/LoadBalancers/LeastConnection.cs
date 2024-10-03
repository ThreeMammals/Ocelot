using Microsoft.AspNetCore.Http;
using Ocelot.Responses;
using Ocelot.Values;

namespace Ocelot.LoadBalancer.LoadBalancers;

public class LeastConnection : ILoadBalancer
{
    private readonly Func<Task<List<Service>>> _services;
    private readonly List<Lease> _leases;
    private readonly string _serviceName;
    private static readonly object SyncRoot = new();

    public string Type => nameof(LeastConnection);

    public LeastConnection(Func<Task<List<Service>>> services, string serviceName)
    {
        _services = services;
        _serviceName = serviceName;
        _leases = new List<Lease>();
    }

    public event EventHandler<LeaseEventArgs> Leased;
    protected virtual void OnLeased(LeaseEventArgs e) => Leased?.Invoke(this, e);

    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        var services = await _services.Invoke();
        if ((services?.Count ?? 0) == 0)
        {
            return new ErrorResponse<ServiceHostAndPort>(new ServicesAreNullError($"Services were null/empty in {Type} for '{_serviceName}' during {nameof(LeaseAsync)} operation!"));
        }

        lock (SyncRoot)
        {
            //todo - maybe this should be moved somewhere else...? Maybe on a repeater on seperate thread? loop every second and update or something?
            UpdateLeasing(services);

            Lease wanted = GetLeaseWithLeastConnections();
            _ = Update(ref wanted, true);

            var index = services.FindIndex(s => s.HostAndPort == wanted);
            OnLeased(new(wanted, services[index], index));

            return new OkResponse<ServiceHostAndPort>(new(wanted.HostAndPort));
        }
    }

    public void Release(ServiceHostAndPort hostAndPort)
    {
        lock (SyncRoot)
        {
            var matchingLease = _leases.Find(l => l == hostAndPort);
            if (matchingLease != Lease.Null)
            {
                _ = Update(ref matchingLease, false);
            }
        }
    }

    private int Update(ref Lease item, bool increase)
    {
        var index = _leases.IndexOf(item);
        _ = increase ? item.Connections++ : item.Connections--;
        _leases[index] = item; // write the value back to the position
        return index;
    }

    private Lease GetLeaseWithLeastConnections()
    {
        var min = _leases.Min(l => l.Connections);
        return _leases.Find(l => l.Connections == min);
    }

    private void UpdateLeasing(List<Service> services)
    {
        if (_leases.Count > 0)
        {
            _leases.RemoveAll(l => !services.Exists(s => s.HostAndPort == l));

            services.Where(s => !_leases.Exists(l => l == s.HostAndPort))
                .ToList()
                .ForEach(s => _leases.Add(new(s.HostAndPort, 0)));
        }
        else
        {
            services.ForEach(s => _leases.Add(new(s.HostAndPort)));
        }
    }
}
