using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Neurotoxin.Soulshot.Annotations;
using Neurotoxin.Soulshot.Extensions;
using Neurotoxin.Soulshot.Query;

namespace Neurotoxin.Soulshot
{
    public class ColumnInfoCollection<TBase> : IColumnInfoCollection
    {
        public Type BaseType
        {
            get { return typeof (TBase); }
        }

        public Type[] MappedTypes { get; private set; }
        public TableAttribute Table { get; private set; }
        public List<CrossReference> CrossReferences { get; private set; }
        private Dictionary<string, ColumnInfo> _dictionary = new Dictionary<string, ColumnInfo>();

        public ColumnInfoCollection(TableAttribute table, IEnumerable<ColumnInfo> collection = null)
        {
            var baseType = typeof(TBase);
            MappedTypes = baseType.Assembly.GetTypes()
                                           .Where(baseType.IsAssignableFrom)
                                           .OrderBy(t => t.GetGenerationNumberFrom(baseType))
                                           .ThenBy(t => t.Name)
                                           .ToArray();
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
            return Equals((ColumnInfoCollection<TBase>)obj);
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
            //TODO: get rid of reflection

            var parts = columnName.Split('.');
            var obj = instance;
            IColumnInfoCollection columns = this;
            var type = GetObjectType(obj);
            if (typeof(IEntityProxy).IsAssignableFrom(type)) type = type.BaseType;
            for (var i = 0; i < parts.Length; i++)
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

        public ColumnInfoCollection<TBase> ToTable(string tableName, string schemaName = null)
        {
            Table.Name = tableName;
            Table.Schema = schemaName;
            return this;
        }

        public ColumnInfoCollection<TBase> HasKey(Expression<Func<TBase, object>> expression)
        {
            var column = ExpressionToColumn(expression);
            column.Value.IsIdentity = true;
            return this;
        }

        public ColumnInfoCollection<TBase> HasForeignKey(Expression<Func<TBase, object>> expression)
        {
            throw new NotImplementedException();
        }

        public ColumnInfoCollection<TBase> HasDefaultValue(Expression<Func<TBase, object>> expression, object value)
        {
            var column = ExpressionToColumn(expression);
            column.Value.DefaultValue = value;
            return this;
        }

        public ColumnInfoCollection<TBase> IsUnique(Expression<Func<TBase, object>> expression)
        {
            var column = ExpressionToColumn(expression);
            column.Value.IndexType = IndexType.Unique;
            return this;
        }        

        public ColumnInfoCollection<TBase> Ignore(Expression<Func<TBase, object>> expression)
        {
            var column = ExpressionToColumn(expression);
            _dictionary.Remove(column.Key);
            return this;
        }

        private KeyValuePair<string, ColumnInfo> ExpressionToColumn(Expression<Func<TBase, object>> expression)
        {
            var unaryExpression = (expression).Body as UnaryExpression;
            if (unaryExpression != null)
            {
                var propertyExpression = unaryExpression.Operand as MemberExpression;
                if (propertyExpression != null)
                {
                    var property = propertyExpression.Member as PropertyInfo;
                    if (property != null) return _dictionary.First(ci => ci.Value.PropertyName == property.Name);
                }
            }
            throw new Exception("Property cannot be determined");
        }

        private static Type GetObjectType(object obj)
        {
            var type = obj.GetType();
            if (typeof(IEntityProxy).IsAssignableFrom(type)) type = type.BaseType;
            return type;
        }
    }
}