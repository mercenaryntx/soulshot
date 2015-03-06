using System.Collections.Generic;

namespace Neurotoxin.Soulshot
{
    public interface IEntityProxy
    {
        EntityState State { get; set; }
        HashSet<string> DirtyProperties { get; }
    }
}