using Consul;

namespace Ocelot.AcceptanceTests.ServiceDiscovery;

internal static class ConsulAgentServiceExtensions
{
    public static AgentService WithServiceName(this AgentService agent, string serviceName)
    {
        agent.Service = serviceName;
        return agent;
    }

    public static AgentService WithPort(this AgentService agent, int port)
    {
        agent.Port = port;
        return agent;
    }

    public static AgentService WithAddress(this AgentService agent, string address)
    {
        agent.Address = address;
        return agent;
    }

    public static ServiceEntry ToServiceEntry(this AgentService agent) => new()
    {
        Service = agent,
    };
}
