using System.Collections.Generic;

namespace Neurotoxin.Soulshot
{
    public interface IEntityProxy
    {
        object GeneratedFrom { get; set; }
        EntityState State { get; set; }
        HashSet<string> DirtyProperties { get; }
    }
}