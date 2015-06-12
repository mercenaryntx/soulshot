using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot
{
    public class ColumnInfoCollection : IEnumerable<ColumnInfo>
    {
        public Type BaseType { get; private set; }
        public TableAttribute Table { get; private set; }
        public List<CrossReference> CrossReferences { get; private set; }
        private Dictionary<string, ColumnInfo> _dictionary;

        public int Count
        {
            get { return _dictionary.Count; }
        }

        public ColumnInfoCollection(Type baseType, TableAttribute table, IEnumerable<ColumnInfo> collection = null)
        {
            BaseType = baseType;
            Table = table;
            CrossReferences = new List<CrossReference>();
            if (collection != null) SetCollection(collection);
        }

        public void SetCollection(IEnumerable<ColumnInfo> collection)
        {
            _dictionary = collection.ToDictionary(c => c.ColumnName, c => c);
        }

        public IEnumerator<ColumnInfo> GetEnumerator()
        {
            return _dictionary.Values.GetEnumerator();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (!obj.GetType().IsInstanceOfType(this) && !GetType().IsInstanceOfType(obj)) return false;
            return Equals((ColumnInfoCollection)obj);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public ColumnInfo this[string key]
        {
            get { return _dictionary[key]; }
        }

        public ColumnInfo this[int index]
        {
            get { return _dictionary.ElementAt(index).Value; }
        }

        protected bool Equals(IEnumerable<ColumnInfo> other)
        {
            return this.SequenceEqual(other);
        }

        public void SetValue(IEntityProxy instance, string columnName, object value, ColumnMapper mapper)
        {
            var parts = columnName.Split('.');
            var obj = instance;
            var columns = this;
            var type = GetObjectType(obj);
            if (typeof(IEntityProxy).IsAssignableFrom(type)) type = type.BaseType;
            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                var propertyName = columns[part].PropertyName;
                var pi = type.GetProperty(propertyName);
                if (pi == null || !pi.CanWrite) continue;
                if (i == parts.Length - 1)
                {
                    var mappedValue = mapper.MapToType(value, pi);
                    pi.SetValue(obj, mappedValue);
                    obj.State = EntityState.Unchanged;
                }
                else
                {
                    columns = columns[part].ReferenceTable;
                    obj = pi.GetValue(obj) as IEntityProxy;
                    type = GetObjectType(obj);
                }
            }
        }

        private Type GetObjectType(object obj)
        {
            var type = obj.GetType();
            if (typeof(IEntityProxy).IsAssignableFrom(type)) type = type.BaseType;
            return type;
        }
    }
}