namespace Ocelot.Requester.QoS
{
    public interface IQoSProvider
    {
        CircuitBreaker CircuitBreaker { get; }
    }
}