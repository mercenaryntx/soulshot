using System;
using System.Collections;
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

        void CacheEntity(object entity);
        void CacheEntities(IEnumerable entities);
    }
}