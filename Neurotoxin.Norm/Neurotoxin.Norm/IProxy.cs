using System.Collections.Generic;

namespace Neurotoxin.Norm
{
    public interface IProxy
    {
        EntityState State { get; set; }
        HashSet<string> DirtyProperties { get; }
    }
}