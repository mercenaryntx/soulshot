using System;
using System.Collections.Generic;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot
{
    public interface IColumnInfoCollection : IEnumerable<ColumnInfo>
    {
        Type BaseType { get; }
        Type[] MappedTypes { get; }
        TableAttribute Table { get; }
        List<CrossReference> CrossReferences { get; }

        ColumnInfo this[string key] { get; }
        ColumnInfo this[int index] { get; }

        void SetCollection(IEnumerable<ColumnInfo> collection);
        void SetValue(IEntityProxy instance, string columnName, object value, ColumnMapper mapper);
    }
}