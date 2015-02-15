using System.Collections.Generic;

namespace Neurotoxin.Norm
{
    public interface IDbSet
    {
        List<ColumnInfo> Columns { get; }
    }
}