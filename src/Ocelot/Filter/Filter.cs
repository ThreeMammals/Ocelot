using Ocelot.Configuration.File;

namespace Ocelot.Filter
{
    public abstract class Filter<T> : IFilter<T>
    {
        public FilterType FilterType { get; set; }
        public T[] Values { get; set; }

        public Filter() { }
        public Filter(FilterType filterType, T[] values)
        {
            FilterType = filterType;
            Values = values;
        }

        public virtual bool PassesFilter(T value)
        {
            // there's probably a clever way to clean this up but this is legible.
            if (FilterType == FilterType.Blacklist)
            {
                foreach (var item in Values)
                {
                    if (value.Equals(item))
                    {
                        return false; // the value is in the blacklist, so it can't pass the filter
                    }
                }

                return true; // the value wasn't found in the blacklist, so it passes the filter
            }
            else // filter is whitelist
            {
                foreach (var item in Values)
                {
                    if (value.Equals(item))
                    {
                        return true; // the value is in the whitelist, so it passes the filter
                    }
                }

                return false; // the value is not in the whitelist, so it doesn't pass the filter
            }
        }
    }
}
