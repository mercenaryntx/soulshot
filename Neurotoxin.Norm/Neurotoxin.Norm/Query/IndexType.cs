using System;

namespace Neurotoxin.Norm.Query
{
    [Flags]
    public enum IndexType
    {
        Default = 0,
        Unique = 2,
        Clustered = 4
    }
}