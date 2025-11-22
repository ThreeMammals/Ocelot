using System;

namespace Ocelot.Filter
{
    public class HttpStatusCodeFilter : Filter<HttpStatusCode>
    {
        public HttpStatusCodeFilter() { }

        public HttpStatusCodeFilter(FilterType filterType, HttpStatusCode[] values) : base(filterType, values ?? Enum.GetValues(typeof(HttpStatusCode)).Cast<HttpStatusCode>().ToArray())
        {
        }

        public HttpStatusCodeFilter(FilterType filterType, string[] values) :base(filterType, ParseInput(values))
        {
        }

        static HttpStatusCode[] ParseInput(string[] values)
        {
            List<HttpStatusCode> valuesList = new List<HttpStatusCode>();
            if (values == null)
            {
                valuesList.AddRange(Enum.GetValues(typeof(HttpStatusCode)).Cast<HttpStatusCode>());
            }
            else
            {
                foreach (var value in values)
                {
                    int i;
                    if (int.TryParse(value, out i))
                    {
                        if (Enum.IsDefined(typeof(HttpStatusCode), i))
                        {
                            valuesList.Add((HttpStatusCode)i);
                        }
                    }
                    else
                    {
                        foreach (var code in Enum.GetValues<HttpStatusCode>())
                        {
                            string codeClass = ((int)code / 100) + "xx";
                            bool code_is_in_range = string.Equals(value, codeClass, StringComparison.OrdinalIgnoreCase);
                            if (code_is_in_range)
                            {
                                valuesList.Add(code);
                            }
                        }
                    }
                }
            }

            return valuesList.Distinct().ToArray();
        }
    }
}
