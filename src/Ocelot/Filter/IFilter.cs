namespace Ocelot.Filter
{
    public interface IFilter<T>
    {
        bool PassesFilter(T value);
    }
}
