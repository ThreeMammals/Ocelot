namespace Ocelot.Infrastructure
{
    public class Wait
    {
        public static Waiter WaitFor(int milliSeconds)
        {
            return new Waiter(milliSeconds);
        }
    }
}
