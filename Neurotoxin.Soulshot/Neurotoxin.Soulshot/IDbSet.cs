using System;
using System.Collections;
using System.Collections.Generic;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot
{
    public interface IDbSet
    {
        DbContext Context { get; }
        TableAttribute Table { get; }
        IColumnInfoCollection Columns { get; }
        ColumnInfo PrimaryKey { get; }
        Type EntityType { get; }

        void Init();
        void SaveChanges();

        void CacheEntity(object entity);
        void CacheEntities(IEnumerable entities);
        IEnumerable GetDiscriminatorValues(Type type);

        void UpdateTable(IEnumerable<ColumnInfo> storedColumns);
    }
}