using System;

namespace Ocelot.Raft
{
    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Method|AttributeTargets.Property)]
    public class ExcludeFromCoverageAttribute : Attribute{}
}