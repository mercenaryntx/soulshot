using System;
using Neurotoxin.Norm.Annotations;

namespace Neurotoxin.Norm
{
    public interface IDbSet
    {
        DbContext Context { get; }
        TableAttribute Table { get; }
        ColumnInfoCollection Columns { get; }
        ColumnInfo PrimaryKey { get; }
        Type EntityType { get; }

        void Init();
        void SaveChanges();
    }
}