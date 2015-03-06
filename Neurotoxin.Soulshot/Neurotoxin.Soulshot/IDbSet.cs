using System;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot
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