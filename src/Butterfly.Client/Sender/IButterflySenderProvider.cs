namespace Butterfly.Client
{
    public interface IButterflySenderProvider
    {
        IButterflySender GetSender();
    }
}