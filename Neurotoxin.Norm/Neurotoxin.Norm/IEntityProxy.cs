using System.Collections.Generic;

namespace Neurotoxin.Norm
{
    public interface IEntityProxy
    {
        EntityState State { get; set; }
        HashSet<string> DirtyProperties { get; }
    }
}