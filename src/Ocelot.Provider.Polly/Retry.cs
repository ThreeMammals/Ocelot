namespace Ocelot.Provider.Polly
{
    public class Retry
    {
        public Retry(params IAsyncPolicy[] policies)
        {
            Policies = policies.Where(p => p != null).ToArray();
        }

        public IAsyncPolicy[] Policies { get; }
    }
}
