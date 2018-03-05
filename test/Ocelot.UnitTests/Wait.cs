namespace Ocelot.UnitTests
{
    public class Wait
    {
        public static Waiter WaitFor(int milliSeconds)
        {
            return new Waiter(milliSeconds);
        }
    }
}